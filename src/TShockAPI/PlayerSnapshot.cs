using System.Reflection;
using Terraria;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TShockAPI;

static class PlayerSnapshot
{
	static readonly MethodInfo SaveData = typeof(Player).Assembly
		.GetType("Terraria.ModLoader.IO.PlayerIO")!
		.GetMethod("SaveData", BindingFlags.Static | BindingFlags.NonPublic)
		?? throw new MissingMethodException("Terraria.ModLoader.IO.PlayerIO", "SaveData");

	public static void Save(Player player, out byte[] plr, out byte[] tplr)
	{
		PlayerLoader.PreSavePlayer(player);
		var file = new PlayerFileData("ssc", false)
		{
			Player = player,
			Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
		};
		plr = Player.SavePlayerFile_Vanilla(file);
		PlayerLoader.PostSavePlayer(player);

		var tag = (TagCompound)SaveData.Invoke(null, [player])!;
		using var ms = new MemoryStream();
		TagIO.ToStream(tag, ms);
		tplr = ms.ToArray();
	}

	public static Player Load(byte[] plr, byte[] tplr)
	{
		var file = new PlayerFileData("ssc", false);
		Player.LoadPlayerFromStream(file, plr, tplr);
		if (file.customDataFail != null)
			throw file.customDataFail;
		if (file.Player is null)
			throw new InvalidOperationException("LoadPlayerFromStream returned no player.");
		return file.Player;
	}
}
