using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockLoader.Abstractions;

namespace PortablePlugin;

[ApiVersion(2, 1)]
public sealed class PortablePlugin : TerrariaPlugin
{
    public PortablePlugin(Main game) : base(game)
    {
    }

    public override string Name => "Portable";
    public override Version Version => new(0, 1, 0);
    public override string Author => "tShockLoader";
    public override string Description => "Dual-environment TmlBridge probe.";

    public override void Initialize()
    {
        Commands.ChatCommands.Add(new Command(OnCommand, "portable"));
        var available = TmlBridge.TryGet(out var tml);
        var sdk = typeof(TmlBridge).Assembly.GetName().Name;
        var copies = AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == "TShockLoader.Abstractions");
        var line = available
            ? $"PortablePlugin: tml product={tml.Host.Product} contract={tml.Host.ContractVersion} loader={tml.Host.LoaderVersion} tml={tml.Host.TmlVersion} sdk={sdk} copies={copies}"
            : $"PortablePlugin: tml unavailable sdk={sdk} copies={copies}";
        TShock.Log.ConsoleInfo(line);
    }

    static void OnCommand(CommandArgs args)
    {
        if (TmlBridge.TryGet(out var tml))
        {
            args.Player.SendSuccessMessage($"tml {tml.Host.Product} {tml.Host.ContractVersion}");
            return;
        }

        args.Player.SendSuccessMessage("vanilla");
    }
}
