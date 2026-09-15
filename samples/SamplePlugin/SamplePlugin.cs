using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaApi.Server;
using TShockAPI;

namespace SamplePlugin;

[ApiVersion(2, 1)]
public sealed class SamplePlugin : TerrariaPlugin
{
    readonly Counters counters = new();
    Command? command;
    bool worldChecked;

    public SamplePlugin(Main game) : base(game)
    {
        Order = 1;
    }

    public override string Name => "Probe";
    public override Version Version => new(0, 1, 0);
    public override string Author => "tShockLoader";
    public override string Description => "Host-chain probe for identity, commands, tiles, and hooks.";

    public override void Initialize()
    {
        ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
        ServerApi.Hooks.GamePostInitialize.Register(this, OnGamePostInitialize);
        ServerApi.Hooks.GameUpdate.Register(this, OnPriorityLow, 0);
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        ServerApi.Hooks.GameUpdate.Register(this, OnPriorityHigh, 10);
        ServerApi.Hooks.GamePostUpdate.Register(this, OnGamePostUpdate);
        ServerApi.Hooks.GameWorldConnect.Register(this, OnWorldConnect);
        ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
        ServerApi.Hooks.NetSendData.Register(this, OnNetSendData);
        ServerApi.Hooks.NpcSpawn.Register(this, OnNpcSpawn);
        ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
        ServerApi.Hooks.WorldSave.Register(this, OnWorldSave);
        ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
        ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);

        command = new Command(OnProbe, "probe")
        {
            HelpText = "Dump tShockLoader host-chain probe status.",
        };
        Commands.ChatCommands.Add(command);

