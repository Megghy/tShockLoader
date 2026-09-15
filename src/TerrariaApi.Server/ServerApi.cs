using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Terraria;

namespace TerrariaApi.Server;

public static class ServerApi
{
    public const string PluginsPath = "ServerPlugins";
    public static readonly Version ApiVersion = new(2, 1, 0, 0);

    static Main? game;
    static readonly List<PluginContainer> plugins = [];

    public static string[] ClientUUID { get; } = new string[256];
    public static bool IgnoreVersion { get; set; }
    public static string ServerPluginsDirectoryPath { get; internal set; } = "";
    public static ReadOnlyCollection<PluginContainer> Plugins => plugins.AsReadOnly();
    public static HookManager Hooks { get; } = new();
    public static LogWriterManager LogWriter { get; private set; } = null!;
    public static ProfilerManager Profiler { get; } = new();
    public static bool IsWorldRunning { get; internal set; }
    public static bool RunningMono { get; } = Type.GetType("Mono.Runtime") != null;
    public static bool ForceUpdate { get; set; }
    public static bool UseAsyncSocketsInMono { get; set; }

    internal static Main Game => game ?? throw new InvalidOperationException("ServerApi session is not bound.");
    internal static bool IsBound => game is not null;
    internal static Func<string, Assembly?>? AdditionalAssemblyResolve { get; set; }

    internal static void Bind(Main instance, string logFilePath)
    {
        if (game is not null)
            throw new InvalidOperationException("ServerApi session is already bound.");

        game = instance;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logFilePath))!);
        LogWriter = new LogWriterManager(enabled: true, logFilePath);
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        LogWriter.ServerWriteLine($"TerrariaApi - Server v{ApiVersion} started.", TraceLevel.Info);
    }

    internal static void AddPlugin(PluginContainer container) => plugins.Add(container);

    internal static void Unbind()
    {
        plugins.Clear();
        AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
        AdditionalAssemblyResolve = null;
        Profiler.Deatch();
        LogWriter?.Deatch();
        LogWriter = null!;
        game = null;
    }

    static Assembly? Resolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        if (name is "OTAPI" or "OTAPI.Upcoming" or "TerrariaServer" or "TerrariaApi.Server")
            return typeof(ServerApi).Assembly;

        if (name is null)
            return null;
        return AdditionalAssemblyResolve?.Invoke(name);
    }
}
