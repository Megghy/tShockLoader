using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using tShockLoader.HookAttach;

namespace tShockLoader.Runtime;

static class NetContractProbe
{
    public static void Verify()
    {
        var buffer = new MessageBuffer
        {
            whoAmI = 0,
            readBuffer = new byte[65535],
        };

        AssertTmlPacketsAreNotCancelled(buffer);
        AssertLargeModFileIsNotCancelled(buffer);
        AssertGetDataGate(buffer);
    }

    static void AssertTmlPacketsAreNotCancelled(MessageBuffer buffer)
    {
        foreach (var id in new byte[]
        {
            (byte)PacketTypes.InGameChangeConfig,
            (byte)PacketTypes.ModPacket,
            (byte)PacketTypes.SyncMods,
            (byte)PacketTypes.ModFile,
            (byte)PacketTypes.KeepAliveDuringModReload,
        })
        {
            if (InvokeGetData(buffer, id, 32))
                throw new InvalidOperationException($"TML packet {id} was cancelled by TSAPI.");
        }
    }

    static void AssertLargeModFileIsNotCancelled(MessageBuffer buffer)
    {
        if (InvokeGetData(buffer, (byte)PacketTypes.ModFile, 50000))
            throw new InvalidOperationException("TML ModFile payload was truncated by the vanilla 1000-byte cap.");
    }

    static void AssertGetDataGate(MessageBuffer buffer)
    {
        var core = ServerApi.Plugins.First(p => p.Source == "Core").Plugin;
        void cancelTile(GetDataEventArgs e)
        {
            if (e.MsgID == PacketTypes.Tile)
                e.Handled = true;
        }

        ServerApi.Hooks.NetGetData.Register(core, cancelTile, int.MaxValue);
        MessageBufferHooks.CaptureOnly = true;
        try
        {
            Inject(buffer, (byte)PacketTypes.Tile, 16);
            if (MessageBufferHooks.LastAllowedOrig)
                throw new InvalidOperationException("Cancelled vanilla Tile still reached TML GetData.");

            Inject(buffer, MessageID.ModPacket, 16);
            if (!MessageBufferHooks.LastAllowedOrig)
                throw new InvalidOperationException("ModPacket did not reach TML GetData.");
        }
        finally
        {
            MessageBufferHooks.CaptureOnly = false;
            ServerApi.Hooks.NetGetData.Deregister(core, cancelTile);
        }
    }

    static bool InvokeGetData(MessageBuffer buffer, byte packetId, int length)
    {
        Fill(buffer, packetId);
        var msgId = packetId;
        var index = 1;
        var size = length;
        return ServerApi.Hooks.InvokeNetGetData(ref msgId, buffer, ref index, ref size);
    }

    static void Inject(MessageBuffer buffer, byte packetId, int length)
    {
        Fill(buffer, packetId);
        buffer.GetData(0, length, out _);
    }

    static void Fill(MessageBuffer buffer, byte packetId)
    {
        Array.Clear(buffer.readBuffer);
        buffer.readBuffer[0] = packetId;
    }
}
