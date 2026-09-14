using System.Diagnostics;
using Terraria;
using Terraria.ModLoader;
using TerrariaApi.Server;
using TerrariaApi.Server.Hooking;
using tShockLoader.Plugins;

namespace tShockLoader.Runtime;

public static class PluginHost
{
    static Mod? mod;
    static bool started;

    public static void Start(Mod owner)
    {
        if (started)
            throw new InvalidOperationException("tShockLoader session is already started.");

        mod = owner;
        ServerApi.ServerPluginsDirectoryPath = Path.Combine(AppContext.BaseDirectory, ServerApi.PluginsPath);
        Directory.CreateDirectory(ServerApi.ServerPluginsDirectoryPath);

        ServerApi.Bind(Main.instance);
        HookAttach.HookManager.AttachAll();
        AttachApiHooks();
        PluginLoader.Load(Main.instance);
        started = true;

        var loaded = ServerApi.Plugins.Count;
        ServerApi.LogWriter.ServerWriteLine(
            $"tShockLoader ready. ServerPlugins={ServerApi.ServerPluginsDirectoryPath} plugins={loaded}",
            TraceLevel.Info);
        mod.Logger.Info($"ServerPlugins={ServerApi.ServerPluginsDirectoryPath} plugins={loaded}");
    }

    public static void Stop()
    {
        if (!started)
            return;

        PluginLoader.Unload();
        ServerApi.Unbind();
        HookAttach.HookManager.DetachAll();
        started = false;
        mod = null;
    }

    static void AttachApiHooks()
    {
        var hooks = ServerApi.Hooks;
        GameHooks.AttachTo(hooks);
        ItemHooks.AttachTo(hooks);
        NetHooks.AttachTo(hooks);
        NpcHooks.AttachTo(hooks);
        ProjectileHooks.AttachTo(hooks);
        ServerHooks.AttachTo(hooks);
        WiringHooks.AttachTo(hooks);
        WorldHooks.AttachTo(hooks);
    }
}
