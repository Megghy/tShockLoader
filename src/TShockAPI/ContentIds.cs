using Terraria.ModLoader;

namespace TShockAPI;

static class ContentIds
{
    public static int Items => ItemLoader.ItemCount;
    public static int Npcs => NPCLoader.NPCCount;
    public static int Buffs => BuffLoader.BuffCount;
    public static int Prefixes => PrefixLoader.PrefixCount;
    public static int Tiles => TileLoader.TileCount;
    public static int Walls => WallLoader.WallCount;
    public static int Projectiles => ProjectileLoader.ProjectileCount;
}
