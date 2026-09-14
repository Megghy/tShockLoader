// OTAPI, Version=1.4.4.9, Culture=neutral, PublicKeyToken=null
// OTAPI.Hooks
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.Net.Sockets;
using TerrariaApi.Server;

namespace OTAPI
{
    public static class Hooks
    {
        public static class Main
        {
            public class StatusTextChangeArgs : EventArgs
            {
                public string Value { get; set; }

                public StatusTextChangeArgs(string value)
                {
                    Value = value;
                }
            }

            public class CommandProcessEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public string Command { get; set; }

                public string Lowered { get; set; }
            }

            public static event EventHandler<StatusTextChangeArgs>? StatusTextChange;

            public static event EventHandler<CommandProcessEventArgs> CommandProcess;

            public static string InvokeStatusTextChange(string value)
            {
                StatusTextChangeArgs statusTextChangeArgs = new StatusTextChangeArgs(value);
                Main.StatusTextChange?.Invoke(null, statusTextChangeArgs);
                return statusTextChangeArgs.Value;
            }

            public static bool InvokeCommandProcess(string lowered, string raw)
            {
                CommandProcessEventArgs e = new CommandProcessEventArgs
                {
                    Lowered = lowered,
                    Command = raw
                };
                Main.CommandProcess?.Invoke(null, e);
                return e.Result != HookResult.Cancel;
            }
        }

        public static class NPC
        {
            public class TransformingEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public Terraria.NPC Npc { get; set; }

                public int NewType { get; set; }
            }

            public class KilledEventArgs : EventArgs
            {
                public Terraria.NPC Npc { get; set; }
            }

            public class CreateEventArgs : EventArgs
            {
                public Terraria.NPC Npc { get; set; }

                public IEntitySource Source { get; set; }

                public int X { get; set; }

                public int Y { get; set; }

                public int Type { get; set; }

                public int Start { get; set; }

                public float Ai0 { get; set; }

                public float Ai1 { get; set; }

                public float Ai2 { get; set; }

                public float Ai3 { get; set; }

                public int Target { get; set; }
            }

            public class MechSpawnEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public float X { get; set; }

                public float Y { get; set; }

                public int Type { get; set; }

                public int Num { get; set; }

                public int Num2 { get; set; }

