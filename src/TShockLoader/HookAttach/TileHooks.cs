using Terraria;

namespace tShockLoader.HookAttach
{
    public static class TileHooks
    {
        public static void Attach()
        {
            On_Tile.ctor += On_Tile_ctor;
        }
        public static void Detach()
        {
            On_Tile.ctor -= On_Tile_ctor;
        }

        private static void On_Tile_ctor(On_Tile.orig_ctor orig, ref Tile self, uint tileId)
        {
            self = OTAPI.Hooks.Tile.InvokeCreate(orig, ref self, tileId);
        }
    }
}