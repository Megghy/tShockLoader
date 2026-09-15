/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Initializers;
using Terraria.Localization;
using Terraria.UI.Chat;

namespace TShockAPI.Localization
{
	/// <summary>
	/// Provides a series of methods that give English texts
	/// </summary>
	public static class EnglishLanguage
	{
		private static readonly Dictionary<int, string> ItemNames = new Dictionary<int, string>();

		private static readonly Dictionary<int, string> NpcNames = new Dictionary<int, string>();

		private static readonly Dictionary<int, string> Prefixs = new Dictionary<int, string>();

		private static readonly Dictionary<int, string> Buffs = new Dictionary<int, string>();

		private static readonly Dictionary<string,string> VanillaCommandsPrefixs = new Dictionary<string, string>();

		internal static void Initialize()
		{
			CaptureVanillaCommands();
		}

		public static void RebuildContentNames()
		{
			WithEnglishCulture(() =>
			{
				ItemNames.Clear();
				NpcNames.Clear();
				Prefixs.Clear();
				Buffs.Clear();

				for (var i = -48; i < ContentIds.Items; i++)
					ItemNames[i] = Lang.GetItemNameValue(i) ?? string.Empty;

				for (var i = -17; i < ContentIds.Npcs; i++)
					NpcNames[i] = Lang.GetNPCNameValue(i) ?? string.Empty;

				for (var i = 0; i < ContentIds.Buffs; i++)
					Buffs[i] = Lang.GetBuffName(i) ?? string.Empty;

				var prefixCount = Math.Min(ContentIds.Prefixes, Lang.prefix.Length);
				for (var i = 0; i < prefixCount; i++)
				{
					var text = Lang.prefix[i];
					if (text is not null)
						Prefixs[i] = text.Value;
				}
			});
		}

		static void CaptureVanillaCommands()
		{
			WithEnglishCulture(() =>
			{
				var localizedField = typeof(ChatCommandProcessor).GetField("_localizedCommands", BindingFlags.Instance | BindingFlags.NonPublic)
					?? throw new MissingFieldException(typeof(ChatCommandProcessor).FullName, "_localizedCommands");
				var localized = (Dictionary<LocalizedText, ChatCommandId>)localizedField.GetValue(ChatManager.Commands)!;
				var nameField = typeof(ChatCommandId).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic)
					?? throw new MissingFieldException(typeof(ChatCommandId).FullName, "_name");
				if (localized.Count == 0)
					ChatInitializer.Load();
				foreach (var command in localized)
				{
					var name = (string)nameField.GetValue(command.Value)!;
					if (VanillaCommandsPrefixs.ContainsKey(name))
						continue;
					VanillaCommandsPrefixs.Add(name, command.Key.Value);
				}
				localized.Clear();
			});
		}

		static void WithEnglishCulture(Action action)
		{
			var culture = Language.ActiveCulture;
			var skip = culture == GameCulture.FromCultureName(GameCulture.CultureName.English);
			try
			{
				if (!skip)
					LanguageManager.Instance.SetLanguage(GameCulture.FromCultureName(GameCulture.CultureName.English));
				action();
			}
			finally
			{
				if (!skip)
					LanguageManager.Instance.SetLanguage(culture);
			}
		}

		/// <summary>
		/// Get the english name of an item
		/// </summary>
		/// <param name="id">Id of the item</param>
		/// <returns>Item name in English</returns>
		public static string GetItemNameById(int id)
		{
			string itemName;
			if (ItemNames.TryGetValue(id, out itemName))
				return itemName;

			return null;
		}

		/// <summary>
		/// Get the english name of a npc
		/// </summary>
		/// <param name="id">Id of the npc</param>
		/// <returns>Npc name in English</returns>
		public static string GetNpcNameById(int id)
		{
			string npcName;
			if (NpcNames.TryGetValue(id, out npcName))
				return npcName;

			return null;
		}

		/// <summary>
		/// Get prefix in English
		/// </summary>
		/// <param name="id">Prefix Id</param>
		/// <returns>Prefix in English</returns>
		public static string GetPrefixById(int id)
		{
			string prefix;
			if (Prefixs.TryGetValue(id, out prefix))
				return prefix;

			return null;
		}

		/// <summary>
		/// Get buff name in English
		/// </summary>
		/// <param name="id">Buff Id</param>
		/// <returns>Buff name in English</returns>
		public static string GetBuffNameById(int id)
		{
			string buff;
			if (Buffs.TryGetValue(id, out buff))
				return buff;

			return null;
		}

		/// <summary>
		/// Get vanilla command prefix in English
		/// </summary>
		/// <param name="name">vanilla command name</param>
		/// <returns>vanilla command prefix in English</returns>
		public static string GetCommandPrefixByName(string name)
		{
			string commandText;
			if (VanillaCommandsPrefixs.TryGetValue(name, out commandText))
				return commandText;
			return null;
		}
	}
}
