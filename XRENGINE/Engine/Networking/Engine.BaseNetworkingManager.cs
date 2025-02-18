using RestSharp;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using XREngine.Data;
using XREngine.Data.Core;
using XREngine.Data.Transforms.Rotations;
using XREngine.Scene.Transforms;

namespace XREngine
{
    public static partial class Engine
    {
        public abstract class BaseNetworkingManager : XRBase
        {
            public bool UDPServerConnectionEstablished
                => UdpReceiver?.Client.Connected ?? false;

            public BaseNetworkingManager()
            {
                Time.Timer.UpdateFrame += ConsumeQueues;
            }
            ~BaseNetworkingManager()
            {
                Time.Timer.UpdateFrame -= ConsumeQueues;
            }

            /// <summary>
            /// Sends from server to all connected clients, or from client to all other p2p clients.
            /// </summary>
            public UdpClient? UdpMulticastSender { get; set; }
            /// <summary>
            /// Receives from server or from other p2p clients.
            /// </summary>
            public UdpClient? UdpReceiver { get; set; }
            public IPEndPoint? MulticastEndPoint { get; set; }

            protected abstract void SendUDP();
            protected virtual async Task ReadUDP()
            {
                if (!(UdpReceiver?.Client.Connected ?? false))
                    return;

                while (UdpReceiver.Available > 0)
                {
                    UdpReceiveResult result = await UdpReceiver.ReceiveAsync();
                    byte[] allData = result.Buffer;
                    Buffer.BlockCopy(allData, 0, _udpInBuffer, _udpBufferOffset, allData.Length);
                    _udpBufferOffset += allData.Length;
                }

                ReadReceivedData(_udpInBuffer, ref _udpBufferOffset, _decompBuffer);
            }
            public virtual void ConsumeQueues()
            {
                var read = Task.Run(ReadUDP);
                var send = Task.Run(SendUDP);
                Task.WaitAll(read, send);
            }

            protected void StartUdpMulticastSender(IPAddress udpMulticastIP, int udpMulticastPort)
            {
                UdpMulticastSender = new UdpClient { /*MulticastLoopback = false*/ };
                MulticastEndPoint = new IPEndPoint(udpMulticastIP, udpMulticastPort);
                UdpMulticastSender.Connect(MulticastEndPoint);
            }

            /// <summary>
            /// Run on the server - receives from clients
            /// </summary>
            /// <param name="udpPort"></param>
            protected void StartUdpReceiver(int udpPort)
            {
                UdpClient listener = new();
                // Allow multiple clients to use the same port
                listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));
                UdpReceiver = listener;
            }

            /// <summary>
            /// Run on the client - receives from server
            /// </summary>
            /// <param name="udpMulticastServerIP"></param>
            /// <param name="upMulticastServerPort"></param>
            protected void StartUdpMulticastReceiver(IPAddress serverIP, IPAddress udpMulticastServerIP, int upMulticastServerPort)
            {
                UdpClient udpClient = new() { ExclusiveAddressUse = false };
                udpClient.Connect(serverIP, upMulticastServerPort);
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.JoinMulticastGroup(udpMulticastServerIP);
                UdpReceiver = udpClient;
            }

            protected ConcurrentQueue<byte[]> UdpSendQueue { get; } = new ConcurrentQueue<byte[]>();
            //protected ConcurrentQueue<byte[]> TcpSendQueue { get; } = new ConcurrentQueue<byte[]>();

            private void EnqueueBroadcast(byte[] bytes)
                => UdpSendQueue.Enqueue(bytes);

            protected void ConsumeAndSendUDPQueue(UdpClient client, IPEndPoint endPoint)
            {
                while (UdpSendQueue.TryDequeue(out byte[]? bytes))
                    client.Send(bytes, bytes.Length, endPoint);
            }

            public enum EBroadcastType : byte
            {
                Full,
                Property,
                Data,
                Transform
            }

