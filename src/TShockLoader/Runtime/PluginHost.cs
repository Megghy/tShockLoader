using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Terraria;
using Terraria.ModLoader;
using TerrariaApi.Server;
using TerrariaApi.Server.Hooking;
using TShockAPI;
using TShockLoader.Abstractions;
using tShockLoader.Plugins;

namespace tShockLoader.Runtime;

public static class PluginHost
{
    static Mod? mod;
    static bool bound;
    static bool hooksAttached;
    static bool tmlBound;
    static bool pluginsLoaded;

    public static void Start(Mod owner)
    {
        if (bound)
            throw new InvalidOperationException("tShockLoader session is already started.");

        mod = owner;
        var paths = LoaderPaths.Resolve(Environment.GetCommandLineArgs());
        Directory.CreateDirectory(paths.PluginRoot);
        Directory.CreateDirectory(paths.TShockDataRoot);
        Directory.CreateDirectory(paths.LogRoot);
        TShock.SavePath = paths.TShockDataRoot;
        ServerApi.ServerPluginsDirectoryPath = paths.PluginRoot;

        try
        {
            ExtractNativeLibraries(owner, paths);
            ServerApi.Bind(Main.instance, Path.Combine(paths.LogRoot, "ServerLog.txt"));
            bound = true;
            Log($"config directory={paths.TShockDataRoot}");

            HookAttach.HookManager.AttachAll();
            ApiHooks.Attach(ServerApi.Hooks);
            hooksAttached = true;
            Log($"Main.dedServ={Main.dedServ} gameMenu={Main.gameMenu} netMode={Main.netMode}");

            BindAbstractions(owner);
            TmlBridge.Bind(new HostTmlContext());
            tmlBound = true;
            Log("tml bridge bound");

            PluginLoader.Load(Main.instance);
            pluginsLoaded = true;
            ServerApi.Hooks.InvokeGameInitialize();
            Log($"GameInitialize invoked after plugin registration (nativeObserved={GameHooks.GameInitializeObserved})");

            var loaded = ServerApi.Plugins.Count;
            Log($"ready plugins={loaded}");
            owner.Logger.Info($"ServerPlugins={paths.PluginRoot} plugins={loaded}");
            NetContractProbe.Verify();
            Log("net contract ok");
        }
        catch
        {
            Stop();
            throw;
        }
    }

    public static void Stop()
    {
        Exception? first = null;

        if (pluginsLoaded || bound)
            first = RunCleanup(PluginLoader.Unload, first);

        if (tmlBound)
            first = RunCleanup(TmlBridge.Unbind, first);

        if (hooksAttached)
        {
            first = RunCleanup(ApiHooks.Detach, first);
            first = RunCleanup(HookAttach.HookManager.DetachAll, first);
        }

        if (bound)
            first = RunCleanup(ServerApi.Unbind, first);

        if (sqliteInterop != IntPtr.Zero)
        {
            NativeLibrary.Free(sqliteInterop);
            sqliteInterop = IntPtr.Zero;
        }

        bound = false;
        hooksAttached = false;
        tmlBound = false;
        pluginsLoaded = false;
        mod = null;
        if (first is not null)
            Console.WriteLine($"tShockLoader: first cleanup error {first}");
    }

    static void Log(string message)
    {
        var line = $"tShockLoader: {message}";
        Console.WriteLine(line);
        if (ServerApi.IsBound)
            ServerApi.LogWriter.ServerWriteLine(line, TraceLevel.Info);
        mod?.Logger.Info(message);
    }

    static IntPtr sqliteInterop;

    static void BindAbstractions(Mod owner)
    {
        const string packed = "TShockLoader.Abstractions.dll";
        if (!owner.FileExists(packed))
            throw new FileNotFoundException($"tShockLoader.tmod is missing {packed}.");
        PluginLoader.SetAbstractionsHash(SHA256.HashData(owner.GetFileBytes(packed)));
    }

    static void ExtractNativeLibraries(Mod owner, LoaderPaths paths)
    {
        const string packed = "native/SQLite.Interop.dll";
        if (!owner.FileExists(packed))
            throw new FileNotFoundException($"tShockLoader.tmod is missing {packed}.");

        Directory.CreateDirectory(paths.CacheRoot);
        var dest = Path.Combine(paths.CacheRoot, "SQLite.Interop.dll");
        File.WriteAllBytes(dest, owner.GetFileBytes(packed));
        sqliteInterop = NativeLibrary.Load(dest);
        var sqlite = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "System.Data.SQLite")
            ?? Assembly.Load("System.Data.SQLite");
        NativeLibrary.SetDllImportResolver(sqlite, ResolveSqlite);
        Log($"native sqlite={dest}");
    }

    static IntPtr ResolveSqlite(string name, Assembly _, DllImportSearchPath? __)
    {
        if (name.Equals("SQLite.Interop", StringComparison.OrdinalIgnoreCase)
            || name.Equals("SQLite.Interop.dll", StringComparison.OrdinalIgnoreCase))
            return sqliteInterop;
        return IntPtr.Zero;
    }

    static Exception? RunCleanup(Action action, Exception? first)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            first ??= ex;
            Console.WriteLine($"tShockLoader cleanup error: {ex}");
        }

        return first;
    }
}
