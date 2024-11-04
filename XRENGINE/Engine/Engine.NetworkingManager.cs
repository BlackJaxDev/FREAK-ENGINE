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
        public class NetworkingManager : XRBase
        {
            /// <summary>
            /// Determines if this process is the host server.
            /// The server can also be a client in a multicast scenario with no dedicated server.
            /// </summary>
            public bool IsServer { get; set; } = true;
            /// <summary>
            /// Determines if this process is a client.
            /// Clients connect to a server to send and receive remote player updates.
            /// </summary>
            public bool IsClient { get; set; } = true;
            /// <summary>
            /// Sends from server to all connected clients.
            /// Or, in a p2p scenario, sends from client to all other clients.
            /// </summary>
            public UdpClient? UdpMulticastSender { get; set; }
            /// <summary>
            /// Receives from server or from other clients in a p2p scenario.
            /// </summary>
            public UdpClient? UdpReceiver { get; set; }
            /// <summary>
            /// Sends from client to server.
            /// Not used in a p2p scenario.
            /// </summary>
            public UdpClient? UdpSender { get; set; }
            public IPEndPoint? ServerIP { get; set; }
            public IPEndPoint? MulticastEndPoint { get; set; }
            /// <summary>
            /// Receives from server or from other clients in a p2p scenario.
            /// </summary>
            public TcpClient? TcpReceiver { get; set; }
            /// <summary>
            /// Sends from client to server.
            /// </summary>
            public TcpClient? TcpSender { get; set; }
            /// <summary>
            /// Listener for incoming TCP connections.
            /// </summary>
            public TcpListener? TcpListener { get; set; }
            /// <summary>
            /// List of TCP clients connected to this server, or in a p2p scenario, connected to this client.
            /// </summary>
            public List<TcpClient> TcpClients { get; } = [];
            /// <summary>
            /// Used for interacting with the game's API.
            /// </summary>
            public RestClient? RestClient { get; set; }
            public bool TCPConnectionEstablished => TcpReceiver?.Connected ?? false;
            public bool UDPServerConnectionEstablished => UdpReceiver?.Client.Connected ?? false;

            public void StartClient(
                IPAddress udpMulticastGroupIP,
                IPAddress serverIP,
                int udpPort,
                int tcpPort)
            {
                Debug.Out($"Starting client with udp multicast group ({udpMulticastGroupIP}:{udpPort}) sending to server at ({serverIP}:{udpPort})");
                IsClient = true;

                //Receive from server
                StartUdpMulticastReceiver(udpMulticastGroupIP, udpPort);
                StartTcpListener(IPAddress.Any, tcpPort);

                //Send to server
                StartUdpSender(serverIP, udpPort);
                StartTcpSender(serverIP, tcpPort);

                if (PeerToPeer)
                {
                    //StartUdpReceiver(udpMulticastServerPort);
                    //StartTcpListener(IPAddress.Any, udpMulticastServerPort);
                }

                Time.Timer.SwapBuffers += SwapClient;
            }

            private void StartTcpSender(IPAddress serverIP, int tcpPort)
            {
                TcpSender = new TcpClient();
                if (ServerIP is not null)
                    TcpSender.Connect(serverIP, tcpPort);
            }

            private void StartUdpSender(IPAddress serverIP, int udpMulticastServerPort)
            {
                UdpSender = new UdpClient();
                ServerIP = new IPEndPoint(serverIP, udpMulticastServerPort);
            }

            public void StartServer(
                IPAddress udpMulticastGroupIP,
                int udpPort,
                IPAddress tcpListenerIP,
                int tcpPort)
            {
                Debug.Out($"Starting server at udp({udpMulticastGroupIP}:{udpPort}) and tcp({tcpListenerIP}:{tcpPort})");
                IsServer = true;

                //Send to clients
                StartUdpMulticastSender(udpMulticastGroupIP, udpPort);

                //Receive from clients
                StartUdpReceiver(udpPort);

                //Accept incoming TCP connections
                StartTcpListener(tcpListenerIP, tcpPort);

                Time.Timer.SwapBuffers += SwapServer;
            }

            private void StartUdpReceiver(int udpPort)
            {
                UdpClient listener = new();
                // Allow multiple clients to use the same port
                listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));
            }

            private void StartUdpMulticastSender(IPAddress udpMulticastIP, int udpMulticastPort)
            {
                UdpMulticastSender = new UdpClient { MulticastLoopback = false };
                MulticastEndPoint = new IPEndPoint(udpMulticastIP, udpMulticastPort);
            }
            private void StartUdpMulticastReceiver(IPAddress udpMulticastServerIP, int upMulticastServerPort)
            {
                UdpClient udpClient = new() { ExclusiveAddressUse = false };
                //Join the multicast group
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, upMulticastServerPort));
                try
                {
                    udpClient.JoinMulticastGroup(udpMulticastServerIP);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                UdpReceiver = udpClient;
            }

            private void StartTcpListener(IPAddress tcpListenerIP, int tcpListenerPort)
            {
                TcpListener = new TcpListener(tcpListenerIP, tcpListenerPort);
                TcpListener.Start();
            }

            private void SwapServer()
            {
                //Read incoming data from clients
                Task.Run(ReadTCP);
                Task.Run(ReadUDP);

                //Send outgoing data to clients
                Task.Run(SendMulticastUdp);
                Task.Run(BroadcastToTcpClients);
            }
            private void SwapClient()
            {
                //Read incoming data from server or other clients
                Task.Run(ReadTCP);
                Task.Run(ReadUDP);

                //Send outgoing data to server or other clients
                Task.Run(ClientSendUdp);
                Task.Run(ClientSendTcp);
            }

            private void ClientSendTcp()
            {
                //Send outgoing data
                if (PeerToPeer)
                {
                    //Multicast to other clients like a server would
                    BroadcastToTcpClients();
                }
                else
                {
                    //Send directly to server
                    SendDirectTcp();
                }
            }

            private void ClientSendUdp()
            {
                //Send outgoing data
                if (PeerToPeer)
                {
                    //Multicast to other clients like a server would
                    SendMulticastUdp();
                }
                else
                {
                    //Send directly to server
                    SendDirectUdp();
                }
            }

            private void SendDirectTcp()
            {
                if (TcpReceiver is null)
                    return;
                
                NetworkStream stream = TcpReceiver.GetStream();
                while (TcpSendQueue.TryDequeue(out byte[]? bytes))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
            }

            private void SendMulticastUdp()
            {
                if (UdpMulticastSender is not null && MulticastEndPoint is not null)
                    BroadcastUdp(UdpMulticastSender, MulticastEndPoint);
            }

            private void SendDirectUdp()
            {
                if (UdpSender is not null && ServerIP is not null)
                    BroadcastUdp(UdpSender, ServerIP);
            }

            private ConcurrentQueue<byte[]> UdpSendQueue { get; } = new ConcurrentQueue<byte[]>();
            private ConcurrentQueue<byte[]> TcpSendQueue { get; } = new ConcurrentQueue<byte[]>();
            public bool PeerToPeer { get; set; } = false;

            private void EnqueueBroadcast(byte[] bytes, bool udp)
            {
                if (udp)
                    UdpSendQueue.Enqueue(bytes);
                else
                    TcpSendQueue.Enqueue(bytes);
            }

            private void BroadcastUdp(UdpClient client, IPEndPoint endPoint)
            {
                while (UdpSendQueue.TryDequeue(out byte[]? bytes))
                    client.Send(bytes, bytes.Length, endPoint);
            }

            private void BroadcastToTcpClients()
            {
                AcceptClientConnections();
                while (TcpSendQueue.TryDequeue(out byte[]? bytes))
                {
                    lock (TcpClients)
                    {
                        List<TcpClient> disconnectedClients = [];
                        foreach (var client in TcpClients)
                        {
                            try
                            {
                                if (client.Connected)
                                {
                                    NetworkStream stream = client.GetStream();
                                    stream.Write(bytes, 0, bytes.Length);
                                    stream.Flush();
                                }
                                else
                                {
                                    Debug.Out("Client disconnected");
                                    disconnectedClients.Add(client);
                                }
                            }
                            catch
                            {
                                Debug.Out("Client disconnected");
                                disconnectedClients.Add(client);
                            }
                        }
                        foreach (var client in disconnectedClients)
                        {
                            TcpClients.Remove(client);
                            client.Close();
                        }
                    }
                }
            }

            private void AcceptClientConnections()
            {
                while (TcpListener?.Pending() ?? false)
                {
                    var client = TcpListener.AcceptTcpClient();
                    lock (TcpClients)
                    {
                        TcpClients.Add(client);
                    }
                }
            }

            public enum EBroadcastType : byte
            {
                Full,
                Prop,
                Data,
                Transform
            }

            public void Broadcast(XRWorldObjectBase obj, bool udp, bool compress = true)
            {
                if (!obj.HasAuthority)
                    return;

                bool connected = udp ? UDPServerConnectionEstablished : TCPConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(obj.ID, udp, compress, GetObjBytes(obj), EBroadcastType.Full);
                Task.Run(EncodeAndQueue);
            }

            private static byte[] GetObjBytes(XRWorldObjectBase obj)
                => Encoding.UTF8.GetBytes(AssetManager.Serializer.Serialize(obj));

            public void BroadcastData(XRWorldObjectBase obj, object value, string idStr, bool udp, bool compress = true)
            {
                if (!obj.HasAuthority)
                    return;

                bool connected = udp ? UDPServerConnectionEstablished : TCPConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(obj.ID, udp, compress, GetDataBytes(value, idStr), EBroadcastType.Data);
                Task.Run(EncodeAndQueue);
            }

            private static byte[] GetDataBytes(object value, string idStr)
                => Encoding.UTF8.GetBytes(AssetManager.Serializer.Serialize((idStr, value)));

            public void BroadcastPropertyUpdated<T>(XRWorldObjectBase obj, string? propName, T? value, bool udp, bool compress = true)
            {
                if (!obj.HasAuthority)
                    return;

                bool connected = udp ? UDPServerConnectionEstablished : TCPConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(obj.ID, udp, compress, GetPropBytes(propName, value), EBroadcastType.Prop);
                Task.Run(EncodeAndQueue);
            }

            private byte[] Send(Guid id, bool udp, bool compress, byte[] data, EBroadcastType type)
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

                EnqueueBroadcast(allData, udp);

                return data;
            }

            private static void SetGuidAndData(Guid id, byte[] data, byte[] allData, ref int offset)
            {
                Buffer.BlockCopy(id.ToByteArray(), 0, allData, offset, 16);
                offset += 16;
                Buffer.BlockCopy(data, 0, allData, offset, data.Length);
                offset += data.Length;
            }

            private static byte[] GetPropBytes<T>(string? propName, T? value)
                => Encoding.UTF8.GetBytes(AssetManager.Serializer.Serialize((propName, value)));

            private readonly byte[] _decompBuffer = new byte[4096];
            private readonly byte[] _tcpInBuffer = new byte[1024];
            private readonly byte[] _udpInBuffer = new byte[1024];
            private int _tcpBufferOffset = 0;
            private int _udpBufferOffset = 0;

            private async Task ReadUDP()
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

            private async Task ReadTCP()
            {
                if (!(TcpReceiver?.Connected ?? false))
                    return;

                NetworkStream stream = TcpReceiver.GetStream();
                while (stream.DataAvailable)
                {
                    int bytesRead = await stream.ReadAsync(_tcpInBuffer.AsMemory(_tcpBufferOffset, _tcpInBuffer.Length - _tcpBufferOffset));
                    _tcpBufferOffset += bytesRead;
                }

                ReadReceivedData(_tcpInBuffer, ref _tcpBufferOffset, _decompBuffer);
            }

            private static void ReadReceivedData(byte[] inBuf, ref int inBufOffset, byte[] decompBuffer)
            {
                int offset = 0;
                while (inBufOffset >= 8)
                {
                    int dataLength = BitConverter.ToInt32(inBuf, offset) & 0x00FFFFFF;
                    if (inBufOffset >= dataLength + 8)
                    {
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
                                new Guid(decompBuffer.Take(16).ToArray()),
                                type,
                                decompBuffer,
                                16,
                                decompLen - 16);
                        }
                        else
                        {
                            Propogate(
                                new Guid(inBuf.Take(16).ToArray()),
                                type,
                                inBuf,
                                offset + 16,
                                dataLength - 16);
                        }

                        inBufOffset -= dataLength + 8;
                    }
                }
            }

            private static void ReadHeader(
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
            private static void Propogate(Guid id, EBroadcastType type, byte[] data, int dataOffset, int dataLen)
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
                    case EBroadcastType.Prop:
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
            private enum ETransformValueFlags
            {
                Quats = 1,
                Vector3s = 2,
                Rotators = 4,
                Scalars = 8,
                Ints = 16
            }

            private static ETransformValueFlags MakeFlags((object value, int bitsPerComponent)[] values)
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
        }
    }
}
