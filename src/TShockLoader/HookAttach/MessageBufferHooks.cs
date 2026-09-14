using Terraria;
using Terraria.ID;

namespace tShockLoader.HookAttach;

public static class MessageBufferHooks
{
    public static void Attach()
    {
        On_MessageBuffer.GetData += OnGetData;
    }

    public static void Detach()
    {
        On_MessageBuffer.GetData -= OnGetData;
    }

    static void OnGetData(On_MessageBuffer.orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
    {
        messageType = 0;
        byte packetId = self.readBuffer[start];
        int readOffset = start + 1;
        int maxPackets = MessageID.Count;
        if (OTAPI.Hooks.MessageBuffer.InvokeGetData(self, ref packetId, ref readOffset, ref start, ref length, ref messageType, maxPackets))
            orig(self, start, length, out messageType);
        else
            messageType = 0;
    }
}