            public void Broadcast(XRWorldObjectBase obj, bool udp, bool compress = true)
            {
                if (!obj.HasNetworkAuthority)
                    return;

                bool connected =/* udp ? */UDPServerConnectionEstablished/* : TCPConnectionEstablished*/;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(obj.ID, udp, compress, GetObjBytes(obj), EBroadcastType.Full);
                Task.Run(EncodeAndQueue);
            }

            protected static byte[] GetObjBytes(XRWorldObjectBase obj)
                => Encoding.UTF8.GetBytes(AssetManager.Serializer.Serialize(obj));

            public void BroadcastData(XRWorldObjectBase obj, object value, string idStr, bool udp, bool compress = true)
            {
                if (!obj.HasNetworkAuthority)
                    return;

                bool connected = /*udp ? */UDPServerConnectionEstablished/* : TCPConnectionEstablished*/;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(obj.ID, udp, compress, GetDataBytes(value, idStr), EBroadcastType.Data);
                Task.Run(EncodeAndQueue);
            }

            private static byte[] GetDataBytes(object value, string idStr)
                => Encoding.UTF8.GetBytes(AssetManager.Serializer.Serialize((idStr, value)));

            public void BroadcastPropertyUpdated<T>(XRWorldObjectBase obj, string? propName, T? value, bool udp, bool compress = true)
            {
                if (!obj.HasNetworkAuthority)
                    return;

                bool connected = /*udp ? */UDPServerConnectionEstablished /*: TCPConnectionEstablished*/;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(obj.ID, udp, compress, GetPropBytes(propName, value), EBroadcastType.Property);
                Task.Run(EncodeAndQueue);
            }

            protected byte[] Send(Guid id, bool udp, bool compress, byte[] data, EBroadcastType type)
            {
                byte flags = (byte)(((byte)type << 1) | (compress ? 1 : 0));

                byte[] allData;

                if (compress)
                {
                    byte[] uncompData = new byte[16 + data.Length];
                    int offset = 0;
                    SetGuidAndData(id, data, uncompData, ref offset);
                    byte[] compData = Compression.Compress(uncompData);

                    allData = new byte[8 + compData.Length];
                    Buffer.SetByte(allData, 0, flags);
                    Buffer.BlockCopy(BitConverter.GetBytes(compData.Length), 0, allData, 1, 3);
                    Buffer.BlockCopy(BitConverter.GetBytes(ElapsedTime), 0, allData, 4, 4);
                    Buffer.BlockCopy(compData, 0, allData, 8, compData.Length);
                }
                else
                {
                    allData = new byte[24 + data.Length];
                    Buffer.SetByte(allData, 0, flags);
                    Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, allData, 1, 3);
                    Buffer.BlockCopy(BitConverter.GetBytes(ElapsedTime), 0, allData, 4, 4);
                    int offset = 8;
                    SetGuidAndData(id, data, allData, ref offset);
                }

                EnqueueBroadcast(allData);

