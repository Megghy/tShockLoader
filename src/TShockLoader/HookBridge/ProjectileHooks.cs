using Terraria;

namespace TerrariaApi.Server.Hooking;

internal static class ProjectileHooks
{
	private static HookManager _hookManager;

	/// <summary>
	/// Attaches any of the OTAPI Projectile hooks to the existing <see cref="HookManager"/> implementation
	/// </summary>
	/// <param name="hookManager">HookManager instance which will receive the events</param>
	public static void AttachTo(HookManager hookManager)
	{
		_hookManager = hookManager;

		//HookEvents.Terraria.Projectile.SetDefaults += OnSetDefaults;
		On_Projectile.SetDefaults += OnSetDefaults;
		//HookEvents.Terraria.Projectile.AI += OnAI;
		On_Projectile.AI += OnAI;
	}

	private static void OnSetDefaults(On_Projectile.orig_SetDefaults orig, Projectile self, int Type)
	{
		orig(self, Type);
		_hookManager.InvokeProjectileSetDefaults(ref Type, self);
	}

	private static void OnAI(On_Projectile.orig_AI orig, Projectile self)
	{
		if (_hookManager.InvokeProjectileAIUpdate(self))
			orig(self);
	}
}
