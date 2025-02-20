using System.Net;
using System.Net.Sockets;

namespace XREngine
{
    public static partial class Engine
    {
        public class ServerNetworkingManager : BaseNetworkingManager
        {
            public override bool IsServer => true;
            public override bool IsClient => false;
            public override bool IsP2P => false;

            public void Start(
                IPAddress udpMulticastGroupIP,
                int udpMulticastPort,
                int udpRecievePort)
            {
                Debug.Out($"Starting server at udp(multicast:{udpMulticastGroupIP}:{udpMulticastPort} / recieve:{udpRecievePort})");
                StartUdpMulticastSender(udpMulticastGroupIP, udpMulticastPort);
                StartUdpReceiver(udpRecievePort);
            }

            /// <summary>
            /// Run on the server - receives from clients
            /// </summary>
            /// <param name="udpPort"></param>
            protected void StartUdpReceiver(int udpPort)
            {
                UdpClient listener = new();
                //listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));
                UdpReceiver = listener;
            }

            protected override async Task SendUDP()
            {
                //Send to clients
                await ConsumeAndSendUDPQueue(UdpMulticastSender, MulticastEndPoint);
            }
        }
    }
}
