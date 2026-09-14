using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tShockLoader.HookAttach
{
    public static class ChestHooks
    {
        public static void Attach()
        {
            On_Chest.PutItemInNearbyChest += On_Chest_QuickStack;
        }

        public static void Detach()
        {
            On_Chest.PutItemInNearbyChest -= On_Chest_QuickStack;
        }
        private static bool IsPlayerInChest(int i)
        {
            for (int j = 0; j < 255; j++)
            {
                if (Main.player[j].chest == i)
                    return true;
            }

            return false;
        }
        private static Item On_Chest_QuickStack(On_Chest.orig_PutItemInNearbyChest orig, Item item, Vector2 position)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return item;

            bool flag = true;
            for (int i = 0; i < 8000; i++)
            {
                if (OTAPI.Hooks.Chest.InvokeQuickStack(-1, item, i))
                {
                    continue;
                } // added
                bool flag2 = false;
                bool flag3 = false;
                if (Main.chest[i] == null || IsPlayerInChest(i) || Chest.IsLocked(Main.chest[i].x, Main.chest[i].y))
                    continue;

                Vector2 vector = new(Main.chest[i].x * 16 + 16, Main.chest[i].y * 16 + 16);
                if ((vector - position).Length() >= 600f)
                    continue;

                for (int j = 0; j < Main.chest[i].item.Length; j++)
                {
                    if (Main.chest[i].item[j].IsAir)
                    {
                        flag3 = true;
                    }
                    else if (item.type == Main.chest[i].item[j].type)
                    {
                        flag2 = true;
                        // flag set above means "item of same type found in chest, able to be put into an empty slot later", which means we have to respect it before TryStackItems
                        if (!ItemLoader.TryStackItems(Main.chest[i].item[j], item, out int numTransfered))
                            continue;

                        if (numTransfered > 0)
                            Chest.VisualizeChestTransfer(position, vector, item, numTransfered);

                        if (item.stack <= 0)
                        {
                            item.SetDefaults();
                            return item;
                        }

                        /* #OnStackHook
                        int num = Main.chest[i].item[j].maxStack - Main.chest[i].item[j].stack;
                        if (num > 0) {
                            if (num > item.stack)
                                num = item.stack;

                            VisualizeChestTransfer(position, vector, item, num);
                            if (flag) {
                                item.stack -= num;
                                Main.chest[i].item[j].stack += num;
                            }

                            // Extra context.
                            if (item.stack <= 0) {
                                item.SetDefaults();
                                return item;
                            }
                        }
                        */
                    }
                    else
                    {
                        flag3 = true;
                    }
                }

                if (!(flag2 && flag3) || item.stack <= 0)
                    continue;

                for (int k = 0; k < Main.chest[i].item.Length; k++)
                {
                    if (Main.chest[i].item[k].type == 0 || Main.chest[i].item[k].stack == 0)
                    {
                        Chest.VisualizeChestTransfer(position, vector, item, item.stack);
                        if (flag)
                        {
                            Main.chest[i].item[k] = item.Clone();
                            item.SetDefaults();
                        }

                        return item;
                    }
                }
            }

            return item;
        }
    }
}
