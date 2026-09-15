using System.IO;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace P3Content;

public sealed class P3Content : Mod
{
    public static int LastMarker;

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        LastMarker = reader.ReadInt32();
        Logger.Info($"HandlePacket marker={LastMarker} from={whoAmI}");
    }
}

public sealed class P3ProbeItem : ModItem
{
    public const int TagMarker = 0x50333301;
    public static int LoadedTag;

    public override string Texture => $"Terraria/Images/Item_{ItemID.DirtBlock}";

    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.DirtBlock);
        Item.maxStack = 9999;
    }

    public override void SaveData(TagCompound tag) => tag["probe"] = TagMarker;

    public override void LoadData(TagCompound tag) => LoadedTag = tag.GetInt("probe");
}

public sealed class P3ProbePlayer : ModPlayer
{
    public const int TagMarker = 0x50333302;
    public static int LoadedTag;
    public int Marker = TagMarker;

    public override void SaveData(TagCompound tag) => tag["probe"] = Marker;

    public override void LoadData(TagCompound tag)
    {
        Marker = tag.GetInt("probe");
        LoadedTag = Marker;
    }
}
