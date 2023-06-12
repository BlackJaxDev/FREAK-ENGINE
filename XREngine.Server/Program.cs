using XREngine.Server.LoadBalance;

namespace XREngine.Server
{
    public class Program
    {
        private static LoadBalancingServer? _loadBalancingServer;

        public static async Task Main()
        {
            _loadBalancingServer = new LoadBalancingServer(8000, new RoundRobinLeastLoadBalancer(new[]
            {
                new Server { IP = "192.168.0.2", Port = 8001 },
                new Server { IP = "192.168.0.3", Port = 8002 },
                new Server { IP = "192.168.0.4", Port = 8003 },
            }));

            await _loadBalancingServer.Start();
        }
    }
}