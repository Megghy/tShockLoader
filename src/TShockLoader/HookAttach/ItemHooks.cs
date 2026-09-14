using Microsoft.Xna.Framework;
using OTAPI;
using Terraria;

namespace tShockLoader.HookAttach
{
    public static class ItemHooks
    {
        public static void Attach()
        {
            On_Item.MechSpawn += On_Item_MechSpawn;
        }

        public static void Detach()
        {
            On_Item.MechSpawn -= On_Item_MechSpawn;
        }

        private static bool On_Item_MechSpawn(On_Item.orig_MechSpawn orig, float x, float y, int type)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            for (int i = 0; i < 200; i++)
            {
                if (Main.item[i].active && Main.item[i].type == type)
                {
                    num++;
                    Vector2 vector = new Vector2(x, y);
                    float num4 = Main.item[i].position.X - vector.X;
                    float num5 = Main.item[i].position.Y - vector.Y;
                    float num6 = (float)Math.Sqrt(num4 * num4 + num5 * num5);
                    if (num6 < 300f)
                        num2++;

                    if (num6 < 800f)
                        num3++;
                }
            }

            if (num2 >= 3 || num3 >= 6 || num >= 10)
            {
                return Hooks.Item.InvokeMechSpawn(result: false, x, y, type, num, num2, num3);
            }
            return Hooks.Item.InvokeMechSpawn(result: true, x, y, type, num, num2, num3);
        }
    }
}
