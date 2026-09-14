namespace tShockLoader.HookAttach
{
    public static class HookManager
    {
        public static void AttachAll()
        {
            TileHooks.Attach();
            MainHooks.Attach();
            NPCHooks.Attach();
            WorldGenHooks.Attach();
            NetMessageHooks.Attach();
            WiringHooks.Attach();
            MessageBufferHooks.Attach();
            ItemHooks.Attach();
            NetplayHooks.Attach();
            CollisionHooks.Attach();
            ChestHooks.Attach();
        }

        public static void DetachAll()
        {
            TileHooks.Detach();
            MainHooks.Detach();
            NPCHooks.Detach();
            WorldGenHooks.Detach();
            NetMessageHooks.Detach();
            WiringHooks.Detach();
            MessageBufferHooks.Detach();
            ItemHooks.Detach();
            NetplayHooks.Detach();
            CollisionHooks.Detach();
            ChestHooks.Detach();
        }
    }
}
