using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Localization;
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

public class tShockLoaderSystem : ModSystem
{
    public override void PostSetupContent()
    {
        if (!Main.dedServ)
            return;

        EnglishLanguage.RebuildContentNames();
        TShock.Utils.ComputeMaxStyles();
        Mod.Logger.Info($"content names vanillaItems={ItemID.Count} items={ItemLoader.ItemCount} npcs={NPCLoader.NPCCount} buffs={BuffLoader.BuffCount} prefixes={PrefixLoader.PrefixCount} tiles={TileLoader.TileCount}");
    }

    public override void OnWorldLoad()
    {
        if (!Main.dedServ)
            return;

        ContentContractProbe.Verify();
    }
}

public class GlobalTileHandler : GlobalTile
{
    public override bool PreHitWire(int i, int j, int type)
    {
        return !OTAPI.Hooks.Wiring.InvokeAnnouncementBox(i, j, type);
    }
}