        var identity = IdentityFailures();
        Log(Report("initialize"));
        if (identity.Count > 0)
            throw new InvalidOperationException("Probe identity failed: " + string.Join("; ", identity));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
            ServerApi.Hooks.GamePostInitialize.Deregister(this, OnGamePostInitialize);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnPriorityLow);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            ServerApi.Hooks.GameUpdate.Deregister(this, OnPriorityHigh);
            ServerApi.Hooks.GamePostUpdate.Deregister(this, OnGamePostUpdate);
            ServerApi.Hooks.GameWorldConnect.Deregister(this, OnWorldConnect);
            ServerApi.Hooks.NetGetData.Deregister(this, OnNetGetData);
            ServerApi.Hooks.NetSendData.Deregister(this, OnNetSendData);
            ServerApi.Hooks.NpcSpawn.Deregister(this, OnNpcSpawn);
            ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
            ServerApi.Hooks.WorldSave.Deregister(this, OnWorldSave);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
            if (command is not null)
                Commands.ChatCommands.Remove(command);
        }

        base.Dispose(disposing);
    }

    void OnGameInitialize(EventArgs _) => counters.GameInitialize++;
    void OnGamePostInitialize(EventArgs _)
    {
        counters.GamePostInitialize++;
        CheckWorld();
    }
    void OnGamePostUpdate(EventArgs _) => counters.GamePostUpdate++;
    void OnNetGetData(GetDataEventArgs _) => counters.NetGetData++;
    void OnNetSendData(SendDataEventArgs _) => counters.NetSendData++;
    void OnNpcSpawn(NpcSpawnEventArgs _) => counters.NpcSpawn++;
    void OnNpcKilled(NpcKilledEventArgs _) => counters.NpcKilled++;
    void OnWorldSave(WorldSaveEventArgs _) => counters.WorldSave++;
    void OnServerJoin(JoinEventArgs _) => counters.ServerJoin++;
    void OnServerLeave(LeaveEventArgs _) => counters.ServerLeave++;

    void OnWorldConnect(EventArgs _)
    {
        counters.WorldConnect++;
        CheckWorld();
    }

    void OnPriorityHigh(EventArgs _) => counters.PriorityFirst ??= "high";
    void OnPriorityLow(EventArgs _) => counters.PriorityFirst ??= "low";

    void OnGameUpdate(EventArgs _)
    {
        counters.GameUpdate++;
        if (counters.GameUpdate == 1)
            Log(Report("first-update"));
        if (!worldChecked && WorldReady())
            CheckWorld();
    }

    void OnProbe(CommandArgs args)
    {
        foreach (var line in Report("command"))
            args.Player.SendInfoMessage(line);
        var fails = AllFailures();
        foreach (var fail in fails)
            args.Player.SendErrorMessage("FAIL " + fail);
        if (fails.Count == 0)
            args.Player.SendSuccessMessage("probe ok");
    }

    void CheckWorld()
    {
        if (worldChecked || !WorldReady())
            return;

        worldChecked = true;
        Log(Report("world"));
        var fails = TileFailures();
        if (fails.Count > 0)
            throw new InvalidOperationException("Probe tile failed: " + string.Join("; ", fails));
    }

    List<string> Report(string stage)
    {
        var plugins = string.Join(",", ServerApi.Plugins.Select(p => $"{p.Plugin.Name}:{p.Source}"));
        return
        [
            $"[probe] stage={stage} TShock={TShock.VersionNum} api={ServerApi.ApiVersion}",
            $"[probe] identity tshock={typeof(TShock).Assembly.FullName}",
            $"[probe] identity api={typeof(ServerApi).Assembly.FullName}",
            $"[probe] assemblies tshock={CountAssemblies("TShockAPI")} api={CountAssemblies("TerrariaApi.Server")} tml={CountAssemblies("tModLoader")}",
            $"[probe] plugins={ServerApi.Plugins.Count} [{plugins}]",
            $"[probe] commands={Commands.ChatCommands.Count} players={TShock.Players?.Length.ToString() ?? "null"} maxNet={Main.maxNetPlayers}",
            $"[probe] content vanillaItems={ItemID.Count} items={ItemLoader.ItemCount} npcs={NPCLoader.NPCCount} buffs={BuffLoader.BuffCount} prefixes={PrefixLoader.PrefixCount}",
            $"[probe] hooks init={counters.GameInitialize} postInit={counters.GamePostInitialize} worldConnect={counters.WorldConnect} update={counters.GameUpdate} postUpdate={counters.GamePostUpdate} priority={counters.PriorityFirst ?? "none"} clients={Terraria.Netplay.HasClients} gameMenu={Main.gameMenu}",
            $"[probe] hooks getData={counters.NetGetData} sendData={counters.NetSendData} npcSpawn={counters.NpcSpawn} npcKilled={counters.NpcKilled} worldSave={counters.WorldSave} join={counters.ServerJoin} leave={counters.ServerLeave}",
            $"[probe] {DescribeTiles()}",
        ];
    }

    static bool WorldReady() =>
        Main.maxTilesX > 0 && Main.maxTilesY > 0 && (Main.spawnTileX != 0 || Main.spawnTileY != 0);

    static string DescribeTiles()
    {
        if (!WorldReady())
            return "tiles not loaded";

        var spawn = Main.tile[Main.spawnTileX, Main.spawnTileY];
        var scan = ScanSpawn(32);
        return $"tiles {Main.maxTilesX}x{Main.maxTilesY} spawn={Main.spawnTileX},{Main.spawnTileY} has={spawn.HasTile} type={spawn.TileType} wall={spawn.WallType} liquid={spawn.LiquidAmount} scanned={scan.Total} solid={scan.Solid} walls={scan.Walls} liquids={scan.Liquids}";
    }

    static (int Total, int Solid, int Walls, int Liquids) ScanSpawn(int radius)
    {
        int x0 = Math.Clamp(Main.spawnTileX - radius, 0, Main.maxTilesX - 1);
        int x1 = Math.Clamp(Main.spawnTileX + radius, 0, Main.maxTilesX - 1);
        int y0 = Math.Clamp(Main.spawnTileY - radius, 0, Main.maxTilesY - 1);
        int y1 = Math.Clamp(Main.spawnTileY + radius, 0, Main.maxTilesY - 1);
        int total = 0, solid = 0, walls = 0, liquids = 0;
        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                var tile = Main.tile[x, y];
                total++;
                if (tile.HasTile)
                    solid++;
                if (tile.WallType > 0)
                    walls++;
                if (tile.LiquidAmount > 0)
                    liquids++;
            }
        }

        return (total, solid, walls, liquids);
    }

    List<string> IdentityFailures()
    {
        List<string> fails = [];
        if (CountAssemblies("TShockAPI") != 1)
            fails.Add($"TShockAPI instances={CountAssemblies("TShockAPI")}");
        if (CountAssemblies("TerrariaApi.Server") != 1)
            fails.Add($"TerrariaApi.Server instances={CountAssemblies("TerrariaApi.Server")}");
        if (typeof(TShock).Assembly.GetName().Name != "TShockAPI")
            fails.Add($"TShock assembly={typeof(TShock).Assembly.GetName().Name}");
        if (typeof(ServerApi).Assembly.GetName().Name != "TerrariaApi.Server")
            fails.Add($"ServerApi assembly={typeof(ServerApi).Assembly.GetName().Name}");
        if (TShock.Players is null)
            fails.Add("TShock.Players is null");
        if (Commands.ChatCommands.Count == 0)
            fails.Add("Commands.ChatCommands is empty");
        if (!ServerApi.Plugins.Any(p => p.Plugin is TShock && p.Source == "Core"))
            fails.Add("TShock Core plugin missing");
        return fails;
    }

    List<string> TileFailures()
    {
        List<string> fails = [];
        if (Main.maxTilesX <= 0 || Main.maxTilesY <= 0)
            fails.Add($"world size {Main.maxTilesX}x{Main.maxTilesY}");
        else
        {
            var scan = ScanSpawn(32);
            if (scan.Total == 0)
                fails.Add("tile scan empty");
            if (scan.Solid == 0)
                fails.Add("spawn window has no solid tiles");
        }

        return fails;
    }

    List<string> AllFailures()
    {
        var fails = IdentityFailures();
        fails.AddRange(TileFailures());
        if (counters.GameInitialize == 0)
            fails.Add("GameInitialize=0");
        if (counters.GamePostInitialize == 0)
            fails.Add("GamePostInitialize=0");
        if (Terraria.Netplay.HasClients && counters.GameUpdate == 0)
            fails.Add("GameUpdate=0 with clients");
        if (Terraria.Netplay.HasClients && !Main.gameMenu && counters.WorldConnect == 0)
            fails.Add("GameWorldConnect=0 with clients");
        return fails;
    }

    static int CountAssemblies(string name) =>
        AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == name);

    static void Log(List<string> lines)
    {
        foreach (var line in lines)
        {
            Console.WriteLine(line);
            TShock.Log?.Info(line);
        }
    }

    sealed class Counters
    {
        public int GameInitialize;
        public int GamePostInitialize;
        public int GameUpdate;
        public string? PriorityFirst;
        public int GamePostUpdate;
        public int WorldConnect;
        public int NetGetData;
        public int NetSendData;
        public int NpcSpawn;
        public int NpcKilled;
        public int WorldSave;
        public int ServerJoin;
        public int ServerLeave;
    }
}
