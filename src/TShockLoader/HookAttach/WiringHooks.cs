using Terraria;

namespace tShockLoader.HookAttach
{
    public static class WiringHooks
    {
        public static void Attach()
        {
            On_Wiring.HitSwitch += On_Wiring_HitSwitch;
        }

        public static void Detach()
        {
            On_Wiring.HitSwitch -= On_Wiring_HitSwitch;
        }

        private static void On_Wiring_HitSwitch(On_Wiring.orig_HitSwitch orig, int i, int j)
        {
            if (OTAPI.Hooks.Wiring.InvokeAnnouncementBox(i, j, -1))
            {
                orig(i, j);
            }
        }
    }
}
