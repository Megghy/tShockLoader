using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.NetModules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Net;

namespace TShockAPI
{
	public class PlayerData
	{
		public NetItem[] inventory = new NetItem[NetItem.MaxInventory];
		public int health = TShock.ServerSideCharacterConfig.Settings.StartingHealth;
		public int maxHealth = TShock.ServerSideCharacterConfig.Settings.StartingHealth;
		public int mana = TShock.ServerSideCharacterConfig.Settings.StartingMana;
		public int maxMana = TShock.ServerSideCharacterConfig.Settings.StartingMana;
		public bool exists;
		public int spawnX = -1;
		public int spawnY = -1;
		public int? extraSlot;
		public int? skinVariant;
		public int? hair;
		public byte hairDye;
		public Color? hairColor;
		public Color? pantsColor;
		public Color? shirtColor;
		public Color? underShirtColor;
		public Color? shoeColor;
		public Color? skinColor;
		public Color? eyeColor;
		public bool[] hideVisuals;
		public int questsCompleted;
		public int usingBiomeTorches;
		public int happyFunTorchTime;
		public int unlockedBiomeTorches;
		public int currentLoadoutIndex;
		public int ateArtisanBread;
		public int usedAegisCrystal;
		public int usedAegisFruit;
		public int usedArcaneCrystal;
		public int usedGalaxyPearl;
		public int usedGummyWorm;
		public int usedAmbrosia;
		public int unlockedSuperCart;
		public int enabledSuperCart;
		byte[] plrData = [];
		byte[] tplrData = [];
		bool snapshotDirty;

		[Obsolete("The player argument is not used.")]
		public PlayerData(TSPlayer player) : this(true) { }

		public PlayerData(bool includingStarterInventory = true)
		{
			for (int i = 0; i < NetItem.MaxInventory; i++)
				inventory[i] = new NetItem();

			if (!includingStarterInventory)
				return;

			for (int i = 0; i < TShock.ServerSideCharacterConfig.Settings.StartingInventory.Count; i++)
			{
				var item = TShock.ServerSideCharacterConfig.Settings.StartingInventory[i];
				StoreSlot(i, item.NetId, item.PrefixId, item.Stack);
			}
		}

		public void StoreSlot(int slot, int netID, int prefix, int stack)
			=> StoreSlot(slot, new NetItem(netID, stack, prefix));

		public void StoreSlot(int slot, NetItem item)
		{
			if ((uint)slot >= inventory.Length)
				return;
			inventory[slot] = item;
			snapshotDirty = true;
		}

		public void CopyCharacter(TSPlayer player) => Capture(player.TPlayer);

		public void Capture(Player t)
		{
			CopyStats(t);
			PlayerSnapshot.Save(t, out plrData, out tplrData);
			snapshotDirty = false;
			FillFlatten(t);
		}

		public Player Materialize()
		{
			EnsureSnapshot();
			return PlayerSnapshot.Load(plrData, tplrData);
		}

		public void RestoreCharacter(TSPlayer player)
		{
			player.IgnoreSSCPackets = true;
			var loaded = HasSnapshot ? Materialize() : BuildLegacyPlayer();
			loaded.whoAmI = player.Index;
			loaded.name = player.TPlayer.name;
			Main.player[player.Index] = loaded;

			NetMessage.SendData((int)PacketTypes.SyncLoadout, remoteClient: player.Index, number: player.Index, number2: loaded.CurrentLoadoutIndex);
			NetMessage.SendData((int)PacketTypes.SyncLoadout, ignoreClient: player.Index, number: player.Index, number2: loaded.CurrentLoadoutIndex);
			SendEquipment(player, -1, -1);
			SendPlayerState(player, -1);
			SendEquipment(player, player.Index, -1);
			SendPlayerState(player, player.Index);

			for (int k = 0; k < Player.MaxBuffs; k++)
				loaded.buffType[k] = 0;
			NetMessage.SendData(MessageID.PlayerBuffs, -1, -1, NetworkText.Empty, player.Index);
			NetMessage.SendData(MessageID.PlayerBuffs, player.Index, -1, NetworkText.Empty, player.Index);
			NetMessage.SendData(MessageID.QuestsCountSync, player.Index, -1, NetworkText.Empty, player.Index);
			NetMessage.SendData(MessageID.QuestsCountSync, -1, -1, NetworkText.Empty, player.Index);
			NetMessage.SendData(MessageID.ReleaseItemOwnership, player.Index, -1, NetworkText.Empty, 400);

			if (!Main.GameModeInfo.IsJourneyMode)
				return;

			var sacrificedItems = TShock.ResearchDatastore.GetSacrificedItems();
			for (int i = 0; i < ContentIds.Items; i++)
			{
				sacrificedItems.TryGetValue(i, out var amount);
				NetManager.Instance.SendToClient(NetCreativeUnlocksModule.SerializeItemSacrifice(i, amount), player.Index);
			}
		}

