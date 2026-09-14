using Terraria;
using Terraria.Localization;

namespace tShockLoader.HookAttach;

public static class NetMessageHooks
{
    public static void Attach()
    {
        On_NetMessage.CheckBytes += OnCheckBytes;
        On_NetMessage.SendData += OnSendData;
    }

    public static void Detach()
    {
        On_NetMessage.CheckBytes -= OnCheckBytes;
        On_NetMessage.SendData -= OnSendData;
    }

    static void OnCheckBytes(On_NetMessage.orig_CheckBytes orig, int bufferIndex)
    {
        try
        {
            orig(bufferIndex);
        }
        catch (Exception ex)
        {
            OTAPI.Hooks.NetMessage.InvokeCheckBytesException(ex);
        }
    }

    static void OnSendData(On_NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
    {
        var args = new OTAPI.Hooks.NetMessage.SendDataEventArgs
        {
            Event = OTAPI.HookEvent.Before,
            MsgType = msgType,
            RemoteClient = remoteClient,
            IgnoreClient = ignoreClient,
            Text = text,
            Number = number,
            Number2 = number2,
            Number3 = number3,
            Number4 = number4,
            Number5 = number5,
            Number6 = number6,
            Number7 = number7,
        };

        if (OTAPI.Hooks.NetMessage.InvokeSendData(args) == OTAPI.HookResult.Cancel)
            return;

        orig(args.MsgType, args.RemoteClient, args.IgnoreClient, args.Text, args.Number, args.Number2, args.Number3,
            args.Number4, args.Number5, args.Number6, args.Number7);

        args.Event = OTAPI.HookEvent.After;
        OTAPI.Hooks.NetMessage.InvokeSendData(args);
    }
}
