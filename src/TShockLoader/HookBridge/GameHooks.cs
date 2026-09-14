using Microsoft.Xna.Framework;
using OTAPI;
using Terraria;

namespace TerrariaApi.Server.Hooking;

internal static class GameHooks
{
	private static HookManager _hookManager;

	/// <summary>
	/// Attaches any of the OTAPI Game hooks to the existing <see cref="HookManager"/> implementation
	/// </summary>
	/// <param name="hookManager">HookManager instance which will receive the events</param>
	public static void AttachTo(HookManager hookManager)
	{
		_hookManager = hookManager;

		//HookEvents.Terraria.Main.Update += OnUpdate;
		Terraria.On_Main.Update += OnUpdate;
		//HookEvents.Terraria.Main.Initialize += OnInitialize;//
		Terraria.On_Main.Initialize += OnInitialize;
		//HookEvents.Terraria.Netplay.StartServer += OnStartServer;
		Terraria.On_Netplay.StartServer += OnStartServer;

		Hooks.WorldGen.HardmodeTilePlace += OnHardmodeTilePlace;
		Hooks.WorldGen.HardmodeTileUpdate += OnHardmodeTileUpdate;
		Hooks.Item.MechSpawn += OnItemMechSpawn;
		Hooks.NPC.MechSpawn += OnNpcMechSpawn;
	}

	private static void OnUpdate(On_Main.orig_Update orig, Main self, GameTime gameTime)
	{
		_hookManager.InvokeGameUpdate();
		orig(self, gameTime);
		_hookManager.InvokeGamePostUpdate();
	}

	private static void OnHardmodeTileUpdate(object sender, Hooks.WorldGen.HardmodeTileUpdateEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		if (_hookManager.InvokeGameHardmodeTileUpdate(e.X, e.Y, e.Type))
		{
			e.Result = HookResult.Cancel;
		}
	}

	private static void OnHardmodeTilePlace(object sender, Hooks.WorldGen.HardmodeTilePlaceEventArgs e)
	{
		if (e.Result == HardmodeTileUpdateResult.Cancel)
		{
			return;
		}
		if (_hookManager.InvokeGameHardmodeTileUpdate(e.X, e.Y, e.Type))
		{
			e.Result = HardmodeTileUpdateResult.Cancel;
		}
	}

	private static void OnInitialize(On_Main.orig_Initialize orig, Main self)
	{
		HookManager.InitialiseAPI();
		_hookManager.InvokeGameInitialize();
		orig(self);
	}

	private static void OnStartServer(On_Netplay.orig_StartServer orig)
	{
		_hookManager.InvokeGamePostInitialize();
		orig();
	}

	private static void OnItemMechSpawn(object sender, Hooks.Item.MechSpawnEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		if (!_hookManager.InvokeGameStatueSpawn(e.Num2, e.Num3, e.Num, (int)(e.X / 16f), (int)(e.Y / 16f), e.Type, false))
		{
			e.Result = HookResult.Cancel;
		}
	}

	private static void OnNpcMechSpawn(object sender, Hooks.NPC.MechSpawnEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		if (!_hookManager.InvokeGameStatueSpawn(e.Num2, e.Num3, e.Num, (int)(e.X / 16f), (int)(e.Y / 16f), e.Type, true))
		{
			e.Result = HookResult.Cancel;
		}
	}
}
