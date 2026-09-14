using Terraria;

namespace tShockLoader.HookAttach
{
    public static class WorldGenHooks
    {
        public static void Attach()
        {
            On_WorldGen.meteor += On_WorldGen_meteor;
            On_WorldGen.PlaceTile += On_WorldGen_PlaceTile;
            On_WorldGen.hardUpdateWorld += On_WorldGen_hardUpdateWorld;
        }

        public static void Detach()
        {
            On_WorldGen.meteor -= On_WorldGen_meteor;
            On_WorldGen.PlaceTile -= On_WorldGen_PlaceTile;
            On_WorldGen.hardUpdateWorld -= On_WorldGen_hardUpdateWorld;
        }

        private static bool On_WorldGen_meteor(On_WorldGen.orig_meteor orig, int i, int j, bool ignorePlayers)
        {
            int x = i;
            int y = j;
            if (!OTAPI.Hooks.WorldGen.InvokeMeteor(ref x, ref y))
                return false;
            return orig(x, y, ignorePlayers);
        }

        private static bool On_WorldGen_PlaceTile(On_WorldGen.orig_PlaceTile orig, int x, int y, int type, bool mute, bool forced, int plr, int style)
        {
            // 只在硬模式下调用
            if (Main.hardMode)
            {
                return OTAPI.Hooks.WorldGen.InvokeHardmodeTilePlace(x, y, type, mute, forced, plr, style);
            }
            return orig(x, y, type, mute, forced, plr, style);
        }

        private static void On_WorldGen_hardUpdateWorld(On_WorldGen.orig_hardUpdateWorld orig, int i, int j)
        {
            ushort type = 0;
            if (!OTAPI.Hooks.WorldGen.InvokeHardmodeTileUpdate(i, j, type))
            {
                orig(i, j);
            }
        }
    }
}
