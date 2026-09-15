using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
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
    static readonly List<PluginContainer> initializedOrder = [];

    internal static void Load(Main game)
    {
        loadedAssemblies["TShockAPI"] = typeof(TShock).Assembly;
        loadedAssemblies["TerrariaApi.Server"] = typeof(ServerApi).Assembly;
        loadedAssemblies["tShockLoader"] = typeof(PluginLoader).Assembly;
        ServerApi.AdditionalAssemblyResolve = ResolveLoaded;
        HostContext.Resolving -= ResolveHost;
        HostContext.Resolving += ResolveHost;

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
        var pending = new List<(string Path, byte[] Pe, string Id)>();

        foreach (var file in EnumeratePluginFiles(pluginRoot))
        {
            var id = Path.GetFileNameWithoutExtension(file.Name);
            if (ignored.Contains(id) || ignored.Contains(file.Name))
            {
                ServerApi.LogWriter.ServerWriteLine($"{id} was ignored from being loaded.", TraceLevel.Verbose);
                continue;
            }

            byte[] pe;
            try
            {
                pe = RelinkPluginFile(file.FullName);
            }
            catch (BadImageFormatException)
            {
                continue;
            }
            catch (RelinkUnsupportedException ex)
            {
                ServerApi.LogWriter.ServerWriteLine($"{id} skipped: {ex.Message}", TraceLevel.Error);
                continue;
            }

            pending.Add((file.FullName, pe, id));
        }

        OtapiRuntime.Install();
        if (OtapiRuntime.Assembly is not null)
            loadedAssemblies["OTAPI.Runtime"] = OtapiRuntime.Assembly;

        foreach (var item in pending)
            LoadPluginAssembly(item.Pe, item.Id, game, initWatches);

        var sequence = ServerApi.Plugins
            .OrderBy(p => p.Plugin.Order)
            .ThenBy(p => p.Plugin.Name, StringComparer.Ordinal)
            .ThenBy(p => p.Plugin.GetType().FullName, StringComparer.Ordinal)
            .ThenBy(p => p.Plugin.GetType().Assembly.GetName().Name, StringComparer.Ordinal)
            .ToList();
        ServerApi.LogWriter.ServerWriteLine(
            $"init-order {string.Join(",", sequence.Select(p => $"{p.Plugin.Name}:{p.Plugin.Order}"))}",
            TraceLevel.Info);

        foreach (var container in sequence)
            InitializePlugin(container, initWatches[container.Plugin]);
    }

    static void InitializePlugin(PluginContainer container, Stopwatch watch)
    {
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
        initializedOrder.Add(container);
        ServerApi.LogWriter.ServerWriteLine(
            $"Plugin {container.Plugin.Name} v{container.Plugin.Version} by {container.Plugin.Author} initiated ({watch.Elapsed.TotalMilliseconds:0}ms) source={container.Source}",
            TraceLevel.Info);
    }

    internal static void Unload()
    {
        var plugins = ServerApi.Plugins.ToList();
        var thirdInited = initializedOrder.Where(p => p.Source != "Core").Reverse();
        var thirdRest = plugins.Where(p => p.Source != "Core" && !initializedOrder.Contains(p)).Reverse();
        var core = plugins.Where(p => p.Source == "Core");
        var order = thirdInited.Concat(thirdRest).Concat(core).ToList();
        ServerApi.LogWriter.ServerWriteLine(
            $"dispose-order {string.Join(",", order.Select(p => p.Plugin.Name))}",
            TraceLevel.Info);

        var first = DisposeAll(order);
        OtapiRuntimeBinder.DisposeAll();
        initializedOrder.Clear();
        loadedAssemblies.Clear();
        if (first is not null)
            throw first;
    }

    static Exception? DisposeAll(IEnumerable<PluginContainer> containers)
    {
        Exception? first = null;
        foreach (var container in containers)
        {
            try
            {
                container.DeInitialize();
                container.Dispose();
            }
            catch (Exception ex)
            {
                first ??= ex;
                ServerApi.LogWriter.ServerWriteLine(
                    $"Plugin \"{container.Plugin.Name}\" has thrown an exception during disposal:\n{ex}",
                    TraceLevel.Error);
            }
        }

        return first;
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

    static void LoadPluginAssembly(byte[] pe, string id, Main game, Dictionary<TerrariaPlugin, Stopwatch> initWatches)
    {
        if (!loadedAssemblies.TryGetValue(id, out var assembly))
        {
            using var stream = new MemoryStream(pe, writable: false);
            assembly = HostContext.LoadFromStream(stream);
            loadedAssemblies[id] = assembly;
        }

        CollectPlugins(assembly, game, initWatches);
    }

    static byte[] RelinkPluginFile(string path)
    {
        var pe = File.ReadAllBytes(path);
        var id = Path.GetFileNameWithoutExtension(path);
        if (!PluginRelinker.NeedsRelink(pe))
            return pe;

        pe = PluginRelinker.Relink(pe, id);
        ServerApi.LogWriter.ServerWriteLine($"relink {id}", TraceLevel.Info);
        return pe;
    }

    static readonly AssemblyLoadContext HostContext =
        AssemblyLoadContext.GetLoadContext(typeof(PluginLoader).Assembly)
        ?? throw new InvalidOperationException("tShockLoader assembly has no load context.");

    static Assembly? ResolveHost(AssemblyLoadContext context, AssemblyName identity)
    {
        var name = identity.Name;
        if (string.IsNullOrEmpty(name))
            return null;

        foreach (var asm in context.Assemblies)
        {
            if (string.Equals(asm.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                return asm;
        }

        if (name is "OTAPI.Runtime")
            return OtapiRuntime.Assembly;
        if (name is "TerrariaServer")
            return typeof(ServerApi).Assembly;
        if (name is "Terraria")
            return typeof(Main).Assembly;

        return ResolveLoaded(name);
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

        foreach (var asm in HostContext.Assemblies)
        {
            if (!string.Equals(asm.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            loadedAssemblies[name] = asm;
            return asm;
        }

        var candidate = Path.Combine(ServerApi.ServerPluginsDirectoryPath, name + ".dll");
        if (!File.Exists(candidate))
            return null;

        var id = Path.GetFileNameWithoutExtension(candidate);
        if (CoreAssemblyNames.Contains(id, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Core assembly copy found at {candidate}.");

        var pe = RelinkPluginFile(candidate);
        using var stream = new MemoryStream(pe, writable: false);
        var assembly = HostContext.LoadFromStream(stream);
        loadedAssemblies[name] = assembly;
        return assembly;
    }
}
