using Terraria;
using Terraria.ModLoader;

namespace tShockLoader.Runtime;

public sealed class LifecycleLog : ModSystem
{
    public override void OnModLoad() => Write(nameof(OnModLoad));
    public override void PostSetupContent() => Write(nameof(PostSetupContent));
    public override void PostWorldLoad() => Write($"PostWorldLoad world={Main.worldName} maxTilesX={Main.maxTilesX}");
    public override void PreSaveAndQuit() => Write(nameof(PreSaveAndQuit));
    public override void Unload() => Write(nameof(Unload));

    static void Write(string stage)
        => Console.WriteLine($"tShockLoader lifecycle {stage} thread={Environment.CurrentManagedThreadId} dedServ={Main.dedServ} gameMenu={Main.gameMenu} netMode={Main.netMode}");
}
