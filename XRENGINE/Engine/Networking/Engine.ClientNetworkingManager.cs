using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;

namespace XREngine
{
    public static partial class Engine
    {
        public class ClientNetworkingManager : BaseNetworkingManager
        {
            public override bool IsServer => false;
            public override bool IsClient => true;
            public override bool IsP2P => false;

            /// <summary>
            /// The server IP to send to.
            /// </summary>
            public IPEndPoint? ServerIP { get; set; }
            /// <summary>
            /// Sends from client to server.
            /// </summary>
            public UdpClient? UdpSender { get; set; }

            public void Start(
                IPAddress udpMulticastGroupIP,
                int udpMulticastPort,
                IPAddress serverIP,
                int udpSendPort)
            {
                Debug.Out($"Starting client with udp(multicast: {udpMulticastGroupIP}:{udpMulticastPort}) sending to server at ({serverIP}:{udpSendPort})");
                StartUdpMulticastReceiver(serverIP, udpMulticastGroupIP, udpMulticastPort);
                StartUdpSender(serverIP, udpMulticastPort);
            }

            protected void StartUdpSender(IPAddress serverIP, int udpMulticastServerPort)
            {
                UdpSender = new UdpClient();
                ServerIP = new IPEndPoint(serverIP, udpMulticastServerPort);
                //UdpSender.Connect(ServerIP);
            }

            protected override async Task SendUDP()
            {
                //Send to server
                await ConsumeAndSendUDPQueue(UdpSender, ServerIP);
            }

            //public void RequestWorldChange()
            //    => ReplicateStateChange(new StateChangeInfo(EStateChangeType.WorldChange, JsonConvert.SerializeObject()), true);
        }
    }
}
