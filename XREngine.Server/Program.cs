using XREngine.Networking.LoadBalance.Balancers;

namespace XREngine.Networking
{
    /// <summary>
    /// There will be several of these programs running on different machines.
    /// The user is first directed to a load balancer server, which will then redirect them to a game host server.
    /// </summary>
    public class Program
    {
        private static readonly CommandServer _loadBalancer;

        static Program()
        {
            _loadBalancer = new CommandServer(
                8000,
                new RoundRobinLeastLoadBalancer(new[]
                {
                    new Server { IP = "192.168.0.2", Port = 8001 },
                    new Server { IP = "192.168.0.3", Port = 8002 },
                    new Server { IP = "192.168.0.4", Port = 8003 },
                }),
                new Authenticator(""));
        }

        public static async Task Main()
        {
            await _loadBalancer.Start();
        }
    }
}