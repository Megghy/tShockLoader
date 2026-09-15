using Microsoft.Xna.Framework;
using System.Reflection;
using MonoMod.RuntimeDetour;
using OTAPI;
using Terraria;
using tShockLoader.Plugins;

namespace TerrariaApi.Server.Hooking;

internal static class GameHooks
{
	private static HookManager _hookManager;

	internal static bool GameInitializeObserved { get; private set; }
	static Hook? youCanSleepNowHook;

	/// <summary>
	/// Attaches any of the OTAPI Game hooks to the existing <see cref="HookManager"/> implementation
	/// </summary>
	/// <param name="hookManager">HookManager instance which will receive the events</param>
	public static void AttachTo(HookManager hookManager)
	{
		_hookManager = hookManager;

		Terraria.On_Main.Update += OnUpdate;
		Main.OnTickForThirdPartySoftwareOnly += OnDedicatedTick;
		Terraria.On_Main.Initialize += OnInitialize;
		Terraria.On_Netplay.StartServer += OnStartServer;

		Hooks.WorldGen.HardmodeTilePlace += OnHardmodeTilePlace;
		Hooks.WorldGen.HardmodeTileUpdate += OnHardmodeTileUpdate;
		Hooks.Item.MechSpawn += OnItemMechSpawn;
		Hooks.NPC.MechSpawn += OnNpcMechSpawn;
		youCanSleepNowHook = new Hook(
			HookMethods.Require(typeof(Main), "YouCanSleepNow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
			OnYouCanSleepNow);
	}

	public static void Detach()
	{
		Terraria.On_Main.Update -= OnUpdate;
		Main.OnTickForThirdPartySoftwareOnly -= OnDedicatedTick;
		Terraria.On_Main.Initialize -= OnInitialize;
		Terraria.On_Netplay.StartServer -= OnStartServer;

		Hooks.WorldGen.HardmodeTilePlace -= OnHardmodeTilePlace;
		Hooks.WorldGen.HardmodeTileUpdate -= OnHardmodeTileUpdate;
		Hooks.Item.MechSpawn -= OnItemMechSpawn;
		Hooks.NPC.MechSpawn -= OnNpcMechSpawn;
		youCanSleepNowHook?.Dispose();
		youCanSleepNowHook = null;
		GameInitializeObserved = false;
	}

	delegate void orig_YouCanSleepNow(Main self);
	static void OnYouCanSleepNow(orig_YouCanSleepNow orig, Main self)
	{
		Console.WriteLine("tShockLoader: YouCanSleepNow");
		try
		{
			PluginLoader.Unload();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"tShockLoader: shutdown unload {ex}");
		}

		Environment.Exit(0);
	}

	private static void OnUpdate(On_Main.orig_Update orig, Main self, GameTime gameTime)
	{
		if (Netplay.Disconnect)
		{
			orig(self, gameTime);
			return;
		}

		_hookManager.InvokeGameUpdate();
		orig(self, gameTime);
		_hookManager.InvokeGamePostUpdate();
	}

	static void OnDedicatedTick()
	{
		if (Netplay.Disconnect || Netplay.HasClients)
			return;
		_hookManager.InvokeGameUpdate();
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
		GameInitializeObserved = true;
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
