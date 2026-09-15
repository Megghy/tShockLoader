namespace tShockLoader.Runtime;

sealed class LoaderPaths
{
    public required string InstallRoot { get; init; }
    public required string InstanceRoot { get; init; }
    public required string PluginRoot { get; init; }
    public required string TShockDataRoot { get; init; }
    public required string LogRoot { get; init; }
    public required string CacheRoot { get; init; }

    public static LoaderPaths Current { get; private set; } = null!;

    public static LoaderPaths Resolve(string[] args)
    {
        var install = Path.GetFullPath(AppContext.BaseDirectory);
        string? instanceArg = null;
        string? tmlSave = null;
        string? configPath = null;
        string? logPath = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (TryReadValue(args, ref i, "-instancepath", out var value))
                instanceArg = value;
            else if (TryReadValue(args, ref i, "-tmlsavedirectory", out value))
                tmlSave = value;
            else if (TryReadValue(args, ref i, "-configpath", out value))
                configPath = value;
            else if (TryReadValue(args, ref i, "-logpath", out value))
                logPath = value;
        }

        var instance = ResolveAgainst(install, instanceArg ?? tmlSave ?? install);
        var data = configPath is null
            ? Path.Combine(instance, "tshock")
            : ResolveAgainst(instance, configPath);
        var logs = logPath is null
            ? Path.Combine(data, "logs")
            : ResolveAgainst(instance, logPath);

        Current = new LoaderPaths
        {
            InstallRoot = install,
            InstanceRoot = instance,
            PluginRoot = Path.Combine(instance, TerrariaApi.Server.ServerApi.PluginsPath),
            TShockDataRoot = data,
            LogRoot = logs,
            CacheRoot = Path.Combine(instance, "cache", "tshockloader"),
        };
        return Current;
    }

    static bool TryReadValue(string[] args, ref int i, string flag, out string? value)
    {
        value = null;
        if (!args[i].Equals(flag, StringComparison.OrdinalIgnoreCase) || i + 1 >= args.Length)
            return false;
        value = args[++i];
        return true;
    }

    static string ResolveAgainst(string root, string path)
        => Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(root, path));
}
