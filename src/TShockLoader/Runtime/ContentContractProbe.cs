using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TShockAPI;
using tShockLoader.HookAttach;

namespace tShockLoader.Runtime;

static class ContentContractProbe
{
    public const int Marker = 0x50333301;

    public static void Verify()
    {
        var mod = ModLoader.Mods.FirstOrDefault(m => m.Name == "P3Content");
        if (mod is null)
        {
            Console.WriteLine("tShockLoader: P3Content not loaded; skip content contract");
            return;
        }

        if (!mod.IsNetSynced)
            throw new InvalidOperationException("P3Content has no NetID after AssignNetIDs.");

        var item = mod.Find<ModItem>("P3ProbeItem")
            ?? throw new InvalidOperationException("P3Content.P3ProbeItem was not registered.");
        if (item.Type <= 0 || item.Type >= ItemLoader.ItemCount)
            throw new InvalidOperationException($"P3ProbeItem type {item.Type} is outside ItemLoader.ItemCount={ItemLoader.ItemCount}.");

        var byId = TShock.Utils.GetItemById(item.Type);
        if (byId.type != item.Type)
            throw new InvalidOperationException($"GetItemById({item.Type}) returned type {byId.type}.");

        var byName = TShock.Utils.GetItemByIdOrName("P3 Probe Item");
        if (byName.All(i => i.type != item.Type))
            throw new InvalidOperationException("TShock item lookup missed P3 Probe Item.");

        AssertSscItemTag(item.Type);
        AssertSscModPlayer();
        AssertModPacketReachesOrig(mod.NetID);
        DispatchHandlePacket(mod);
        var received = ReadMarker(mod);
        if (received != Marker)
            throw new InvalidOperationException($"P3Content.HandlePacket marker={received}, expected {Marker}.");

        Console.WriteLine($"tShockLoader: content contract ok vanillaItems={ItemID.Count} items={ItemLoader.ItemCount} p3item={item.Type} netId={mod.NetID}");
    }

    static void AssertSscItemTag(int type)
    {
        var src = new Item();
        src.SetDefaults(type);
        src.stack = 7;
        var loadedField = src.ModItem?.GetType().GetField("LoadedTag", BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingFieldException(src.ModItem?.GetType().FullName, "LoadedTag");
        loadedField.SetValue(null, 0);

        var player = new Player();
        player.inventory[0] = src;
        var stored = new PlayerData(false);
        stored.Capture(player);
        if (stored.PlrData.Length == 0 || stored.TplrData.Length == 0)
            throw new InvalidOperationException("SSC player snapshot was empty for a mod item.");

        var dest = stored.Materialize().inventory[0];
        if (dest.type != type || dest.stack != 7)
            throw new InvalidOperationException($"SSC roundtrip type={dest.type} stack={dest.stack}, expected {type}/7.");
        if ((int)loadedField.GetValue(null)! != 0x50333301)
            throw new InvalidOperationException($"SSC dropped item tag, LoadedTag={loadedField.GetValue(null)}");
    }

    static void AssertSscModPlayer()
    {
        if (!ModContent.TryFind<ModPlayer>("P3Content", "P3ProbePlayer", out var baseline))
            throw new InvalidOperationException("P3ProbePlayer was not registered.");

        var src = new Player();
        var mp = src.GetModPlayer(baseline);
        var marker = mp.GetType().GetField("Marker") ?? throw new MissingFieldException(mp.GetType().FullName, "Marker");
        var loaded = mp.GetType().GetField("LoadedTag", BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingFieldException(mp.GetType().FullName, "LoadedTag");
        marker.SetValue(mp, 0x50333302);
        loaded.SetValue(null, 0);

        var stored = new PlayerData(false);
        stored.Capture(src);
        if (stored.TplrData.Length == 0)
            throw new InvalidOperationException("SSC player snapshot was empty for ModPlayer.");

        stored.Materialize();
        if ((int)loaded.GetValue(null)! != 0x50333302)
            throw new InvalidOperationException($"SSC dropped ModPlayer tag, LoadedTag={loaded.GetValue(null)}");
    }

    static void AssertModPacketReachesOrig(short netId)
    {
        var buffer = new MessageBuffer
        {
            whoAmI = 255,
            readBuffer = new byte[64],
        };
        buffer.readBuffer[0] = MessageID.ModPacket;
        buffer.readBuffer[1] = (byte)netId;
        BitConverter.TryWriteBytes(buffer.readBuffer.AsSpan(2, 4), Marker);
        MessageBufferHooks.CaptureOnly = true;
        try
        {
            buffer.GetData(0, 6, out _);
            if (!MessageBufferHooks.LastAllowedOrig)
                throw new InvalidOperationException("P3 ModPacket was cancelled before TML GetData.");
        }
        finally
        {
            MessageBufferHooks.CaptureOnly = false;
        }
    }

    static void DispatchHandlePacket(Mod mod)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            writer.Write(Marker);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        mod.HandlePacket(reader, 255);
    }

    static int ReadMarker(Mod mod)
    {
        var field = mod.GetType().GetField("LastMarker", BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingFieldException(mod.GetType().FullName, "LastMarker");
        return (int)field.GetValue(null)!;
    }
}
