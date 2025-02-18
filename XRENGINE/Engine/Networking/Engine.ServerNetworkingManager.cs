using System.Net;

namespace XREngine
{
    public static partial class Engine
    {
        public class ServerNetworkingManager : BaseNetworkingManager
        {
            public void Start(
                IPAddress udpMulticastGroupIP,
                int udpMulticastPort,
                int udpRecievePort)
            {
                Debug.Out($"Starting server at udp(multicast:{udpMulticastGroupIP}:{udpMulticastPort} / recieve:{udpRecievePort})");
                StartUdpMulticastSender(udpMulticastGroupIP, udpMulticastPort);
                StartUdpReceiver(udpRecievePort);
            }

            protected override void SendUDP()
            {
                //Send to clients
                if (UdpMulticastSender is not null && MulticastEndPoint is not null)
                    ConsumeAndSendUDPQueue(UdpMulticastSender, MulticastEndPoint);
            }
        }
    }
}
