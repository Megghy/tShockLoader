using Terraria.ID;

namespace TShockAPI
{
	public static class Constants
	{
		public static bool[] Explosives = ItemID.Sets.Factory.CreateBoolSet(new int[]
			{
				// Bombs
				ItemID.Bomb,
				ItemID.StickyBomb,
				ItemID.BouncyBomb,
				ItemID.BombFish,
				ItemID.DirtBomb,
				ItemID.DirtStickyBomb,
				ItemID.ScarabBomb,
				// Launchers
				ItemID.GrenadeLauncher,
				ItemID.RocketLauncher,
				ItemID.SnowmanCannon,
				ItemID.Celeb2,
				// Rockets
				ItemID.RocketII,
				ItemID.RocketIV,
				ItemID.ClusterRocketII,
				ItemID.MiniNukeII,
				// The following are classified as explosives untill we can figure out a better way.
				ItemID.DryRocket,
				ItemID.WetRocket,
				ItemID.LavaRocket,
				ItemID.HoneyRocket,
				// Explosives & misc
				ItemID.Dynamite,
				ItemID.Explosives,
				ItemID.StickyDynamite
	});
	}
}
