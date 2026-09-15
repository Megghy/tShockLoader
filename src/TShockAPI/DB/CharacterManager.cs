/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/

using System;
using System.Data;
using MySqlConnector;
using TShockAPI.DB.Queries;

namespace TShockAPI.DB
{
	public class CharacterManager
	{
		public IDbConnection database;

		public CharacterManager(IDbConnection db)
		{
			database = db;
			var table = new SqlTable("tsCharacter",
				new SqlColumn("Account", MySqlDbType.Int32) { Primary = true },
				new SqlColumn("Health", MySqlDbType.Int32),
				new SqlColumn("MaxHealth", MySqlDbType.Int32),
				new SqlColumn("Mana", MySqlDbType.Int32),
				new SqlColumn("MaxMana", MySqlDbType.Int32),
				new SqlColumn("Inventory", MySqlDbType.Text),
				new SqlColumn("PlrData", MySqlDbType.Blob),
				new SqlColumn("TplrData", MySqlDbType.Blob),
				new SqlColumn("extraSlot", MySqlDbType.Int32),
				new SqlColumn("spawnX", MySqlDbType.Int32),
				new SqlColumn("spawnY", MySqlDbType.Int32),
				new SqlColumn("skinVariant", MySqlDbType.Int32),
				new SqlColumn("hair", MySqlDbType.Int32),
				new SqlColumn("hairDye", MySqlDbType.Int32),
				new SqlColumn("hairColor", MySqlDbType.Int32),
				new SqlColumn("pantsColor", MySqlDbType.Int32),
				new SqlColumn("shirtColor", MySqlDbType.Int32),
				new SqlColumn("underShirtColor", MySqlDbType.Int32),
				new SqlColumn("shoeColor", MySqlDbType.Int32),
				new SqlColumn("hideVisuals", MySqlDbType.Int32),
				new SqlColumn("skinColor", MySqlDbType.Int32),
				new SqlColumn("eyeColor", MySqlDbType.Int32),
				new SqlColumn("questsCompleted", MySqlDbType.Int32),
				new SqlColumn("usingBiomeTorches", MySqlDbType.Int32),
				new SqlColumn("happyFunTorchTime", MySqlDbType.Int32),
				new SqlColumn("unlockedBiomeTorches", MySqlDbType.Int32),
				new SqlColumn("currentLoadoutIndex", MySqlDbType.Int32),
				new SqlColumn("ateArtisanBread", MySqlDbType.Int32),
				new SqlColumn("usedAegisCrystal", MySqlDbType.Int32),
				new SqlColumn("usedAegisFruit", MySqlDbType.Int32),
				new SqlColumn("usedArcaneCrystal", MySqlDbType.Int32),
				new SqlColumn("usedGalaxyPearl", MySqlDbType.Int32),
				new SqlColumn("usedGummyWorm", MySqlDbType.Int32),
				new SqlColumn("usedAmbrosia", MySqlDbType.Int32),
				new SqlColumn("unlockedSuperCart", MySqlDbType.Int32),
				new SqlColumn("enabledSuperCart", MySqlDbType.Int32)
			);
			new SqlTableCreator(db, db.GetSqlQueryBuilder()).EnsureTableStructure(table);
		}

