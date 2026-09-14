using System.Diagnostics;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using tShockLoader.Relinker;

namespace tShockLoader.Plugins;

static class PluginLoader
{
    static readonly string[] CoreAssemblyNames =
    [
        "TShockAPI",
        "TerrariaApi.Server",
        "tShockLoader",
        "OTAPI",
        "OTAPI.Runtime",
        "TerrariaServer",
    ];

    static readonly Dictionary<string, Assembly> loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);

    internal static void Load(Main game)
    {
        loadedAssemblies["TShockAPI"] = typeof(TShock).Assembly;
        loadedAssemblies["TerrariaApi.Server"] = typeof(ServerApi).Assembly;
        loadedAssemblies["tShockLoader"] = typeof(PluginLoader).Assembly;
        ServerApi.AdditionalAssemblyResolve = ResolveLoaded;

        var pluginRoot = ServerApi.ServerPluginsDirectoryPath;
        RejectCoreCopies(pluginRoot);

        var tshock = new TShock(game);
        ServerApi.AddPlugin(new PluginContainer(tshock) { Source = "Core" });

        var ignoredPath = Path.Combine(pluginRoot, "ignoredplugins.txt");
        var ignored = File.Exists(ignoredPath)
            ? new HashSet<string>(
                File.ReadAllLines(ignoredPath).Select(l => l.Trim()).Where(l => l.Length > 0),
                StringComparer.OrdinalIgnoreCase)
            : [];

        var initWatches = new Dictionary<TerrariaPlugin, Stopwatch> { [tshock] = new() };

        foreach (var file in EnumeratePluginFiles(pluginRoot))
        {
            var id = Path.GetFileNameWithoutExtension(file.Name);
            if (ignored.Contains(id) || ignored.Contains(file.Name))
            {
                ServerApi.LogWriter.ServerWriteLine($"{id} was ignored from being loaded.", TraceLevel.Verbose);
                continue;
            }

            LoadPluginAssembly(file.FullName, game, initWatches);
        }

        foreach (var container in ServerApi.Plugins
                     .OrderBy(p => p.Plugin.Order)
                     .ThenBy(p => p.Plugin.Name, StringComparer.Ordinal)
                     .ThenBy(p => p.Plugin.GetType().FullName, StringComparer.Ordinal)
                     .ThenBy(p => p.Plugin.GetType().Assembly.GetName().Name, StringComparer.Ordinal))
        {
            var watch = initWatches[container.Plugin];
            watch.Start();
            try
            {
                container.Initialize();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Plugin \"{container.Plugin.Name}\" has thrown an exception during initialization.", ex);
            }

            watch.Stop();
            ServerApi.LogWriter.ServerWriteLine(
                $"Plugin {container.Plugin.Name} v{container.Plugin.Version} by {container.Plugin.Author} initiated ({watch.Elapsed.TotalMilliseconds:0}ms) source={container.Source}",
                TraceLevel.Info);
        }
    }

    internal static void Unload()
    {
        var initialized = ServerApi.Plugins.Where(p => p.Initialized).Reverse().ToList();
        var tshock = initialized.Where(p => p.Source == "Core").ToList();
        var thirdParty = initialized.Where(p => p.Source != "Core").ToList();

        DisposeAll(thirdParty);
        DisposeAll(tshock);
        loadedAssemblies.Clear();
    }

    static void DisposeAll(IEnumerable<PluginContainer> containers)
    {
        foreach (var container in containers)
        {
            try
            {
                container.DeInitialize();
                container.Dispose();
            }
            catch (Exception ex)
            {
                ServerApi.LogWriter.ServerWriteLine(
                    $"Plugin \"{container.Plugin.Name}\" has thrown an exception during disposal:\n{ex}",
                    TraceLevel.Error);
            }
        }
    }

    static void RejectCoreCopies(string pluginRoot)
    {
        foreach (var name in CoreAssemblyNames)
        {
            var path = Path.Combine(pluginRoot, name + ".dll");
            if (File.Exists(path))
            {
                throw new InvalidOperationException(
                    $"Core assembly copy found at {path}. Remove it from ServerPlugins; core components are delivered with the loader.");
            }
        }
    }

    static IEnumerable<FileInfo> EnumeratePluginFiles(string pluginRoot)
    {
        var directory = new DirectoryInfo(pluginRoot);
        if (!directory.Exists)
            return [];

        return directory.EnumerateFiles("*.dll")
            .Concat(directory.EnumerateFiles("*.dll-plugin"))
            .OrderBy(f => f.Name, StringComparer.Ordinal);
    }

    static void LoadPluginAssembly(string path, Main game, Dictionary<TerrariaPlugin, Stopwatch> initWatches)
    {
        var id = Path.GetFileNameWithoutExtension(path);
        if (!loadedAssemblies.TryGetValue(id, out var assembly))
        {
            try
            {
                assembly = LoadPluginFile(path);
            }
            catch (BadImageFormatException)
            {
                return;
            }

            loadedAssemblies[id] = assembly;
        }

        CollectPlugins(assembly, game, initWatches);
    }

    static Assembly LoadPluginFile(string path)
    {
        var pe = File.ReadAllBytes(path);
        if (PluginRelinker.NeedsRelink(pe))
            pe = PluginRelinker.Relink(pe);
        return Assembly.Load(pe);
    }

    static void CollectPlugins(Assembly assembly, Main game, Dictionary<TerrariaPlugin, Stopwatch> initWatches)
    {
        foreach (var type in assembly.GetExportedTypes())
        {
            if (!type.IsSubclassOf(typeof(TerrariaPlugin)) || !type.IsPublic || type.IsAbstract)
                continue;
            if (type == typeof(TShock))
                continue;

            var versionAttr = (ApiVersionAttribute?)Attribute.GetCustomAttribute(type, typeof(ApiVersionAttribute));
            if (versionAttr is null)
                continue;

            if (!ServerApi.IgnoreVersion &&
                (versionAttr.ApiVersion.Major != ServerApi.ApiVersion.Major ||
                 versionAttr.ApiVersion.Minor != ServerApi.ApiVersion.Minor))
            {
                throw new InvalidOperationException(
                    $"Plugin \"{type.FullName}\" in {assembly.Location} is designed for Server API {versionAttr.ApiVersion.ToString(2)}.");
            }

            var watch = Stopwatch.StartNew();
            TerrariaPlugin plugin;
            try
            {
                plugin = (TerrariaPlugin)Activator.CreateInstance(type, game)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not create an instance of plugin class \"{type.FullName}\".", ex);
            }

            watch.Stop();
            initWatches[plugin] = watch;
            ServerApi.AddPlugin(new PluginContainer(plugin) { Source = "Plugin" });
        }
    }

    static Assembly? ResolveLoaded(string name)
    {
        if (loadedAssemblies.TryGetValue(name, out var loaded))
            return loaded;

        var candidate = Path.Combine(ServerApi.ServerPluginsDirectoryPath, name + ".dll");
        if (!File.Exists(candidate))
            return null;

        var id = Path.GetFileNameWithoutExtension(candidate);
        if (CoreAssemblyNames.Contains(id, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Core assembly copy found at {candidate}.");

        var assembly = LoadPluginFile(candidate);
        loadedAssemblies[name] = assembly;
        return assembly;
    }
}
