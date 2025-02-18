using System.Net;
using System.Net.Sockets;

namespace XREngine
{
    public static partial class Engine
    {
        public class ClientNetworkingManager : BaseNetworkingManager
        {
            public IPEndPoint? ServerIP { get; set; }
            /// <summary>
            /// Sends from client to server.
            /// Not used in a p2p scenario.
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
                UdpSender.Connect(ServerIP);
            }

            protected override void SendUDP()
            {
                //Send to server
                if (UdpSender is not null && ServerIP is not null)
                    ConsumeAndSendUDPQueue(UdpSender, ServerIP);
            }
        }
    }
}
