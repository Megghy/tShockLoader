using OTAPI;
using Terraria;

namespace TerrariaApi.Server.Hooking;

internal static class ItemHooks
{
	private static HookManager _hookManager;

	/// <summary>
	/// Attaches any of the OTAPI Item hooks to the existing <see cref="HookManager"/> implementation
	/// </summary>
	/// <param name="hookManager">HookManager instance which will receive the events</param>
	public static void AttachTo(HookManager hookManager)
	{
		_hookManager = hookManager;

		//HookEvents.Terraria.Item.SetDefaults_Int32_Boolean_ItemVariant += OnSetDefaults;
		Terraria.On_Item.SetDefaults_int_bool_ItemVariant += OnSetDefaults;
		//HookEvents.Terraria.Item.netDefaults += OnNetDefaults;
		Terraria.On_Item.netDefaults += OnNetDefaults;

		Hooks.Chest.QuickStack += OnQuickStack;
	}

	private static void OnNetDefaults(On_Item.orig_netDefaults orig, Item self, int type)
	{
		if (!_hookManager.InvokeItemNetDefaults(ref type, self))
			orig(self, type);
	}

	private static void OnSetDefaults(On_Item.orig_SetDefaults_int_bool_ItemVariant orig, Item self, int Type, bool noMatCheck, Terraria.GameContent.Items.ItemVariant varian)
	{
		if (!_hookManager.InvokeItemSetDefaultsInt(ref Type, self, varian))
		{
			orig(self, Type, noMatCheck, varian);
		}
	}

	private static void OnQuickStack(object sender, Hooks.Chest.QuickStackEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		if (_hookManager.InvokeItemForceIntoChest(Main.chest[e.ChestIndex], e.Item, Main.player[e.PlayerId]))
		{
			e.Result = HookResult.Cancel;
		}
	}
}
