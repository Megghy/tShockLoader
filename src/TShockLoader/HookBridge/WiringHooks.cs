using System.Reflection;
using OTAPI;
using Terraria;

namespace TerrariaApi.Server.Hooking;

internal static class WiringHooks
{
	private static HookManager _hookManager;

	/// <summary>
	/// Attaches any of the OTAPI Wiring hooks to the existing <see cref="HookManager"/> implementation
	/// </summary>
	/// <param name="hookManager">HookManager instance which will receive the events</param>
	public static void AttachTo(HookManager hookManager)
	{
		_hookManager = hookManager;

		Hooks.Wiring.AnnouncementBox += OnAnnouncementBox;
	}

	public static void Detach()
	{
		Hooks.Wiring.AnnouncementBox -= OnAnnouncementBox;
	}

	static void OnAnnouncementBox(object sender, Hooks.Wiring.AnnouncementBoxEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		var fieldInfo = typeof(Wiring).GetField("CurrentUser", BindingFlags.Static | BindingFlags.NonPublic);
		if (_hookManager.InvokeWireTriggerAnnouncementBox((int)fieldInfo.GetValue(null), e.X, e.Y, e.SignId, Main.sign[e.SignId].text))
		{
			e.Result = HookResult.Cancel;
		}
	}
}