		public void LoadSnapshot(byte[] plr, byte[] tplr)
		{
			plrData = plr ?? [];
			tplrData = tplr ?? [];
			snapshotDirty = false;
			if (!HasSnapshot)
				return;
			var loaded = PlayerSnapshot.Load(plrData, tplrData);
			CopyStats(loaded);
			FillFlatten(loaded);
		}

		public void LoadInventoryText(string text)
		{
			var items = text.Split('~').Select(NetItem.Parse).ToList();
			if (items.Count < NetItem.MaxInventory)
			{
				items.InsertRange(67, new NetItem[2]);
				items.InsertRange(77, new NetItem[2]);
				items.InsertRange(87, new NetItem[2]);
				items.AddRange(new NetItem[NetItem.MaxInventory - items.Count]);
			}
			inventory = items.Take(NetItem.MaxInventory).ToArray();
			snapshotDirty = true;
		}

		public byte[] PlrData
		{
			get
			{
				EnsureSnapshot();
				return plrData;
			}
		}

		public byte[] TplrData
		{
			get
			{
				EnsureSnapshot();
				return tplrData;
			}
		}

		bool HasSnapshot => plrData is { Length: > 0 } && tplrData is { Length: > 0 };

		void EnsureSnapshot()
		{
			if (HasSnapshot && !snapshotDirty)
				return;
			var player = HasSnapshot ? PlayerSnapshot.Load(plrData, tplrData) : new Player();
			ApplyStats(player);
			ApplyFlatten(player);
			Capture(player);
		}

		void CopyStats(Player t)
		{
			health = t.statLife > 0 ? t.statLife : 1;
			maxHealth = t.statLifeMax;
			mana = t.statMana;
			maxMana = t.statManaMax;
			spawnX = t.SpawnX;
			spawnY = t.SpawnY;
			extraSlot = t.extraAccessory ? 1 : 0;
			skinVariant = t.skinVariant;
			hair = t.hair;
			hairDye = (byte)t.hairDye;
			hairColor = t.hairColor;
			pantsColor = t.pantsColor;
			shirtColor = t.shirtColor;
			underShirtColor = t.underShirtColor;
			shoeColor = t.shoeColor;
			hideVisuals = t.hideVisibleAccessory;
			skinColor = t.skinColor;
			eyeColor = t.eyeColor;
			questsCompleted = t.anglerQuestsFinished;
			usingBiomeTorches = t.UsingBiomeTorches ? 1 : 0;
			happyFunTorchTime = t.happyFunTorchTime ? 1 : 0;
			unlockedBiomeTorches = t.unlockedBiomeTorches ? 1 : 0;
			currentLoadoutIndex = t.CurrentLoadoutIndex;
			ateArtisanBread = t.ateArtisanBread ? 1 : 0;
			usedAegisCrystal = t.usedAegisCrystal ? 1 : 0;
			usedAegisFruit = t.usedAegisFruit ? 1 : 0;
			usedArcaneCrystal = t.usedArcaneCrystal ? 1 : 0;
			usedGalaxyPearl = t.usedGalaxyPearl ? 1 : 0;
			usedGummyWorm = t.usedGummyWorm ? 1 : 0;
			usedAmbrosia = t.usedAmbrosia ? 1 : 0;
			unlockedSuperCart = t.unlockedSuperCart ? 1 : 0;
			enabledSuperCart = t.enabledSuperCart ? 1 : 0;
		}

