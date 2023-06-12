using System.Security.Cryptography;
using System.Text;

namespace XREngine.Server.LoadBalance.Balancers
{
    public class ConsistentHashingLoadBalancer : LoadBalancer
    {
        private readonly SortedDictionary<uint, Server> _circle;
        private readonly int _replicas;

        public ConsistentHashingLoadBalancer(IEnumerable<Server> servers, int replicas = 100) : base(servers)
        {
            _circle = new SortedDictionary<uint, Server>();
            _replicas = replicas;
        }

        public override void AddServer(Server server)
        {
            for (int i = 0; i < _replicas; i++)
            {
                var hash = Hash($"{server.IP}:{server.Port}:{i}");
                _circle[hash] = server;
            }
        }

        public override void RemoveServer(Server server)
        {
            for (int i = 0; i < _replicas; i++)
            {
                var hash = Hash($"{server.IP}:{server.Port}:{i}");
                _circle.Remove(hash);
            }
        }

        public Server GetServer(string key)
        {
            if (_circle.Count == 0)
            {
                return null;
            }

            var hash = Hash(key);
            if (!_circle.TryGetValue(hash, out var server))
            {
                var greaterThanHash = _circle.Keys.Where(k => k > hash);
                hash = greaterThanHash.Any() ? greaterThanHash.Min() : _circle.Keys.Min();
                server = _circle[hash];
            }

            return server;
        }

        public override Server? GetNextServer()
        {
            throw new NotImplementedException();
        }

        private static uint Hash(string input)
        {
            using var md5 = MD5.Create();
            var data = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return (uint)(data[0] | data[1] << 8 | data[2] << 16 | data[3] << 24);
        }
    }
}
