using Microsoft.Xna.Framework;
using OTAPI;
using Terraria;
using static Terraria.NPC;

namespace TerrariaApi.Server.Hooking;

internal static class NpcHooks
{
	private static HookManager _hookManager;

	/// <summary>
	/// Attaches any of the OTAPI Npc hooks to the existing <see cref="HookManager"/> implementation
	/// </summary>
	/// <param name="hookManager">HookManager instance which will receive the events</param>
	public static void AttachTo(HookManager hookManager)
	{
		_hookManager = hookManager;

		//Terraria.NPC.SetDefaults += OnSetDefaultsById;
		On_NPC.SetDefaults += OnSetDefaultsById;
		//HookEvents.Terraria.NPC.SetDefaultsFromNetId += OnSetDefaultsFromNetId;
		On_NPC.SetDefaultsFromNetId += OnSetDefaultsFromNetId;
		//HookEvents.Terraria.NPC.StrikeNPC += OnStrike;
		On_NPC.StrikeNPC_HitInfo_bool_bool += OnStrike;
		//HookEvents.Terraria.NPC.Transform += OnTransform;
		On_NPC.Transform += OnTransform;
		//HookEvents.Terraria.NPC.AI += OnAI;
		On_NPC.AI += OnAI;

		Hooks.NPC.Spawn += OnSpawn;
		Hooks.NPC.DropLoot += OnDropLoot;
		Hooks.NPC.BossBag += OnBossBagItem;
		Hooks.NPC.Killed += OnKilled;
	}

	public static void Detach()
	{
		On_NPC.SetDefaults -= OnSetDefaultsById;
		On_NPC.SetDefaultsFromNetId -= OnSetDefaultsFromNetId;
		On_NPC.StrikeNPC_HitInfo_bool_bool -= OnStrike;
		On_NPC.Transform -= OnTransform;
		On_NPC.AI -= OnAI;
		Hooks.NPC.Spawn -= OnSpawn;
		Hooks.NPC.DropLoot -= OnDropLoot;
		Hooks.NPC.BossBag -= OnBossBagItem;
		Hooks.NPC.Killed -= OnKilled;
	}

	static void OnKilled(object sender, Hooks.NPC.KilledEventArgs e)
	{
		_hookManager.InvokeNpcKilled(e.Npc);
	}

	static void OnSetDefaultsById(On_NPC.orig_SetDefaults orig, NPC self, int Type, NPCSpawnParams spawnparams = default(NPCSpawnParams))
	{
		if (!_hookManager.InvokeNpcSetDefaultsInt(ref Type, self))
			orig(self, Type, spawnparams);
	}

	static void OnSetDefaultsFromNetId(On_NPC.orig_SetDefaultsFromNetId orig, NPC self, int Type, NPCSpawnParams spawnparams)
	{
		if (!_hookManager.InvokeNpcNetDefaults(ref Type, self))
			orig(self, Type, spawnparams);
	}

	static int OnStrike(On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig, NPC self, HitInfo hitInfo, bool fromNet = false, bool noPlayerInteraction = false)
	{
		var damage = hitInfo.Damage;
		var knockback = hitInfo.Knockback;
		var hitDirection = hitInfo.HitDirection;
		var crit = hitInfo.Crit;
		if (_hookManager.InvokeNpcStrike(self, ref damage, ref knockback, ref hitDirection, ref crit, ref noPlayerInteraction, ref fromNet, null))
			return 0;

		hitInfo.Damage = damage;
		hitInfo.Knockback = knockback;
		hitInfo.HitDirection = hitDirection;
		hitInfo.Crit = crit;
		return orig(self, hitInfo, fromNet, noPlayerInteraction);
	}

	static void OnTransform(On_NPC.orig_Transform orig, NPC self, int newType)
	{
		if (!_hookManager.InvokeNpcTransformation(self.whoAmI))
			orig(self, newType);
	}

	static void OnSpawn(object sender, Hooks.NPC.SpawnEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		var index = e.Index;
		if (_hookManager.InvokeNpcSpawn(ref index))
		{
			e.Result = HookResult.Cancel;
			e.Index = index;
		}
	}

	static void OnDropLoot(object sender, Hooks.NPC.DropLootEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		if (e.Event == HookEvent.Before)
		{
			var Width = e.Width;
			var Height = e.Height;
			var Type = e.Type;
			var Stack = e.Stack;
			var noBroadcast = e.NoBroadcast;
			var pfix = e.Pfix;
			var noGrabDelay = e.NoGrabDelay;
			var reverseLookup = e.ReverseLookup;

			var position = new Vector2(e.X, e.Y);
			if (_hookManager.InvokeNpcLootDrop
			(
				ref position,
				ref Width,
				ref Height,
				ref Type,
				ref Stack,
				ref noBroadcast,
				ref pfix,
				e.Npc.type,
				e.Npc.whoAmI,
				ref noGrabDelay,
				ref reverseLookup
			))
			{
				e.X = (int)position.X;
				e.Y = (int)position.Y;
				e.Result = HookResult.Cancel;
			}
			e.X = (int)position.X;
			e.Y = (int)position.Y;

			e.Width = Width;
			e.Height = Height;
			e.Type = Type;
			e.Stack = Stack;
			e.NoBroadcast = noBroadcast;
			e.Pfix = pfix;
			e.NoGrabDelay = noGrabDelay;
			e.ReverseLookup = reverseLookup;
		}
	}

	static void OnBossBagItem(object sender, Hooks.NPC.BossBagEventArgs e)
	{
		if (e.Result == HookResult.Cancel)
		{
			return;
		}
		var Width = e.Width;
		var Height = e.Height;
		var Type = e.Type;
		var Stack = e.Stack;
		var noBroadcast = e.NoBroadcast;
		var pfix = e.Pfix;
		var noGrabDelay = e.NoGrabDelay;
		var reverseLookup = e.ReverseLookup;

		var positon = new Vector2(e.X, e.Y);
		if (_hookManager.InvokeDropBossBag
		(
			ref positon,
			ref Width,
			ref Height,
			ref Type,
			ref Stack,
			ref noBroadcast,
			ref pfix,
			e.Npc.type,
			e.Npc.whoAmI,
			ref noGrabDelay,
			ref reverseLookup
		))
		{
			e.Result = HookResult.Cancel;
		}

		e.Width = Width;
		e.Height = Height;
		e.Type = Type;
		e.Stack = Stack;
		e.NoBroadcast = noBroadcast;
		e.Pfix = pfix;
		e.NoGrabDelay = noGrabDelay;
		e.ReverseLookup = reverseLookup;
	}

	static void OnAI(On_NPC.orig_AI orig, NPC self)
	{
		if (!_hookManager.InvokeNpcAIUpdate(self))
			orig(self);
	}
}
