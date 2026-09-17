using Terraria;
using Terraria.Localization;

namespace TShockAPI.Localization;

static class ContentNames
{
	static Dictionary<int, string[]> items = new();
	static Dictionary<int, string[]> npcs = new();
	static Dictionary<int, string[]> buffs = new();
	static Dictionary<int, string[]> prefixes = new();

	public static void Rebuild()
	{
		var itemSets = new Dictionary<int, HashSet<string>>();
		var npcSets = new Dictionary<int, HashSet<string>>();
		var buffSets = new Dictionary<int, HashSet<string>>();
		var prefixSets = new Dictionary<int, HashSet<string>>();
		var origin = Language.ActiveCulture;
		try
		{
			foreach (var culture in GameCulture.KnownCultures)
			{
				LanguageManager.Instance.SetLanguage(culture);
				CaptureItems(itemSets);
				CaptureNpcs(npcSets);
				CaptureBuffs(buffSets);
				CapturePrefixes(prefixSets);
				if (culture == GameCulture.FromCultureName(GameCulture.CultureName.English))
					EnglishLanguage.CaptureFromLang();
			}
		}
		finally
		{
			LanguageManager.Instance.SetLanguage(origin);
		}

		items = Freeze(itemSets);
		npcs = Freeze(npcSets);
		buffs = Freeze(buffSets);
		prefixes = Freeze(prefixSets);
	}

	public static List<int> MatchItems(string name) => Match(items, name, 1, ContentIds.Items);
	public static List<int> MatchNpcs(string name) => Match(npcs, name, -17, ContentIds.Npcs);
	public static List<int> MatchBuffs(string name) => Match(buffs, name, 1, ContentIds.Buffs);
	public static List<int> MatchPrefixes(string name) => Match(prefixes, name, 1, ContentIds.Prefixes);

	static void CaptureItems(Dictionary<int, HashSet<string>> sets)
	{
		for (var i = 1; i < ContentIds.Items; i++)
			Add(sets, i, Lang.GetItemNameValue(i));
	}

	static void CaptureNpcs(Dictionary<int, HashSet<string>> sets)
	{
		for (var i = -17; i < ContentIds.Npcs; i++)
			Add(sets, i, Lang.GetNPCNameValue(i));
	}

	static void CaptureBuffs(Dictionary<int, HashSet<string>> sets)
	{
		for (var i = 1; i < ContentIds.Buffs; i++)
			Add(sets, i, Lang.GetBuffName(i));
	}

	static void CapturePrefixes(Dictionary<int, HashSet<string>> sets)
	{
		var count = Math.Min(ContentIds.Prefixes, Lang.prefix.Length);
		for (var i = 1; i < count; i++)
		{
			var text = Lang.prefix[i];
			if (text is not null)
				Add(sets, i, text.Value);
		}
	}

	static void Add(Dictionary<int, HashSet<string>> sets, int id, string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return;
		if (!sets.TryGetValue(id, out var names))
			sets[id] = names = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
		names.Add(name);
	}

	static Dictionary<int, string[]> Freeze(Dictionary<int, HashSet<string>> sets)
	{
		var result = new Dictionary<int, string[]>(sets.Count);
		foreach (var (id, names) in sets)
		{
			var copy = new string[names.Count];
			names.CopyTo(copy);
			result[id] = copy;
		}
		return result;
	}

	static List<int> Match(Dictionary<int, string[]> aliases, string query, int minId, int maxExclusive)
	{
		var exact = new List<int>();
		var prefix = new List<int>();
		var contains = new List<int>();
		for (var id = minId; id < maxExclusive; id++)
		{
			if (!aliases.TryGetValue(id, out var names))
				continue;
			var kind = 0;
			foreach (var name in names)
			{
				if (name.Equals(query, StringComparison.InvariantCultureIgnoreCase))
				{
					kind = 3;
					break;
				}
				if (name.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
					kind = Math.Max(kind, 2);
				else if (name.Contains(query, StringComparison.InvariantCultureIgnoreCase))
					kind = Math.Max(kind, 1);
			}
			if (kind == 3)
				exact.Add(id);
			else if (kind == 2)
				prefix.Add(id);
			else if (kind == 1)
				contains.Add(id);
		}

		if (exact.Count > 0)
			return exact;
		if (prefix.Count != 1)
			prefix.AddRange(contains);
		return prefix;
	}
}
