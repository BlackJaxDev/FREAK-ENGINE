using System.Net;

namespace XREngine
{
    public static partial class Engine
    {
        public class PeerToPeerNetworkingManager : BaseNetworkingManager
        {
            public void Start(
                IPAddress udpMulticastGroupIP,
                int udpMulticastPort,
                IPAddress serverIP)
            {
                Debug.Out($"Starting client with udp(multicast: {udpMulticastGroupIP}:{udpMulticastPort}) sending to server at ({serverIP}:{udpMulticastPort})");
                StartUdpMulticastReceiver(serverIP, udpMulticastGroupIP, udpMulticastPort);
                StartUdpMulticastSender(udpMulticastGroupIP, udpMulticastPort);
            }

            protected override void SendUDP()
            {
                //Send to all peers
                if (UdpMulticastSender is not null && MulticastEndPoint is not null)
                    ConsumeAndSendUDPQueue(UdpMulticastSender, MulticastEndPoint);
            }
        }
    }
}
