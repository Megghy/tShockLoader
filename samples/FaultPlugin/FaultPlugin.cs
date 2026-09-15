using Terraria;
using TerrariaApi.Server;

namespace FaultPlugin;

[ApiVersion(2, 1)]
public sealed class FaultPlugin : TerrariaPlugin
{
    public FaultPlugin(Main game) : base(game)
    {
        Order = 100;
        if (Environment.GetEnvironmentVariable("TSHOCKLOADER_FAULT") == "ctor")
            throw new InvalidOperationException("injected ctor fault");
    }

    public override string Name => "Fault";
    public override Version Version => new(0, 1, 0);
    public override string Author => "tShockLoader";
    public override string Description => "Optional ctor/init failure injection.";

    public override void Initialize()
    {
        if (Environment.GetEnvironmentVariable("TSHOCKLOADER_FAULT") == "init")
            throw new InvalidOperationException("injected init fault");
    }
}
