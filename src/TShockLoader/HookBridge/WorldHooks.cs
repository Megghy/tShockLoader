using System.Reflection;
using MonoMod.RuntimeDetour;
using OTAPI;
using Terraria;

namespace TerrariaApi.Server.Hooking;

internal static class WorldHooks
{
	private static HookManager _hookManager;
	static Hook SaveWorldHook;

	/// <summary>
	/// Attaches any of the OTAPI World hooks to the existing <see cref="HookManager"/> implementation
	/// </summary>
	/// <param name="hookManager">HookManager instance which will receive the events</param>
	public static void AttachTo(HookManager hookManager)
	{
		_hookManager = hookManager;

		//HookEvents.Terraria.IO.WorldFile.SaveWorld_Boolean_Boolean += WorldFile_SaveWorld;
		SaveWorldHook = new Hook(
			HookMethods.Require(typeof(Terraria.IO.WorldFile), "SaveWorld", BindingFlags.Static | BindingFlags.Public, typeof(bool), typeof(bool)),
			HookMethods.Require(typeof(WorldHooks), nameof(WorldFile_SaveWorld), BindingFlags.Static | BindingFlags.NonPublic));
		//HookEvents.Terraria.WorldGen.StartHardmode += WorldGen_StartHardmode;
		On_WorldGen.StartHardmode += WorldGen_StartHardmode;
		//Terraria.WorldGen.SpreadGrass += WorldGen_SpreadGrass;
		On_WorldGen.SpreadGrass += WorldGen_SpreadGrass;
		//Terraria.Main.checkXMas += Main_checkXMas;
		On_Main.checkXMas += Main_checkXMas;
		//Terraria.Main.checkHalloween += Main_checkHalloween;
		On_Main.checkHalloween += Main_checkHalloween;

		Hooks.Collision.PressurePlate += OnPressurePlate;
		Hooks.WorldGen.Meteor += OnDropMeteor;
	}

	public static void Detach()
	{
		SaveWorldHook?.Dispose();
		SaveWorldHook = null;
		On_WorldGen.StartHardmode -= WorldGen_StartHardmode;
		On_WorldGen.SpreadGrass -= WorldGen_SpreadGrass;
		On_Main.checkXMas -= Main_checkXMas;
		On_Main.checkHalloween -= Main_checkHalloween;
		Hooks.Collision.PressurePlate -= OnPressurePlate;
		Hooks.WorldGen.Meteor -= OnDropMeteor;
	}

	static void OnPressurePlate(object sender, Hooks.Collision.PressurePlateEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		if (e.Entity is NPC npc)
		{
			if (_hookManager.InvokeNpcTriggerPressurePlate(npc, e.X, e.Y))
				e.Result = HookResult.Cancel;
		}
		else if (e.Entity is Player player)
		{
			if (_hookManager.InvokePlayerTriggerPressurePlate(player, e.X, e.Y))
				e.Result = HookResult.Cancel;
		}
		else if (e.Entity is Projectile projectile)
		{
			if (_hookManager.InvokeProjectileTriggerPressurePlate(projectile, e.X, e.Y))
				e.Result = HookResult.Cancel;
		}
	}
  delegate void orig_SaveWorld(bool useCloudSaving, bool resetTime = false);
	static void WorldFile_SaveWorld(orig_SaveWorld orig, bool useCloudSaving, bool resetTime = false)
	{
		if (!_hookManager.InvokeWorldSave(resetTime))
			orig(useCloudSaving, resetTime);
	}

	private static void WorldGen_StartHardmode(On_WorldGen.orig_StartHardmode orig)
	{
		if (!_hookManager.InvokeWorldStartHardMode())
			orig();
	}

	static void OnDropMeteor(object sender, Hooks.WorldGen.MeteorEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		if (_hookManager.InvokeWorldMeteorDrop(e.X, e.Y))
		{
			e.Result = HookResult.Cancel;
		}
	}

	private static void Main_checkXMas(On_Main.orig_checkXMas orig)
	{
		if (!_hookManager.InvokeWorldChristmasCheck(ref Main.xMas))
			orig();
	}

	private static void Main_checkHalloween(On_Main.orig_checkHalloween orig)
	{
		if (!_hookManager.InvokeWorldHalloweenCheck(ref Main.halloween))
			orig();
	}

	private static void WorldGen_SpreadGrass(On_WorldGen.orig_SpreadGrass orig, int i, int j, int dirt = 0, int grass = 2, bool repeat = true, TileColorCache color = default)
	{
		if (!_hookManager.InvokeWorldGrassSpread(i, j, dirt, grass, repeat, color))
			orig(i, j, dirt, grass, repeat, color);
	}
}