		void ApplyStats(Player t)
		{
			t.statLife = health;
			t.statLifeMax = maxHealth;
			t.statMana = mana;
			t.statManaMax = maxMana;
			t.SpawnX = spawnX;
			t.SpawnY = spawnY;
			t.hairDye = hairDye;
			t.anglerQuestsFinished = questsCompleted;
			t.UsingBiomeTorches = usingBiomeTorches == 1;
			t.happyFunTorchTime = happyFunTorchTime == 1;
			t.unlockedBiomeTorches = unlockedBiomeTorches == 1;
			t.CurrentLoadoutIndex = currentLoadoutIndex;
			t.ateArtisanBread = ateArtisanBread == 1;
			t.usedAegisCrystal = usedAegisCrystal == 1;
			t.usedAegisFruit = usedAegisFruit == 1;
			t.usedArcaneCrystal = usedArcaneCrystal == 1;
			t.usedGalaxyPearl = usedGalaxyPearl == 1;
			t.usedGummyWorm = usedGummyWorm == 1;
			t.usedAmbrosia = usedAmbrosia == 1;
			t.unlockedSuperCart = unlockedSuperCart == 1;
			t.enabledSuperCart = enabledSuperCart == 1;
			if (extraSlot != null)
				t.extraAccessory = extraSlot.Value == 1;
			if (skinVariant != null)
				t.skinVariant = skinVariant.Value;
			if (hair != null)
				t.hair = hair.Value;
			if (hairColor != null)
				t.hairColor = hairColor.Value;
			if (pantsColor != null)
				t.pantsColor = pantsColor.Value;
			if (shirtColor != null)
				t.shirtColor = shirtColor.Value;
			if (underShirtColor != null)
				t.underShirtColor = underShirtColor.Value;
			if (shoeColor != null)
				t.shoeColor = shoeColor.Value;
			if (skinColor != null)
				t.skinColor = skinColor.Value;
			if (eyeColor != null)
				t.eyeColor = eyeColor.Value;
			t.hideVisibleAccessory = hideVisuals ?? new bool[t.hideVisibleAccessory.Length];
		}

		Player BuildLegacyPlayer()
		{
			var player = new Player();
			ApplyStats(player);
			ApplyFlatten(player);
			return player;
		}

		void FillFlatten(Player t)
		{
			inventory = new NetItem[NetItem.MaxInventory];
			CopyRange(t.inventory, NetItem.InventoryIndex);
			CopyRange(t.armor, NetItem.ArmorIndex);
			CopyRange(t.dye, NetItem.DyeIndex);
			CopyRange(t.miscEquips, NetItem.MiscEquipIndex);
			CopyRange(t.miscDyes, NetItem.MiscDyeIndex);
			CopyRange(t.bank.item, NetItem.PiggyIndex);
			CopyRange(t.bank2.item, NetItem.SafeIndex);
			CopyRange(t.bank3.item, NetItem.ForgeIndex);
			CopyRange(t.bank4.item, NetItem.VoidIndex);
			if (NetItem.TrashIndex.Item1 < inventory.Length)
				inventory[NetItem.TrashIndex.Item1] = new NetItem(t.trashItem);
			CopyRange(t.Loadouts[0].Armor, NetItem.Loadout1Armor);
			CopyRange(t.Loadouts[0].Dye, NetItem.Loadout1Dye);
			CopyRange(t.Loadouts[1].Armor, NetItem.Loadout2Armor);
			CopyRange(t.Loadouts[1].Dye, NetItem.Loadout2Dye);
			CopyRange(t.Loadouts[2].Armor, NetItem.Loadout3Armor);
			CopyRange(t.Loadouts[2].Dye, NetItem.Loadout3Dye);
		}