                public int Num3 { get; set; }
            }

            public class DropLootEventArgs : EventArgs
            {
                public HookEvent Event { get; set; }

                public HookResult? Result { get; set; }

                public IEntitySource Source { get; set; }

                public Terraria.NPC Npc { get; set; }

                public int ItemIndex { get; set; }

                public int X { get; set; }

                public int Y { get; set; }

                public int Width { get; set; }

                public int Height { get; set; }

                public int Type { get; set; }

                public int Stack { get; set; }

                public bool NoBroadcast { get; set; }

                public int Pfix { get; set; }

                public bool NoGrabDelay { get; set; }

                public bool ReverseLookup { get; set; }
            }

            public class SpawnEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public int Index { get; set; }
            }

            public class BossBagEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public IEntitySource Source { get; set; }

                public Terraria.NPC Npc { get; set; }

                public int X { get; set; }

                public int Y { get; set; }

                public int Width { get; set; }

                public int Height { get; set; }

                public int Type { get; set; }

                public int Stack { get; set; }

                public bool NoBroadcast { get; set; }

                public int Pfix { get; set; }

                public bool NoGrabDelay { get; set; }

                public bool ReverseLookup { get; set; }
            }

            public static event EventHandler<TransformingEventArgs> Transforming;

            public static event EventHandler<KilledEventArgs> Killed;

            public static event EventHandler<CreateEventArgs> Create;

            public static event EventHandler<MechSpawnEventArgs> MechSpawn;

            public static event EventHandler<DropLootEventArgs>? DropLoot;

            public static event EventHandler<SpawnEventArgs> Spawn;

            public static event EventHandler<BossBagEventArgs> BossBag;

            public static bool InvokeTransforming(Terraria.NPC instance, ref int newType)
            {
                TransformingEventArgs e = new TransformingEventArgs
                {
                    Npc = instance,
                    NewType = newType
                };
                NPC.Transforming?.Invoke(null, e);
                newType = e.NewType;
                return e.Result != HookResult.Cancel;
            }

            public static void InvokeKilled(Terraria.NPC instance)
            {
                KilledEventArgs e = new KilledEventArgs
                {
                    Npc = instance
                };
                NPC.Killed?.Invoke(null, e);
            }

            public static Terraria.NPC InvokeCreate(IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
            {
                CreateEventArgs e = new CreateEventArgs
                {
                    Source = source,
                    X = X,
                    Y = Y,
                    Type = Type,
                    Start = Start,
                    Ai0 = ai0,
                    Ai1 = ai1,
                    Ai2 = ai2,
                    Ai3 = ai3,
                    Target = Target
                };
                NPC.Create?.Invoke(null, e);
                return e.Npc ?? new Terraria.NPC();
            }

            public static bool InvokeMechSpawn(bool result, float x, float y, int type, int num, int num2, int num3)
            {
                if (result)
                {
                    MechSpawnEventArgs e = new MechSpawnEventArgs
                    {
                        X = x,
                        Y = y,
                        Type = type,
                        Num = num,
                        Num2 = num2,
                        Num3 = num3
                    };
                    NPC.MechSpawn?.Invoke(null, e);
                    return e.Result != HookResult.Cancel;
                }
                return result;
            }

            public static int InvokeDropLoot(IEntitySource source, int X, int Y, int Width, int Height, int Type, int Stack, bool noBroadcast, int pfix, bool noGrabDelay, bool reverseLookup, Terraria.NPC instance)
            {
                DropLootEventArgs e = new DropLootEventArgs
                {
                    Event = HookEvent.Before,
                    Source = source,
                    X = X,
                    Y = Y,
                    Width = Width,
                    Height = Height,
                    Type = Type,
                    Stack = Stack,
                    NoBroadcast = noBroadcast,
                    Pfix = pfix,
                    NoGrabDelay = noGrabDelay,
                    ReverseLookup = reverseLookup,
                    Npc = instance,
                    ItemIndex = 0
                };
                NPC.DropLoot?.Invoke(null, e);
                if (e.Result != HookResult.Cancel)
                {
                    e.ItemIndex = Terraria.Item.NewItem(e.Source, X, Y, Width, Height, Type, Stack, noBroadcast, pfix, noGrabDelay, reverseLookup);
                    e.Event = HookEvent.After;
                    NPC.DropLoot?.Invoke(null, e);
                }
                return e.ItemIndex;
            }

            public static bool InvokeSpawn(ref int index)
            {
                SpawnEventArgs e = new SpawnEventArgs
                {
                    Index = index
                };
                NPC.Spawn?.Invoke(null, e);
                index = e.Index;
                return e.Result != HookResult.Cancel;
            }

            public static int InvokeBossBag(IEntitySource Source, int X, int Y, int Width, int Height, int Type, int Stack, bool noBroadcast, int pfix, bool noGrabDelay, bool reverseLookup, Terraria.NPC npc)
            {
                BossBagEventArgs e = new BossBagEventArgs
                {
                    Source = Source,
                    X = X,
                    Y = Y,
                    Width = Width,
                    Height = Height,
                    Type = Type,
                    Stack = Stack,
                    NoBroadcast = noBroadcast,
                    Pfix = pfix,
                    NoGrabDelay = noGrabDelay,
                    ReverseLookup = reverseLookup,
                    Npc = npc
                };
                NPC.BossBag?.Invoke(null, e);
                if (e.Result == HookResult.Cancel)
                {
                    return -1;
                }
                return Terraria.Item.NewItem(Source, e.X, e.Y, e.Width, e.Height, e.Type, e.Stack, e.NoBroadcast, e.Pfix, e.NoGrabDelay, e.ReverseLookup);
            }
        }

        public static class WorldGen
        {
            public class MeteorEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public int X { get; set; }

                public int Y { get; set; }
            }

            public class HardmodeTilePlaceEventArgs : EventArgs
            {
                public HardmodeTileUpdateResult? Result { get; set; }

                public int X { get; set; }

                public int Y { get; set; }

                public int Type { get; set; }

                public bool Mute { get; set; }

                public bool Forced { get; set; }

                public int Plr { get; set; }

                public int Style { get; set; }
            }

            public class HardmodeTileUpdateEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public int X { get; set; }

                public int Y { get; set; }

                public int Type { get; set; }
            }

            public static event EventHandler<MeteorEventArgs> Meteor;

            public static event EventHandler<HardmodeTilePlaceEventArgs> HardmodeTilePlace;

            public static event EventHandler<HardmodeTileUpdateEventArgs> HardmodeTileUpdate;

            public static bool InvokeMeteor(ref int x, ref int y)
            {
                MeteorEventArgs e = new MeteorEventArgs
                {
                    X = x,
                    Y = y
                };
                WorldGen.Meteor?.Invoke(null, e);
                x = e.X;
                y = e.Y;
                return e.Result != HookResult.Cancel;
            }

            public static bool InvokeHardmodeTilePlace(int x, int y, int type, bool mute, bool forced, int plr, int style)
            {
                HardmodeTilePlaceEventArgs e = new HardmodeTilePlaceEventArgs
                {
                    X = x,
                    Y = y,
                    Type = type,
                    Mute = mute,
                    Forced = forced,
                    Plr = plr,
                    Style = style,
                    Result = HardmodeTileUpdateResult.Continue
                };
                WorldGen.HardmodeTilePlace?.Invoke(null, e);
                if (e.Result == HardmodeTileUpdateResult.Cancel)
                {
                    return false;
                }
                if (e.Result == HardmodeTileUpdateResult.Continue)
                {
                    Terraria.WorldGen.PlaceTile(e.X, e.Y, e.Type, e.Mute, e.Forced, e.Plr, e.Style);
                }
                return true;
            }

            public static bool InvokeHardmodeTileUpdate(int x, int y, ushort type)
            {
                HardmodeTileUpdateEventArgs e = new HardmodeTileUpdateEventArgs
                {
                    X = x,
                    Y = y,
                    Type = type
                };
                WorldGen.HardmodeTileUpdate?.Invoke(null, e);
                if (e.Result == HookResult.Cancel)
                {
                    return false;
                }
                return true;
            }
        }

        public static class Tile
        {
            public delegate Terraria.Tile CreateHandler(Terraria.Tile? existing = null);

            public static CreateHandler? Create;

            public static Terraria.Tile InvokeCreate(On_Tile.orig_ctor orig, ref Terraria.Tile self, uint tileId)
            {
                var result = Create?.Invoke();
                if (!result.HasValue)
                {
                    orig(ref self, tileId);
                    result = self;
                }
                return result.Value;
            }
            // not exist in tml
            /*public static Terraria.Tile InvokeCreate(On_Tile.orig_ctor orig, ref Terraria.Tile self, Terraria.Tile existing)
            {
                var result = Create?.Invoke(existing);
                if (result == null)
                {
                    orig(ref self, existing);
                    result = self;
                }
                return result.Value;
            }*/
        }

        public static class NetMessage
        {
            public class SendBytesEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public ISocket Socket { get; set; }

                public int RemoteClient { get; set; }

                public byte[] Data { get; set; }

                public int Offset { get; set; }

                public int Size { get; set; }

                public SocketSendCallback Callback { get; set; }

                public object State { get; set; }
            }

            [Flags]
            public enum PlayerAnnounceResult : byte
            {
                None = 0,
                SendToPlayer = 1,
                WriteToConsole = 2,
                Default = 3
            }

            public class PlayerAnnounceEventArgs : EventArgs
            {
                public PlayerAnnounceResult Result { get; set; } = PlayerAnnounceResult.Default;

                public int Index { get; set; }

                public NetworkText Text { get; set; }

                public Color Color { get; set; }

                public int ExcludedPlayer { get; set; }

                public int Plr { get; set; }

                public int ToWho { get; set; }

                public int FromWh { get; set; }

                public PlayerAnnounceEventArgs(NetworkText text)
                {
                    Text = text;
                }
            }

            public class CreatePacketWriterEventArgs : EventArgs
            {
                public PacketWriter PacketWriter { get; set; }
            }

            public class CheckBytesExceptionEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public Exception Exception { get; set; }
            }

            public class SendDataEventArgs : EventArgs
            {
                public HookEvent Event { get; set; }

                public HookResult? Result { get; set; }

                public int BufferId { get; set; }

                public int MsgType { get; set; }

                public int RemoteClient { get; set; }

                public int IgnoreClient { get; set; }

                public NetworkText Text { get; set; }

                public int Number { get; set; }

                public float Number2 { get; set; }

                public float Number3 { get; set; }

                public float Number4 { get; set; }

                public int Number5 { get; set; }

                public int Number6 { get; set; }

                public int Number7 { get; set; }
            }

            public static event EventHandler<SendBytesEventArgs> SendBytes;

            public static event EventHandler<PlayerAnnounceEventArgs>? PlayerAnnounce;

            public static event EventHandler<CreatePacketWriterEventArgs> CreatePacketWriter;

            public static event EventHandler<CheckBytesExceptionEventArgs> CheckBytesException;

            public static event EventHandler<SendDataEventArgs> SendData;

            public static void InvokeSendBytes(ISocket socket, byte[] data, int offset, int size, SocketSendCallback callback, object state, int remoteClient)
            {
                SendBytesEventArgs e = new SendBytesEventArgs
                {
                    Socket = socket,
                    Data = data,
                    Offset = offset,
                    Size = size,
                    Callback = callback,
                    State = state,
                    RemoteClient = remoteClient
                };
                NetMessage.SendBytes?.Invoke(null, e);
                if (e.Result != HookResult.Cancel)
                {
                    socket.AsyncSend(data, offset, size, callback, state);
                }
            }

            public static bool InvokePlayerAnnounce(NetworkText text, Color color, int excludedPlayer, int plr, int toWho, int fromWho)
            {
                PlayerAnnounceEventArgs e = new PlayerAnnounceEventArgs(text)
                {
                    Color = color,
                    ExcludedPlayer = excludedPlayer,
                    Plr = plr,
                    ToWho = toWho,
                    FromWh = fromWho
                };
                NetMessage.PlayerAnnounce?.Invoke(null, e);
                if ((e.Result & PlayerAnnounceResult.SendToPlayer) != PlayerAnnounceResult.None)
                {
                    ChatHelper.BroadcastChatMessage(e.Text, e.Color, e.ExcludedPlayer);
                }
                return (e.Result & PlayerAnnounceResult.WriteToConsole) != 0;
            }

            public static PacketWriter InvokeCreatePacketWriter(MemoryStream output)
            {
                CreatePacketWriterEventArgs e = new CreatePacketWriterEventArgs();
                NetMessage.CreatePacketWriter?.Invoke(null, e);
                return e.PacketWriter ?? new PacketWriter(output);
            }

            public static void InvokeCheckBytesException(Exception exception)
            {
                CheckBytesExceptionEventArgs e = new CheckBytesExceptionEventArgs
                {
                    Exception = exception
                };
                NetMessage.CheckBytesException?.Invoke(null, e);
                if (e.Result != HookResult.Cancel)
                {
                    Console.WriteLine(exception);
                }
            }

            public static HookResult? InvokeSendData(SendDataEventArgs args)
            {
                NetMessage.SendData?.Invoke(null, args);
                return args.Result;
            }
        }

        public static class Wiring
        {
            public class AnnouncementBoxEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public int X { get; set; }

                public int Y { get; set; }

                public int SignId { get; set; }
            }

            public static event EventHandler<AnnouncementBoxEventArgs> AnnouncementBox;

            public static bool InvokeAnnouncementBox(int x, int y, int signId)
            {
                AnnouncementBoxEventArgs e = new AnnouncementBoxEventArgs
                {
                    X = x,
                    Y = y,
                    SignId = signId
                };
                Wiring.AnnouncementBox?.Invoke(null, e);
                return e.Result != HookResult.Cancel;
            }
        }

        public static class MessageBuffer
        {
            public class GetDataEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public Terraria.MessageBuffer Instance { get; set; }

                public byte PacketId { get; set; }

                public int ReadOffset { get; set; }

                public int Start { get; set; }

                public int Length { get; set; }

                public int MessageType { get; set; }

                public int MaxPackets { get; set; }
            }

            public class NameCollisionEventArgs : EventArgs
            {
                public HookEvent Event { get; set; }

                public HookResult? Result { get; set; }

                public Player Player { get; set; }
            }

            public class ClientUUIDReceivedEventArgs : EventArgs
            {
                public HookEvent Event { get; set; }

                public HookResult? Result { get; set; }

                public Terraria.MessageBuffer Instance { get; set; }

                public BinaryReader Reader { get; set; }

                public int Start { get; set; }

                public int Length { get; set; }

                public int MessageType { get; set; }

                public string clientUUID { get; set; }
            }

            public static event EventHandler<GetDataEventArgs> GetData;

            public static event EventHandler<NameCollisionEventArgs>? NameCollision;

            public static event EventHandler<ClientUUIDReceivedEventArgs> ClientUUIDReceived;

            public static bool InvokeGetData(Terraria.MessageBuffer instance, ref byte packetId, ref int readOffset, ref int start, ref int length, ref int messageType, int maxPackets)
            {
                GetDataEventArgs e = new GetDataEventArgs
                {
                    Instance = instance,
                    PacketId = packetId,
                    ReadOffset = readOffset,
                    Start = start,
                    Length = length,
                    MessageType = messageType,
                    MaxPackets = maxPackets
                };
                MessageBuffer.GetData?.Invoke(null, e);
                packetId = e.PacketId;
                readOffset = e.ReadOffset;
                start = e.Start;
                length = e.Length;
                messageType = e.MessageType;
                maxPackets = e.MaxPackets;
                return e.Result != HookResult.Cancel && packetId < maxPackets;
            }

            public static bool InvokeNameCollision(Player player)
            {
                NameCollisionEventArgs e = new NameCollisionEventArgs
                {
                    Player = player
                };
                MessageBuffer.NameCollision?.Invoke(null, e);
                return e.Result != HookResult.Cancel;
            }

            public static void InvokeClientUUIDReceived(Terraria.MessageBuffer instance, BinaryReader reader, int start, int length, ref int messageType)
            {
                ClientUUIDReceivedEventArgs e = new ClientUUIDReceivedEventArgs
                {
                    Event = HookEvent.Before,
                    Instance = instance,
                    Reader = reader,
                    Start = start,
                    Length = length,
                    MessageType = messageType
                };
                MessageBuffer.ClientUUIDReceived?.Invoke(null, e);
                if (e.Result != HookResult.Cancel)
                {
                    e.clientUUID = reader.ReadString();
                    ServerApi.ClientUUID[e.Instance.whoAmI] = e.clientUUID;
                    e.Event = HookEvent.After;
                    MessageBuffer.ClientUUIDReceived?.Invoke(null, e);
                }
            }
        }

        public static class Item
        {
            public class MechSpawnEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public float X { get; set; }

                public float Y { get; set; }

                public int Type { get; set; }

                public int Num { get; set; }

                public int Num2 { get; set; }

                public int Num3 { get; set; }
            }

            public static event EventHandler<MechSpawnEventArgs> MechSpawn;

            public static bool InvokeMechSpawn(bool result, float x, float y, int type, int num, int num2, int num3)
            {
                if (result)
                {
                    MechSpawnEventArgs e = new MechSpawnEventArgs
                    {
                        X = x,
                        Y = y,
                        Type = type,
                        Num = num,
                        Num2 = num2,
                        Num3 = num3
                    };
                    Item.MechSpawn?.Invoke(null, e);
                    return e.Result != HookResult.Cancel;
                }
                return result;
            }
        }

        public static class Netplay
        {
            public class CreateTcpListenerEventArgs : EventArgs
            {
                public ISocket Result { get; set; }
            }

            public static event EventHandler<CreateTcpListenerEventArgs> CreateTcpListener;

            public static ISocket InvokeCreateTcpListener()
            {
                CreateTcpListenerEventArgs e = new CreateTcpListenerEventArgs();
                Netplay.CreateTcpListener?.Invoke(null, e);
                return e.Result ?? new TcpSocket();
            }
        }

        public static class Collision
        {
            public class PressurePlateEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public int X { get; set; }

                public int Y { get; set; }

                public Entity Entity { get; set; }
            }

            public static event EventHandler<PressurePlateEventArgs> PressurePlate;

            public static bool InvokePressurePlate(int x, int y, Entity entity)
            {
                PressurePlateEventArgs e = new PressurePlateEventArgs
                {
                    X = x,
                    Y = y,
                    Entity = entity
                };
                Collision.PressurePlate?.Invoke(null, e);
                return e.Result != HookResult.Cancel;
            }
        }

        public static class Chest
        {
            public class QuickStackEventArgs : EventArgs
            {
                public HookResult? Result { get; set; }

                public int PlayerId { get; set; }

                public Terraria.Item Item { get; set; }

                public int ChestIndex { get; set; }
            }

            public static event EventHandler<QuickStackEventArgs> QuickStack;

            public static bool InvokeQuickStack(int playerId, Terraria.Item item, int chestIndex)
            {
                QuickStackEventArgs e = new QuickStackEventArgs
                {
                    PlayerId = playerId,
                    Item = item,
                    ChestIndex = chestIndex
                };
                Chest.QuickStack?.Invoke(null, e);
                return e.Result != HookResult.Cancel;
            }
        }
    }
}
