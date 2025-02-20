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
            protected ConcurrentQueue<(ushort sequenceNum, byte[])> UdpSendQueue { get; } = new();

            public abstract bool IsServer { get; }
            public abstract bool IsClient { get; }
            public abstract bool IsP2P { get; }

            public bool UDPServerConnectionEstablished
                => UdpReceiver?.Client.Connected ?? false;

            public BaseNetworkingManager()
                => Time.Timer.UpdateFrame += ConsumeQueues;
            ~BaseNetworkingManager()
                => Time.Timer.UpdateFrame -= ConsumeQueues;
            
            /// <summary>
            /// Sends from server to all connected clients, or from client to all other p2p clients.
            /// </summary>
            public UdpClient? UdpMulticastSender { get; set; }
            /// <summary>
            /// Receives from server or from other p2p clients.
            /// </summary>
            public UdpClient? UdpReceiver { get; set; }
            public IPEndPoint? MulticastEndPoint { get; set; }

            protected abstract Task SendUDP();
            protected virtual async Task ReadUDP()
            {
                //if (!(UdpReceiver?.Client.Connected ?? false))
                //    return;
                if (UdpReceiver is null)
                    return;

                bool anyData = false;
                while (anyData |= UdpReceiver.Available > 0)
                {
                    UdpReceiveResult result = await UdpReceiver.ReceiveAsync();
                    byte[] allData = result.Buffer;
                    int maxLen = _udpBufferOffset + allData.Length;
                    while (maxLen > _udpInBuffer.Length)
                    {
                        Debug.Out($"UDP buffer overflow, doubling buffer size to {_udpInBuffer.Length * 2}");
                        byte[] newBuffer = new byte[_udpInBuffer.Length * 2];
                        Buffer.BlockCopy(_udpInBuffer, 0, newBuffer, 0, _udpBufferOffset);
                        _udpInBuffer = newBuffer;
                    }
                    Buffer.BlockCopy(allData, 0, _udpInBuffer, _udpBufferOffset, allData.Length);
                    _udpBufferOffset += allData.Length;
                    ReadReceivedData(_udpInBuffer, ref _udpBufferOffset, _decompBuffer);
                }
            }
            public virtual async void ConsumeQueues()
            {
                var read = Task.Run(ReadUDP);
                var send = Task.Run(SendUDP);
                await Task.WhenAll(read, send);
            }

            /// <summary>
            /// Run on server or p2p client - sends to all clients
            /// </summary>
            /// <param name="udpMulticastIP"></param>
            /// <param name="udpMulticastPort"></param>
            protected void StartUdpMulticastSender(IPAddress udpMulticastIP, int udpMulticastPort)
            {
                UdpClient udpClient = new() { /*ExclusiveAddressUse = false*/ };
                UdpMulticastSender = udpClient;
                MulticastEndPoint = new IPEndPoint(udpMulticastIP, udpMulticastPort);
                //UdpMulticastSender.Connect(MulticastEndPoint);
            }

            /// <summary>
            /// Run on client or p2p client - receives from server
            /// </summary>
            /// <param name="udpMulticastServerIP"></param>
            /// <param name="upMulticastServerPort"></param>
            protected void StartUdpMulticastReceiver(IPAddress serverIP, IPAddress udpMulticastServerIP, int upMulticastServerPort)
            {
                UdpClient udpClient = new(upMulticastServerPort) { /*ExclusiveAddressUse = false,*/ MulticastLoopback = false };
                //udpClient.Connect(serverIP, upMulticastServerPort);
                //udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.JoinMulticastGroup(udpMulticastServerIP);
                UdpReceiver = udpClient;
            }

            private void EnqueueBroadcast(ushort sequenceNum, byte[] bytes)
                => UdpSendQueue.Enqueue((sequenceNum, bytes));
            
            private float _maxRoundTripSec = 1.0f;
            public float MaxRoundTripSec
            {
                get => _maxRoundTripSec;
                set => SetField(ref _maxRoundTripSec, value);
            }

            private readonly ConcurrentDictionary<ushort, float> _rttBuffer = [];

            protected async Task ConsumeAndSendUDPQueue(UdpClient? client, IPEndPoint? endPoint)
            {
                if (UdpSendQueue.IsEmpty)
                    return;

                ClearOldRTTs();

                //Send queue MUST be consumed so it doesn't grow infinitely

                //Debug.Out($"Sending {UdpSendQueue.Count} udp packets");
                while (UdpSendQueue.TryDequeue(out (ushort sequenceNum, byte[] bytes) data))
                {
                    if (!_rttBuffer.ContainsKey(data.sequenceNum))
                        _rttBuffer[data.sequenceNum] = Engine.ElapsedTime;

                    if (client is null)
                        continue;

                    _ = await client.SendAsync(data.bytes, data.bytes.Length, endPoint);
                }
            }

            private void ClearOldRTTs()
            {
                if (_rttBuffer.Count <= 0)
                    return;
                
                float oldest = Engine.ElapsedTime - MaxRoundTripSec;
                ushort[] keys = new ushort[_rttBuffer.Count];
                _rttBuffer.Keys.CopyTo(keys, 0);
                foreach (ushort key in keys)
                    if (_rttBuffer[key] < oldest)
                    {
                        Debug.Out($"Packet sequence failed to return: {key}");
                        _rttBuffer.TryRemove(key, out _);
                    }
            }

            public enum EBroadcastType : byte
            {
                Full,
                Property,
                Data,
                Transform
            }

            //protocol header is only 3 bytes so the flag can come right after to align back to 4 bytes
            private static readonly byte[] Protocol = [0x46, 0x52, 0x4B]; // "FRK"
            private const ushort _halfMaxSeq = 32768;
            /// <summary>
            /// Compares two sequence numbers, accounting for the wrap-around point at half the maximum value.
            /// Returns true if left is greater than right, false otherwise.
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <returns></returns>
            private static bool SeqGreater(ushort left, ushort right) =>
                ((left > right) && (left - right <= _halfMaxSeq)) ||
                ((left < right) && (right - left > _halfMaxSeq));
            /// <summary>
            /// Returns the difference between two sequence numbers, accounting for the wrap-around point at half the maximum value.
            /// If left is greater than right, returns left - right.
            /// Else, returns the wrapped-around difference.
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <returns></returns>
            private static int DiffSeq(ushort left, ushort right)
                => left > right 
                ? left - right 
                : (left - 0) + (ushort.MaxValue - right) + 1; //+1, because if right is ushort.MaxValue and left is 0, the difference is 1

            private ushort _localSequence = 0;
            private readonly Deque<ushort> _receivedRemoteSequences = [];

            /// <summary>
            /// Broadcasts the entire object to all connected clients.
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="compress"></param>
            public void Broadcast(XRWorldObjectBase obj, bool compress)
            {
                //if (!obj.HasNetworkAuthority)
                //    return;

                bool connected = UDPServerConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(obj.ID, compress, Encoding.UTF8.GetBytes(AssetManager.Serializer.Serialize(obj)), EBroadcastType.Full);
                Task.Run(EncodeAndQueue);
            }

            /// <summary>
            /// Broadcasts arbitrary data to all connected clients.
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="value"></param>
            /// <param name="idStr"></param>
            /// <param name="compress"></param>
            public void BroadcastData(XRWorldObjectBase obj, object value, string idStr, bool compress)
            {
                //if (!obj.HasNetworkAuthority)
                //    return;

                //bool connected = UDPServerConnectionEstablished;
                //if (!connected)
                //    return;

                //Debug.Out($"Broadcasting data: {idStr} {value}");
                void EncodeAndQueue() => Send(obj.ID, compress, Encoding.UTF8.GetBytes(AssetManager.Serializer.Serialize((idStr, value))), EBroadcastType.Data);
                Task.Run(EncodeAndQueue);
            }

            /// <summary>
            /// Broadcasts a property update to all connected clients.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="obj"></param>
            /// <param name="propName"></param>
            /// <param name="value"></param>
            /// <param name="compress"></param>
            public void BroadcastPropertyUpdated<T>(XRWorldObjectBase obj, string? propName, T? value, bool compress)
            {
                //if (!obj.HasNetworkAuthority)
                //    return;

                bool connected = UDPServerConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(obj.ID, compress, Encoding.UTF8.GetBytes(AssetManager.Serializer.Serialize((propName, value))), EBroadcastType.Property);
                Task.Run(EncodeAndQueue);
            }

            /// <summary>
            /// Broadcasts a transform update to all connected clients.
            /// The transform handles the encoding and decoding of its own data.
            /// </summary>
            /// <param name="transform"></param>
            public void BroadcastTransform(TransformBase transform)
            {
                bool connected = UDPServerConnectionEstablished;
                if (!connected)
                    return;

                void EncodeAndQueue() => Send(transform.ID, false, transform.EncodeToBytes(), EBroadcastType.Transform);
                Task.Run(EncodeAndQueue);
            }

            private const int HeaderLen = 16; //3 bytes for protocol, 1 byte for flags, 2 bytes for sequence, 2 bytes for ack, 4 bytes for ack bitfield, 4 bytes for data length (not including header or guid)
            private const int GuidLen = 16; //Guid is always 16 bytes

            protected byte[] Send(Guid id, bool compress, byte[] data, EBroadcastType type)
            {
                ushort[] acks = ReadRemoteSeqs();

                byte flags = (byte)(((byte)type << 1) | (compress ? 1 : 0));
                _localSequence = _localSequence == ushort.MaxValue ? (ushort)0 : (ushort)(_localSequence + 1);
                ushort seq = _localSequence;
                bool noSeq = acks.Length == 0;
                ushort maxAck = noSeq ? (ushort)0 : acks[^1];
                uint prevAckBitfield = 0;
                if (!noSeq)
                    foreach (ushort ack in acks)
                    {
                        if (ack == maxAck)
                            continue;
                        int diff = DiffSeq(maxAck, ack);
                        if (diff > 32)
                            break;
                        prevAckBitfield |= (uint)(1 << (diff - 1));
                    }

                byte[] allData;
                int uncompDataLen = data.Length;

                if (compress)
                {
                    //First, compress guid and data together
                    byte[] uncompData = new byte[GuidLen + uncompDataLen];
                    int offset = 0;
                    SetGuidAndData(id, data, uncompData, ref offset);
                    byte[] compData = Compression.Compress(uncompData);

                    //Then, create the full packet with header
                    int compDataLen = compData.Length;
                    allData = new byte[HeaderLen + compDataLen];
                    SetHeader(flags, allData, compDataLen, seq, maxAck, prevAckBitfield);

                    Buffer.BlockCopy(compData, 0, allData, HeaderLen, compDataLen);
                }
                else
                {
                    allData = new byte[HeaderLen + GuidLen + uncompDataLen];
                    SetHeader(flags, allData, uncompDataLen, seq, maxAck, prevAckBitfield);

                    int offset = HeaderLen;
                    SetGuidAndData(id, data, allData, ref offset);
                }
                EnqueueBroadcast(seq, allData);
                return data;
            }

            private ushort[] ReadRemoteSeqs()
            {
                ushort[] acks;
                lock (_receivedRemoteSequences)
                    acks = [.. _receivedRemoteSequences];
                return acks;
            }
            private void WriteToRemoteSeqs(ushort seq)
            {
                //If the sequence is more recent than the last one we received, update the last received sequence
                lock (_receivedRemoteSequences)
                {
                    bool noSeq = _receivedRemoteSequences.Count == 0;
                    if (noSeq || SeqGreater(seq, _receivedRemoteSequences.PeekBack()))
                    {
                        _receivedRemoteSequences.PushBack(seq);
                        if (_receivedRemoteSequences.Count > 33) //32 bits + 1 for max ack
                            _receivedRemoteSequences.PopFront();
                    }
                }
            }

            private static void SetHeader(byte flags, byte[] allData, int dataLen, ushort seq, ushort ack, uint ackBitfield)
            {
                //When we compose packet headers,
                //the local sequence becomes the sequence number of the packet,
                //and the remote sequence becomes the ack.
                //The ack bitfield is calculated by looking into a queue of up to 33 packets,
                //containing sequence numbers in the range [remote sequence - 32, remote sequence].
                //We set bit n (in [1,32]) in ack bits to 1 if the sequence number remote sequence - n is in the received queue.
                for (int i = 0; i < 3; i++)
                    Buffer.SetByte(allData, i, Protocol[i]);
                Buffer.SetByte(allData, 3, flags);
                //set sequence
                Buffer.BlockCopy(BitConverter.GetBytes(seq), 0, allData, 4, 2);
                //set ack
                Buffer.BlockCopy(BitConverter.GetBytes(ack), 0, allData, 6, 2);
                //set ack bitfield
                Buffer.BlockCopy(BitConverter.GetBytes(ackBitfield), 0, allData, 8, 4);
                //set data length
                Buffer.BlockCopy(BitConverter.GetBytes(dataLen), 0, allData, 12, 4);
            }

            protected static void SetGuidAndData(Guid id, byte[] data, byte[] allData, ref int offset)
            {
                Buffer.BlockCopy(id.ToByteArray(), 0, allData, offset, 16);
                offset += 16;
                Buffer.BlockCopy(data, 0, allData, offset, data.Length);
                offset += data.Length;
            }

            protected byte[] _decompBuffer = new byte[400000];
            protected byte[] _udpInBuffer = new byte[4096];
            protected int _udpBufferOffset = 0;
            private (bool compress, EBroadcastType type, ushort seq, ushort ack, uint ackBitfield, int dataLength)? _lastConsumedHeader;
            
            protected void ReadReceivedData(byte[] inBuf, ref int availableDataLen, byte[] decompBuffer)
            {
                int offset = 0;
                while (EnoughDataLeft(availableDataLen))
                {
                    //If we were in the middle of reading a packet, finish it
                    if (_lastConsumedHeader is not null)
                    {
                        (bool compress, EBroadcastType type, ushort seq, ushort ack, uint ackBitfield, int dataLength) header = _lastConsumedHeader.Value;
                        ReadPacketData(header.compress, header.type, inBuf, ref availableDataLen, decompBuffer, ref offset, header.dataLength);
                        _lastConsumedHeader = null;
                        continue;
                    }

                    //Search for protocol
                    byte[] protocol = new byte[3];
                    for (int i = 0; i < 3; i++)
                        protocol[i] = inBuf[offset + i];
                    if (!protocol.SequenceEqual(Protocol))
                    {
                        //Skip to next byte
                        offset++;
                        availableDataLen--;
                        continue;
                    }

                    //We have a protocol match
                    offset += 3;
                    availableDataLen -= 3;

                    int startOffset = offset;
                    ReadHeader(
                        inBuf,
                        ref offset,
                        out bool compressed,
                        out EBroadcastType type,
                        out ushort seq,
                        out ushort ack,
                        out uint ackBitfield,
                        out int dataLength);
                    availableDataLen -= offset - startOffset;

                    WriteToRemoteSeqs(seq);

                    //When a packet is received,
                    //ack bitfield is scanned and if bit n is set,
                    //then we acknowledge sequence number packet sequence - n,
                    //if it has not been acked already.
                    for (int i = 0; i < 32; i++)
                        if ((ackBitfield & (1 << i)) != 0)
                            AcknowledgeSeq((ushort)(ack - i - 1));

                    if (availableDataLen < dataLength)
                    {
                        //Not enough data to read the full packet, save the header and wait for more data
                        _lastConsumedHeader = (compressed, type, seq, ack, ackBitfield, dataLength);
                        break;
                    }
                    else //We can parse the full packet now
                        ReadPacketData(compressed, type, inBuf, ref availableDataLen, decompBuffer, ref offset, dataLength);
                }
                if (availableDataLen < 0)
                    throw new Exception("UDP buffer offset is negative");
            }

            private bool EnoughDataLeft(int availableDataLen)
            {
                var lastHeader = _lastConsumedHeader;
                if (lastHeader.HasValue)
                {
                    //If we have a header, we need to check if we have enough data to read the header's full packet
                    bool compressed = lastHeader.Value.compress;
                    int dataLen = lastHeader.Value.dataLength;

                    //Uncompressed data length does not include the guid
                    if (!compressed)
                        dataLen += GuidLen;

                    return availableDataLen >= dataLen;
                }
                else //If we don't have a header, we need to check if we have enough data to read at least a header
                    return availableDataLen >= HeaderLen;
            }

            private void AcknowledgeSeq(ushort ackedSeq)
            {
                if (_rttBuffer.TryRemove(ackedSeq, out float time))
                    UpdateRTT(Engine.ElapsedTime - time);
            }

            private float _averageRTT = 0.0f;
            public float AverageRoundTripTimeSec
            {
                get => _averageRTT;
                private set => SetField(ref _averageRTT, value);
            }

            public float AverageRoundTripTimeMs => MathF.Round(AverageRoundTripTimeSec * 1000.0f);

            private float _rttSmoothingPercent = 0.1f;
            public float RTTSmoothingPercent
            {
                get => _rttSmoothingPercent;
                set => SetField(ref _rttSmoothingPercent, value);
            }

            private void UpdateRTT(float rttSec)
            {
                AverageRoundTripTimeSec = Interp.Lerp(AverageRoundTripTimeSec, rttSec, RTTSmoothingPercent);
                //Debug.Out($"RTT: {MathF.Round(AverageRoundTripTimeSec * 1000.0f)}ms");
            }

            private static void ReadHeader(
                byte[] inBuf,
                ref int offset,
                out bool compressed,
                out EBroadcastType type,
                out ushort seq,
                out ushort ack,
                out uint ackBitfield,
                out int dataLength)
            {
                byte flag = inBuf[offset++];
                compressed = (flag & 1) == 1;
                type = (EBroadcastType)((flag >> 1) & 3);
                seq = BitConverter.ToUInt16(inBuf, offset);
                offset += 2;
                ack = BitConverter.ToUInt16(inBuf, offset);
                offset += 2;
                ackBitfield = BitConverter.ToUInt32(inBuf, offset);
                offset += 4;
                dataLength = BitConverter.ToInt32(inBuf, offset);
                offset += 4;
            }

            private static void ReadPacketData(
                bool compressed,
                EBroadcastType type,
                byte[] inBuf,
                ref int availableDataLen,
                byte[] decompBuffer,
                ref int offset,
                int dataLength)
            {
                //Debug.Out($"Reading {(compressed ? "compressed" : "")} packet data of length {dataLength}");
                if (compressed)
                {
                    //Compressed data length includes the guid
                    availableDataLen -= dataLength;

                    int decompLen = Compression.Decompress(inBuf, offset, dataLength, decompBuffer, 0);
                    Propogate(
                        new Guid([.. decompBuffer.Take(GuidLen)]),
                        type,
                        decompBuffer,
                        GuidLen,
                        decompLen - GuidLen);
                }
                else
                {
                    //Uncompressed data length does not include the guid
                    availableDataLen -= dataLength + GuidLen;

                    Propogate(
                        new Guid([.. inBuf.Take(GuidLen)]),
                        type,
                        inBuf,
                        offset + GuidLen,
                        dataLength - GuidLen);
                }
            }

            //protected static void ReadHeader(
            //    byte[] header,
            //    out bool compress,
            //    out EBroadcastType type,
            //    out int dataLength,
            //    out float elapsed,
            //    int offset = 0)
            //{
            //    byte flag = header[offset];
            //    compress = (flag & 1) == 1;
            //    type = (EBroadcastType)((flag >> 1) & 3);
            //    dataLength = BitConverter.ToInt32(header, offset) & 0x00FFFFFF;
            //    elapsed = BitConverter.ToSingle(header, offset + 4);
            //}

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
