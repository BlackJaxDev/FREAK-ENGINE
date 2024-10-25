using RestSharp;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using XREngine.Data;
using XREngine.Data.Core;

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
            public UdpClient? UdpServer { get; set; }
            public UdpClient? UdpClient { get; set; }
            public IPEndPoint? MulticastEndPoint { get; set; }
            public TcpClient? TcpClient { get; set; }
            public TcpListener? TcpListener { get; set; }
            public List<TcpClient> TcpClients { get; } = [];
            public RestClient? RestClient { get; set; }
            public bool TCPConnectionEstablished => TcpClient?.Connected ?? false;
            public bool UDPConnectionEstablished => UdpClient?.Client.Connected ?? false;

            public void StartClient()
            {
                IsClient = true;

                // Multicast group IP and port (must match the server)
                string multicastIP = "239.0.0.222";
                int multicastPort = 5000;

                UdpClient udpClient = new()
                {
                    ExclusiveAddressUse = false
                };

                // Join multicast group
                IPEndPoint localEndPoint = new(IPAddress.Any, multicastPort);
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.Client.Bind(localEndPoint);
                udpClient.JoinMulticastGroup(IPAddress.Parse(multicastIP));

                Time.Timer.SwapBuffers += SwapClient;
            }

            private void SwapClient()
            {
                Task.Run(ReadTCP);
                Task.Run(ReadUDP);
            }

            private async Task ReadUDP()
            {
                if (!(UdpClient?.Client.Connected ?? false))
                    return;
                
                //IPEndPoint endPoint = new(IPAddress.Any, 5001);
                while (UdpClient.Available > 0)
                {
                    var result = await UdpClient.ReceiveAsync();

                    byte[] allData = result.Buffer;
                    if (allData.Length < 24)
                        continue;

                    Guid id = new(allData.Take(16).ToArray());
                    EBroadcastType type = (EBroadcastType)BitConverter.ToInt32(allData, 16);
                    int dataLength = BitConverter.ToInt32(allData, 20);

                    byte[] data = new byte[dataLength];
                    Array.Copy(allData, 24, data, 0, dataLength);
                    Propogate(id, type, data);
                }
            }

            private async Task ReadTCP()
            {
                if (!(TcpClient?.Connected ?? false))
                    return;
                
                NetworkStream stream = TcpClient.GetStream();
                while (stream.DataAvailable)
                {
                    byte[] headerBuf = new byte[24];
                    int bytesRead = await stream.ReadAsync(headerBuf);
                    if (bytesRead != 24)
                        continue;

                    Guid id = new(headerBuf.Take(16).ToArray());
                    EBroadcastType type = (EBroadcastType)BitConverter.ToInt32(headerBuf, 16);
                    int dataLength = BitConverter.ToInt32(headerBuf, 20);

                    byte[] data = new byte[dataLength];
                    await stream.ReadAsync(data);
                    Propogate(id, type, data);
                }
            }

            private static void Propogate(Guid id, EBroadcastType type, byte[] data)
            {
                if (!XRObjectBase.ObjectsCache.TryGetValue(id, out var obj) || obj is not XRWorldObjectBase worldObj)
                    return;

                //TODO: include a flag to determine if the data is compressed
                data = Compression.Decompress(data);
                string dataStr = System.Text.Encoding.UTF8.GetString(data);

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
                }
            }

            public void StartServer()
            {
                TcpListener = new TcpListener(IPAddress.Any, 5000);

                // Multicast group IP and port
                string multicastIP = "239.0.0.222"; // Multicast address range: 224.0.0.0 to 239.255.255.255
                int multicastPort = 5001;

                // Initialize UDP client for multicast
                UdpServer = new UdpClient
                {
                    MulticastLoopback = false // Disable loopback if not needed
                };
                MulticastEndPoint = new IPEndPoint(IPAddress.Parse(multicastIP), multicastPort);

                IsServer = true;
                Time.Timer.SwapBuffers += SwapServer;
            }

            private void SwapServer()
            {
                AcceptClientConnections();
                if (UdpServer is not null && MulticastEndPoint is not null)
                    BroadcastUdp(UdpServer, MulticastEndPoint);
                BroadcastToTcpClients();
            }

            
            private ConcurrentQueue<byte[]> UdpBroadcastQueue { get; } = new ConcurrentQueue<byte[]>();
            private ConcurrentQueue<byte[]> TcpBroadcastQueue { get; } = new ConcurrentQueue<byte[]>();
            private void EnqueueBroadcast(byte[] bytes, bool udp)
            {
                if (udp)
                    UdpBroadcastQueue.Enqueue(bytes);
                else
                    TcpBroadcastQueue.Enqueue(bytes);
            }

            private void BroadcastUdp(UdpClient client, IPEndPoint endPoint)
            {
                while (UdpBroadcastQueue.TryDequeue(out byte[]? bytes))
                    client.Send(bytes, bytes.Length, endPoint);
            }

            private void BroadcastToTcpClients()
            {
                lock (TcpClients)
                {
                    List<TcpClient> disconnectedClients = [];
                    foreach (var client in TcpClients)
                    {
                        try
                        {
                            NetworkStream stream = client.GetStream();
                            while (TcpBroadcastQueue.TryDequeue(out byte[]? bytes))
                                stream.Write(bytes, 0, bytes.Length);
                            stream.Flush();
                        }
                        catch
                        {
                            Console.WriteLine("Client disconnected");
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

            public void Connect(string ip, int udpPort, int tcpPort)
            {
                TcpClient = new TcpClient(ip, tcpPort);
                UdpServer = new UdpClient(ip, udpPort);
                IsServer = false;
            }

            public enum EBroadcastType
            {
                Full,
                Prop,
                Data
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public unsafe struct Header
            {
                public fixed byte ID[16];
                public int Type;
                public int DataLength;

                public Guid GetID()
                {
                    fixed (byte* ptr = ID)
                    {
                        byte[] id = new byte[16];
                        for (int i = 0; i < 16; i++)
                            id[i] = ptr[i];
                        return new Guid(id);
                    }
                }
                public void SetID(Guid id)
                {
                    byte[] idBytes = id.ToByteArray();
                    fixed (byte* ptr = ID)
                    {
                        for (int i = 0; i < 16; i++)
                            ptr[i] = idBytes[i];
                    }
                }
                public readonly EBroadcastType GetTypeValue()
                    => (EBroadcastType)Type;
                public void SetTypeValue(EBroadcastType type)
                    => Type = (int)type;
            }

            public void Broadcast(XRWorldObjectBase obj, bool udp, bool compress = true)
            {
                bool connected = udp ? UDPConnectionEstablished : TCPConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue()
                {
                    byte[] id = obj.ID.ToByteArray();
                    byte[] type = BitConverter.GetBytes((int)EBroadcastType.Full);

                    string dataStr = AssetManager.Serializer.Serialize(obj);
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);
                    byte[] dataLen = BitConverter.GetBytes(data.Length);

                    var arr = id.Concat(type).Concat(dataLen).Concat(data).ToArray();
                    if (compress)
                        arr = Compression.Compress(arr);

                    EnqueueBroadcast(arr, udp);
                }
                Task.Run(EncodeAndQueue);
            }

            public void BroadcastData(XRWorldObjectBase obj, object value, string idStr, bool udp, bool compress = true)
            {
                bool connected = udp ? UDPConnectionEstablished : TCPConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue()
                {
                    byte[] id = obj.ID.ToByteArray();
                    byte[] type = BitConverter.GetBytes((int)EBroadcastType.Data);

                    string dataStr = AssetManager.Serializer.Serialize((idStr, value));
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);
                    byte[] dataLen = BitConverter.GetBytes(data.Length);

                    var arr = id.Concat(type).Concat(dataLen).Concat(data).ToArray();
                    if (compress)
                        arr = Compression.Compress(arr);

                    EnqueueBroadcast(arr, udp);
                }
                Task.Run(EncodeAndQueue);
            }

            public void BroadcastPropertyUpdated<T>(XRWorldObjectBase obj, string? propName, T? value, bool udp, bool compress = true)
            {
                bool connected = udp ? UDPConnectionEstablished : TCPConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue()
                {
                    byte[] id = obj.ID.ToByteArray();
                    byte[] type = BitConverter.GetBytes((int)EBroadcastType.Prop);

                    string dataStr = AssetManager.Serializer.Serialize((propName, value));
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);
                    byte[] dataLen = BitConverter.GetBytes(data.Length);

                    var arr = id.Concat(type).Concat(dataLen).Concat(data).ToArray();
                    if (compress)
                        arr = Compression.Compress(arr);

                    EnqueueBroadcast(arr, udp);
                }
                Task.Run(EncodeAndQueue);
            }
        }
    }
}
