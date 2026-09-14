using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace SamplePlugin;

[ApiVersion(2, 1)]
public sealed class SamplePlugin : TerrariaPlugin
{
    public SamplePlugin(Main game) : base(game)
    {
        Order = -1;
    }

    public override string Name => "SamplePlugin";
    public override Version Version => new(0, 1, 0);
    public override string Author => "tShockLoader";
    public override string Description => "P0 sample: shared TShock/API types and GameInitialize registration.";

    public override void Initialize()
    {
        Console.WriteLine($"SamplePlugin sees TShock {TShock.VersionNum}, players={TShock.Players.Length}, same TShockAPI={typeof(TShock).Assembly.FullName}");
        Console.WriteLine($"SamplePlugin API assembly={typeof(ServerApi).Assembly.FullName}");
        ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
    }

    void OnGameInitialize(EventArgs args)
    {
        Console.WriteLine($"SamplePlugin GameInitialize TShock.VersionNum={TShock.VersionNum}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
        base.Dispose(disposing);
    }
}