		public PlayerData GetPlayerData(TSPlayer player, int acctid)
		{
			var playerData = new PlayerData(true);
			try
			{
				using var reader = database.QueryReader("SELECT * FROM tsCharacter WHERE Account=@0", acctid);
				if (!reader.Read())
					return playerData;

				playerData.exists = true;
				playerData.health = reader.Get<int>("Health");
				playerData.maxHealth = reader.Get<int>("MaxHealth");
				playerData.mana = reader.Get<int>("Mana");
				playerData.maxMana = reader.Get<int>("MaxMana");
				playerData.extraSlot = reader.Get<int>("extraSlot");
				playerData.spawnX = reader.Get<int>("spawnX");
				playerData.spawnY = reader.Get<int>("spawnY");
				playerData.skinVariant = reader.Get<int?>("skinVariant");
				playerData.hair = reader.Get<int?>("hair");
				playerData.hairDye = (byte)reader.Get<int>("hairDye");
				playerData.hairColor = TShock.Utils.DecodeColor(reader.Get<int?>("hairColor"));
				playerData.pantsColor = TShock.Utils.DecodeColor(reader.Get<int?>("pantsColor"));
				playerData.shirtColor = TShock.Utils.DecodeColor(reader.Get<int?>("shirtColor"));
				playerData.underShirtColor = TShock.Utils.DecodeColor(reader.Get<int?>("underShirtColor"));
				playerData.shoeColor = TShock.Utils.DecodeColor(reader.Get<int?>("shoeColor"));
				playerData.hideVisuals = TShock.Utils.DecodeBoolArray(reader.Get<int?>("hideVisuals"));
				playerData.skinColor = TShock.Utils.DecodeColor(reader.Get<int?>("skinColor"));
				playerData.eyeColor = TShock.Utils.DecodeColor(reader.Get<int?>("eyeColor"));
				playerData.questsCompleted = reader.Get<int>("questsCompleted");
				playerData.usingBiomeTorches = reader.Get<int>("usingBiomeTorches");
				playerData.happyFunTorchTime = reader.Get<int>("happyFunTorchTime");
				playerData.unlockedBiomeTorches = reader.Get<int>("unlockedBiomeTorches");
				playerData.currentLoadoutIndex = reader.Get<int>("currentLoadoutIndex");
				playerData.ateArtisanBread = reader.Get<int>("ateArtisanBread");
				playerData.usedAegisCrystal = reader.Get<int>("usedAegisCrystal");
				playerData.usedAegisFruit = reader.Get<int>("usedAegisFruit");
				playerData.usedArcaneCrystal = reader.Get<int>("usedArcaneCrystal");
				playerData.usedGalaxyPearl = reader.Get<int>("usedGalaxyPearl");
				playerData.usedGummyWorm = reader.Get<int>("usedGummyWorm");
				playerData.usedAmbrosia = reader.Get<int>("usedAmbrosia");
				playerData.unlockedSuperCart = reader.Get<int>("unlockedSuperCart");
				playerData.enabledSuperCart = reader.Get<int>("enabledSuperCart");
				var plr = reader.Get<byte[]>("PlrData");
				var tplr = reader.Get<byte[]>("TplrData");
				if (plr is { Length: > 0 } && tplr is { Length: > 0 })
					playerData.LoadSnapshot(plr, tplr);
				else
					playerData.LoadInventoryText(reader.Get<string>("Inventory") ?? "");
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return playerData;
		}

		public bool SeedInitialData(UserAccount account)
		{
			var data = new PlayerData(true)
			{
				health = TShock.ServerSideCharacterConfig.Settings.StartingHealth,
				maxHealth = TShock.ServerSideCharacterConfig.Settings.StartingHealth,
				mana = TShock.ServerSideCharacterConfig.Settings.StartingMana,
				maxMana = TShock.ServerSideCharacterConfig.Settings.StartingMana,
				spawnX = -1,
				spawnY = -1,
			};
			return Upsert(account.ID, data);
		}

		public bool InsertPlayerData(TSPlayer player, bool fromCommand = false)
		{
			if (!player.IsLoggedIn)
				return false;
			if (player.State < (int)ConnectionState.Complete)
				return false;
			if (player.HasPermission(Permissions.bypassssc) && !fromCommand)
			{
				TShock.Log.ConsoleInfo(GetParticularString("{0} is a player name", $"Skipping SSC save (due to tshock.ignore.ssc) for {player.Account.Name}"));
				return false;
			}

			player.PlayerData.CopyCharacter(player);
			return Upsert(player.Account.ID, player.PlayerData);
		}

		public bool RemovePlayer(int userid)
		{
			try
			{
				database.Query("DELETE FROM tsCharacter WHERE Account=@0;", userid);
				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return false;
		}

		public bool InsertSpecificPlayerData(TSPlayer player, PlayerData data)
		{
			if (!player.IsLoggedIn)
				return false;
			if (player.HasPermission(Permissions.bypassssc))
			{
				TShock.Log.ConsoleInfo(GetParticularString("{0} is a player name", $"Skipping SSC save (due to tshock.ignore.ssc) for {player.Account.Name}"));
				return true;
			}

			return Upsert(player.Account.ID, data);
		}

		bool Upsert(int accountId, PlayerData data)
		{
			var plr = data.PlrData;
			var tplr = data.TplrData;
			try
			{
				if (!GetPlayerData(null, accountId).exists)
				{
					database.Query(
						"INSERT INTO tsCharacter (Account, Health, MaxHealth, Mana, MaxMana, PlrData, extraSlot, spawnX, spawnY, skinVariant, hair, hairDye, hairColor, pantsColor, shirtColor, underShirtColor, shoeColor, hideVisuals, skinColor, eyeColor, questsCompleted, usingBiomeTorches, happyFunTorchTime, unlockedBiomeTorches, currentLoadoutIndex, ateArtisanBread, usedAegisCrystal, usedAegisFruit, usedArcaneCrystal, usedGalaxyPearl, usedGummyWorm, usedAmbrosia, unlockedSuperCart, enabledSuperCart, TplrData) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16, @17, @18, @19, @20, @21, @22, @23, @24, @25, @26, @27, @28, @29, @30, @31, @32, @33);",
						accountId, data.health, data.maxHealth, data.mana, data.maxMana, plr, data.extraSlot,
						data.spawnX, data.spawnY, data.skinVariant, data.hair, data.hairDye,
						TShock.Utils.EncodeColor(data.hairColor), TShock.Utils.EncodeColor(data.pantsColor),
						TShock.Utils.EncodeColor(data.shirtColor), TShock.Utils.EncodeColor(data.underShirtColor),
						TShock.Utils.EncodeColor(data.shoeColor), TShock.Utils.EncodeBoolArray(data.hideVisuals),
						TShock.Utils.EncodeColor(data.skinColor), TShock.Utils.EncodeColor(data.eyeColor),
						data.questsCompleted, data.usingBiomeTorches, data.happyFunTorchTime, data.unlockedBiomeTorches,
						data.currentLoadoutIndex, data.ateArtisanBread, data.usedAegisCrystal, data.usedAegisFruit,
						data.usedArcaneCrystal, data.usedGalaxyPearl, data.usedGummyWorm, data.usedAmbrosia,
						data.unlockedSuperCart, data.enabledSuperCart, tplr);
				}
				else
				{
					database.Query(
						"UPDATE tsCharacter SET Health=@0, MaxHealth=@1, Mana=@2, MaxMana=@3, PlrData=@4, extraSlot=@5, spawnX=@6, spawnY=@7, skinVariant=@8, hair=@9, hairDye=@10, hairColor=@11, pantsColor=@12, shirtColor=@13, underShirtColor=@14, shoeColor=@15, hideVisuals=@16, skinColor=@17, eyeColor=@18, questsCompleted=@19, usingBiomeTorches=@20, happyFunTorchTime=@21, unlockedBiomeTorches=@22, currentLoadoutIndex=@23, ateArtisanBread=@24, usedAegisCrystal=@25, usedAegisFruit=@26, usedArcaneCrystal=@27, usedGalaxyPearl=@28, usedGummyWorm=@29, usedAmbrosia=@30, unlockedSuperCart=@31, enabledSuperCart=@32, TplrData=@33 WHERE Account=@34;",
						data.health, data.maxHealth, data.mana, data.maxMana, plr, data.extraSlot ?? 0,
						data.spawnX, data.spawnY, data.skinVariant, data.hair, data.hairDye,
						TShock.Utils.EncodeColor(data.hairColor), TShock.Utils.EncodeColor(data.pantsColor),
						TShock.Utils.EncodeColor(data.shirtColor), TShock.Utils.EncodeColor(data.underShirtColor),
						TShock.Utils.EncodeColor(data.shoeColor), TShock.Utils.EncodeBoolArray(data.hideVisuals),
						TShock.Utils.EncodeColor(data.skinColor), TShock.Utils.EncodeColor(data.eyeColor),
						data.questsCompleted, data.usingBiomeTorches, data.happyFunTorchTime, data.unlockedBiomeTorches,
						data.currentLoadoutIndex, data.ateArtisanBread, data.usedAegisCrystal, data.usedAegisFruit,
						data.usedArcaneCrystal, data.usedGalaxyPearl, data.usedGummyWorm, data.usedAmbrosia,
						data.unlockedSuperCart, data.enabledSuperCart, tplr, accountId);
				}

				return true;
			}
			catch (Exception ex)
			{
				TShock.Log.Error(ex.ToString());
			}

			return false;
		}
	}
}