                return data;
            }

            protected static void SetGuidAndData(Guid id, byte[] data, byte[] allData, ref int offset)
            {
                Buffer.BlockCopy(id.ToByteArray(), 0, allData, offset, 16);
                offset += 16;
                Buffer.BlockCopy(data, 0, allData, offset, data.Length);
                offset += data.Length;
            }

            protected static byte[] GetPropBytes<T>(string? propName, T? value)
                => Encoding.UTF8.GetBytes(AssetManager.Serializer.Serialize((propName, value)));

            protected readonly byte[] _decompBuffer = new byte[4096];
            //private readonly byte[] _tcpInBuffer = new byte[1024];
            protected readonly byte[] _udpInBuffer = new byte[1024];
            //private int _tcpBufferOffset = 0;
            protected int _udpBufferOffset = 0;

            protected static void ReadReceivedData(byte[] inBuf, ref int inBufOffset, byte[] decompBuffer)
            {
                int offset = 0;
                while (inBufOffset >= 8)
                {
                    int dataLength = BitConverter.ToInt32(inBuf, offset) & 0x00FFFFFF;
                    if (inBufOffset < dataLength + 8)
                        continue;
                    
                    //We can parse the full packet now
                    byte flag = inBuf[offset];
                    bool compressed = (flag & 1) == 1;
                    EBroadcastType type = (EBroadcastType)((flag >> 1) & 3);
                    offset += 4;
                    float elapsed = BitConverter.ToSingle(inBuf, offset);
                    offset += 4;

                    if (compressed)
                    {
                        int decompLen = Compression.Decompress(inBuf, offset, dataLength, decompBuffer, 0);
                        Propogate(
                            new Guid([.. decompBuffer.Take(16)]),
                            type,
                            decompBuffer,
                            16,
                            decompLen - 16);
                    }
                    else
                    {
                        Propogate(
                            new Guid([.. inBuf.Take(16)]),
                            type,
                            inBuf,
                            offset + 16,
                            dataLength - 16);
                    }

                    inBufOffset -= dataLength + 8;
                }
            }

            protected static void ReadHeader(
                byte[] header,
                out bool compress,
                out EBroadcastType type,
                out int dataLength,
                out float elapsed,
                int offset = 0)
            {
                byte flag = header[offset];
                compress = (flag & 1) == 1;
                type = (EBroadcastType)((flag >> 1) & 3);
                dataLength = BitConverter.ToInt32(header, offset) & 0x00FFFFFF;
                elapsed = BitConverter.ToSingle(header, offset + 4);
            }

            /// <summary>
            /// Finds the target object in the cache and applies the data to it.
            /// </summary>
            /// <param name="id"></param>
            /// <param name="type"></param>
            /// <param name="data"></param>
            /// <param name="dataOffset"></param>
            /// <param name="dataLen"></param>
            protected static void Propogate(Guid id, EBroadcastType type, byte[] data, int dataOffset, int dataLen)
            {
                if (!XRObjectBase.ObjectsCache.TryGetValue(id, out var obj) || obj is not XRWorldObjectBase worldObj)
                    return;

                string dataStr = Encoding.UTF8.GetString(data, dataOffset, dataLen);

                switch (type)
                {
                    case EBroadcastType.Full:
                        {
                            var newObj = AssetManager.Deserializer.Deserialize(dataStr, worldObj.GetType()) as XRWorldObjectBase;
                            if (newObj is not null)
                                worldObj.CopyFrom(newObj);
                            break;
                        }
                    case EBroadcastType.Property:
                        {
                            var (propName, value) = AssetManager.Deserializer.Deserialize<(string propName, object value)>(dataStr);
                            worldObj.SetReplicatedProperty(propName, value);
                            break;
                        }
                    case EBroadcastType.Data:
                        {
                            var (id2, value2) = AssetManager.Deserializer.Deserialize<(string id, object data)>(dataStr);
                            worldObj.ReceiveData(id2, value2);
                            break;
                        }
                    case EBroadcastType.Transform:
                        {
                            if (worldObj is TransformBase transform)
                                transform.DecodeFromBytes(data);
                            break;
                        }
                }
            }

            public void ReplicateTransform(TransformBase transform)
            {
                bool connected = UDPServerConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(transform.ID, true, true, transform.EncodeToBytes(), EBroadcastType.Transform);
                Task.Run(EncodeAndQueue);
            }

            [Flags]
            protected enum ETransformValueFlags
            {
                Quats = 1,
                Vector3s = 2,
                Rotators = 4,
                Scalars = 8,
                Ints = 16
            }

            protected static ETransformValueFlags MakeFlags((object value, int bitsPerComponent)[] values)
            {
                ETransformValueFlags flags = 0;
                foreach (var value in values)
                {
                    if (value.value is Quaternion)
                        flags |= ETransformValueFlags.Quats;
                    else if (value.value is Vector3)
                        flags |= ETransformValueFlags.Vector3s;
                    else if (value.value is Rotator)
                        flags |= ETransformValueFlags.Rotators;
                    else if (value.value is float)
                        flags |= ETransformValueFlags.Scalars;
                    else if (value.value is int)
                        flags |= ETransformValueFlags.Ints;
                }
                return flags;
            }

            ///// <summary>
            ///// Receives from server or from other clients in a p2p scenario.
            ///// </summary>
            //public TcpClient? TcpReceiver { get; set; }
            ///// <summary>
            ///// Sends from client to server.
            ///// </summary>
            //public TcpClient? TcpSender { get; set; }
            ///// <summary>
            ///// Listener for incoming TCP connections.
            ///// </summary>
            //public TcpListener? TcpListener { get; set; }
            ///// <summary>
            ///// List of TCP clients connected to this server, or in a p2p scenario, connected to this client.
            ///// </summary>
            //public List<TcpClient> TcpClients { get; } = [];

            //public bool TCPConnectionEstablished => TcpReceiver?.Connected ?? false;

            //private void StartTcpSender(IPAddress serverIP, int tcpPort)
            //{
            //    TcpSender = new TcpClient();
            //    if (ServerIP is not null)
            //        TcpSender.Connect(serverIP, tcpPort);
            //}

            //private void StartTcpListener(IPAddress tcpListenerIP, int tcpListenerPort)
            //{
            //    TcpListener = new TcpListener(tcpListenerIP, tcpListenerPort);
            //    TcpListener.Start();
            //}

            //private void ClientSendTcp()
            //{
            //    //Send outgoing data
            //    if (PeerToPeer)
            //    {
            //        //Multicast to other clients like a server would
            //        BroadcastToTcpClients();
            //    }
            //    else
            //    {
            //        //Send directly to server
            //        SendDirectTcp();
            //    }
            //}

            //private void SendDirectTcp()
            //{
            //    if (TcpReceiver is null)
            //        return;

            //    NetworkStream stream = TcpReceiver.GetStream();
            //    while (TcpSendQueue.TryDequeue(out byte[]? bytes))
            //    {
            //        stream.Write(bytes, 0, bytes.Length);
            //        stream.Flush();
            //    }
            //}

            //private void BroadcastToTcpClients()
            //{
            //    AcceptClientConnections();
            //    while (TcpSendQueue.TryDequeue(out byte[]? bytes))
            //    {
            //        lock (TcpClients)
            //        {
            //            List<TcpClient> disconnectedClients = [];
            //            foreach (var client in TcpClients)
            //            {
            //                try
            //                {
            //                    if (client.Connected)
            //                    {
            //                        NetworkStream stream = client.GetStream();
            //                        stream.Write(bytes, 0, bytes.Length);
            //                        stream.Flush();
            //                    }
            //                    else
            //                    {
            //                        Debug.Out("Client disconnected");
            //                        disconnectedClients.Add(client);
            //                    }
            //                }
            //                catch
            //                {
            //                    Debug.Out("Client disconnected");
            //                    disconnectedClients.Add(client);
            //                }
            //            }
            //            foreach (var client in disconnectedClients)
            //            {
            //                TcpClients.Remove(client);
            //                client.Close();
            //            }
            //        }
            //    }
            //}

            //private void AcceptClientConnections()
            //{
            //    while (TcpListener?.Pending() ?? false)
            //    {
            //        var client = TcpListener.AcceptTcpClient();
            //        lock (TcpClients)
            //        {
            //            TcpClients.Add(client);
            //        }
            //    }
            //}

            //private async Task ReadTCP()
            //{
            //    if (!(TcpReceiver?.Connected ?? false))
            //        return;

            //    NetworkStream stream = TcpReceiver.GetStream();
            //    while (stream.DataAvailable)
            //    {
            //        int bytesRead = await stream.ReadAsync(_tcpInBuffer.AsMemory(_tcpBufferOffset, _tcpInBuffer.Length - _tcpBufferOffset));
            //        _tcpBufferOffset += bytesRead;
            //    }

            //    ReadReceivedData(_tcpInBuffer, ref _tcpBufferOffset, _decompBuffer);
            //}
        }
    }
}
