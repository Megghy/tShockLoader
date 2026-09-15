namespace tShockLoader.HookAttach
{
    public static class HookManager
    {
        public static void AttachAll()
        {
            NPCHooks.Attach();
            WorldGenHooks.Attach();
            NetMessageHooks.Attach();
            WiringHooks.Attach();
            MessageBufferHooks.Attach();
            ItemHooks.Attach();
            ChestHooks.Attach();
        }

        public static void DetachAll()
        {
            ChestHooks.Detach();
            ItemHooks.Detach();
            MessageBufferHooks.Detach();
            WiringHooks.Detach();
            NetMessageHooks.Detach();
            WorldGenHooks.Detach();
            NPCHooks.Detach();
        }
    }
}
