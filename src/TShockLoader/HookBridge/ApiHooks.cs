using TerrariaApi.Server;

namespace TerrariaApi.Server.Hooking;

static class ApiHooks
{
	public static void Attach(HookManager hooks)
	{
		GameHooks.AttachTo(hooks);
		ItemHooks.AttachTo(hooks);
		NetHooks.AttachTo(hooks);
		NpcHooks.AttachTo(hooks);
		ProjectileHooks.AttachTo(hooks);
		ServerHooks.AttachTo(hooks);
		WiringHooks.AttachTo(hooks);
		WorldHooks.AttachTo(hooks);
	}

	public static void Detach()
	{
		WorldHooks.Detach();
		WiringHooks.Detach();
		ServerHooks.Detach();
		ProjectileHooks.Detach();
		NpcHooks.Detach();
		NetHooks.Detach();
		ItemHooks.Detach();
		GameHooks.Detach();
	}
}