		void ApplyFlatten(Player t)
		{
			ApplyRange(t.inventory, NetItem.InventoryIndex);
			ApplyRange(t.armor, NetItem.ArmorIndex);
			ApplyRange(t.dye, NetItem.DyeIndex);
			ApplyRange(t.miscEquips, NetItem.MiscEquipIndex);
			ApplyRange(t.miscDyes, NetItem.MiscDyeIndex);
			ApplyRange(t.bank.item, NetItem.PiggyIndex);
			ApplyRange(t.bank2.item, NetItem.SafeIndex);
			ApplyRange(t.bank3.item, NetItem.ForgeIndex);
			ApplyRange(t.bank4.item, NetItem.VoidIndex);
			if (NetItem.TrashIndex.Item1 < inventory.Length)
				t.trashItem = inventory[NetItem.TrashIndex.Item1].ToItem();
			ApplyRange(t.Loadouts[0].Armor, NetItem.Loadout1Armor);
			ApplyRange(t.Loadouts[0].Dye, NetItem.Loadout1Dye);
			ApplyRange(t.Loadouts[1].Armor, NetItem.Loadout2Armor);
			ApplyRange(t.Loadouts[1].Dye, NetItem.Loadout2Dye);
			ApplyRange(t.Loadouts[2].Armor, NetItem.Loadout3Armor);
			ApplyRange(t.Loadouts[2].Dye, NetItem.Loadout3Dye);
		}

		void CopyRange(Item[] items, Tuple<int, int> range)
		{
			int n = Math.Min(items.Length, range.Item2 - range.Item1);
			for (int i = 0; i < n; i++)
			{
				int slot = range.Item1 + i;
				if ((uint)slot < inventory.Length)
					inventory[slot] = new NetItem(items[i]);
			}
		}

		void ApplyRange(Item[] dest, Tuple<int, int> range)
		{
			int n = Math.Min(dest.Length, range.Item2 - range.Item1);
			for (int i = 0; i < n; i++)
			{
				int slot = range.Item1 + i;
				if ((uint)slot < inventory.Length)
					dest[i] = inventory[slot].ToItem();
			}
		}

		static void SendEquipment(TSPlayer player, int remoteClient, int ignoreClient)
		{
			var t = player.TPlayer;
			float slot = 0;
			SendSlots(player.Index, remoteClient, ignoreClient, t.inventory, ref slot);
			SendSlots(player.Index, remoteClient, ignoreClient, t.armor, ref slot);
			SendSlots(player.Index, remoteClient, ignoreClient, t.dye, ref slot);
			SendSlots(player.Index, remoteClient, ignoreClient, t.miscEquips, ref slot);
			SendSlots(player.Index, remoteClient, ignoreClient, t.miscDyes, ref slot);
			SendSlots(player.Index, remoteClient, ignoreClient, t.bank.item, ref slot);
			SendSlots(player.Index, remoteClient, ignoreClient, t.bank2.item, ref slot);
			SendSlot(player.Index, remoteClient, ignoreClient, t.trashItem, ref slot);
			SendSlots(player.Index, remoteClient, ignoreClient, t.bank3.item, ref slot);
			SendSlots(player.Index, remoteClient, ignoreClient, t.bank4.item, ref slot);
			for (int i = 0; i < t.Loadouts.Length; i++)
			{
				SendSlots(player.Index, remoteClient, ignoreClient, t.Loadouts[i].Armor, ref slot);
				SendSlots(player.Index, remoteClient, ignoreClient, t.Loadouts[i].Dye, ref slot);
			}
		}

		static void SendSlots(int player, int remoteClient, int ignoreClient, Item[] items, ref float slot)
		{
			foreach (var item in items)
				SendSlot(player, remoteClient, ignoreClient, item, ref slot);
		}

		static void SendSlot(int player, int remoteClient, int ignoreClient, Item item, ref float slot)
		{
			NetMessage.SendData(MessageID.SyncEquipment, remoteClient, ignoreClient, NetworkText.FromLiteral(item.Name), player, slot, item.prefix);
			slot++;
		}

		static void SendPlayerState(TSPlayer player, int remoteClient)
		{
			NetMessage.SendData(MessageID.SyncPlayer, remoteClient, -1, NetworkText.FromLiteral(player.Name), player.Index);
			NetMessage.SendData(MessageID.PlayerMana, remoteClient, -1, NetworkText.Empty, player.Index);
			NetMessage.SendData(MessageID.PlayerLifeMana, remoteClient, -1, NetworkText.Empty, player.Index);
		}
	}
}
