using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace SamplePlugin;

[ApiVersion(2, 1)]
public sealed class NegativeProbe : TerrariaPlugin
{
    public NegativeProbe(Main game) : base(game)
    {
        Order = -10;
    }

    public override string Name => "NegativeProbe";
    public override Version Version => new(0, 1, 0);
    public override string Author => "tShockLoader";
    public override string Description => "Initialize before TShock; dispose while TShock is still alive.";

    public override void Initialize()
    {
        Console.WriteLine($"[probe] NegativeProbe Initialize tshockLog={TShock.Log is not null}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Console.WriteLine($"[probe] NegativeProbe dispose tshockLog={TShock.Log is not null}");
        base.Dispose(disposing);
    }
}

[ApiVersion(2, 1)]
public sealed class ProbeTwin : TerrariaPlugin
{
    public ProbeTwin(Main game) : base(game)
    {
        Order = 1;
    }

    public override string Name => "ProbeTwin";
    public override Version Version => new(0, 1, 0);
    public override string Author => "tShockLoader";
    public override string Description => "Same Order as Probe; sorted by Name.";

    public override void Initialize()
    {
        Console.WriteLine("[probe] ProbeTwin Initialize");
    }
}
