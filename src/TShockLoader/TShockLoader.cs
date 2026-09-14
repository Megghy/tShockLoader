using Terraria;
using Terraria.ModLoader;
using TerrariaApi.Server;
using tShockLoader.Runtime;

namespace tShockLoader;

public class tShockLoader : Mod
{
    public override void Load()
    {
        if (!Main.dedServ)
            return;

        PluginHost.Start(this);
    }

    public override void Unload()
    {
        PluginHost.Stop();
    }
}

public class GlobalTileHandler : GlobalTile
{
    public override bool PreHitWire(int i, int j, int type)
    {
        return !OTAPI.Hooks.Wiring.InvokeAnnouncementBox(i, j, type);
    }
}
