using System.Reflection;
using MonoMod.RuntimeDetour;
using OTAPI;
using Terraria;
using Terraria.ModLoader;

namespace TerrariaApi.Server.Hooking;

internal static class ServerHooks
{
	private static HookManager _hookManager;
	static Hook? ExecuteCommandHook;

	/// <summary>
	/// Attaches any of the OTAPI Server hooks to the existing <see cref="HookManager"/> implementation
	/// </summary>
	/// <param name="hookManager">HookManager instance which will receive the events</param>
	public static void AttachTo(HookManager hookManager)
	{
		_hookManager = hookManager;

		//HookEvents.Terraria.Main.startDedInput += Main_startDedInput;
		On_Main.startDedInput += Main_startDedInput;
		On_RemoteClient.Reset += RemoteClient_Reset;
		ExecuteCommandHook = new Hook(
			HookMethods.Require(typeof(Main), nameof(Main.ExecuteCommand), BindingFlags.Static | BindingFlags.Public, typeof(string), typeof(CommandCaller)),
			HookMethods.Require(typeof(ServerHooks), nameof(OnExecuteCommand), BindingFlags.Static | BindingFlags.NonPublic));
		Hooks.Main.CommandProcess += OnProcess;
	}

	public static void Detach()
	{
		On_Main.startDedInput -= Main_startDedInput;
		On_RemoteClient.Reset -= RemoteClient_Reset;
		ExecuteCommandHook?.Dispose();
		ExecuteCommandHook = null;
		Hooks.Main.CommandProcess -= OnProcess;
	}

	delegate void orig_ExecuteCommand(string text, CommandCaller commandCaller);

	static void OnExecuteCommand(orig_ExecuteCommand orig, string text, CommandCaller commandCaller)
	{
		if (commandCaller.CommandType == CommandType.Console
		    && !Hooks.Main.InvokeCommandProcess(text.ToLowerInvariant(), text))
			return;

		orig(text, commandCaller);
	}

	static void Main_startDedInput(On_Main.orig_startDedInput orig)
	{
		if (Environment.GetCommandLineArgs().Any(x => x.Equals("-disable-commands")))
		{
			Console.WriteLine("Command thread has been disabled.");
			return;
		}

		orig();
	}

	static void OnProcess(object sender, Hooks.Main.CommandProcessEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		if (_hookManager.InvokeServerCommand(e.Command))
		{
			e.Result = HookResult.Cancel;
		}
	}

	static void RemoteClient_Reset(On_RemoteClient.orig_Reset orig, RemoteClient client)
	{
		if (!Netplay.Disconnect)
		{
			if (client.IsActive)
			{
				_hookManager.InvokeServerLeave(client.Id);
			}
			_hookManager.InvokeServerSocketReset(client);
		}
	}
}
