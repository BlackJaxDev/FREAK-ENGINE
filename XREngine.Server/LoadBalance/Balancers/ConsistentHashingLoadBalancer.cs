﻿using System.Security.Cryptography;
using System.Text;

namespace XREngine.Networking.LoadBalance.Balancers
{
    public class ConsistentHashingLoadBalancer(IEnumerable<Server> servers, int replicas = 100) : LoadBalancer(servers)
    {
        private readonly SortedDictionary<uint, Server> _circle = [];

        private static string GetHashString(Server server, int i)
            => $"{server.IP}:{server.Port}:{i}";

        public override void AddServer(Server server)
        {
            for (int i = 0; i < replicas; i++)
                _circle[Hash(GetHashString(server, i))] = server;
        }
        
        public override void RemoveServer(Server server)
        {
            for (int i = 0; i < replicas; i++)
                _circle.Remove(Hash(GetHashString(server, i)));
        }

        public Server? GetServer(string key)
        {
            if (_circle.Count == 0)
                return null;
            
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
            var data = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return (uint)(data[0] | data[1] << 8 | data[2] << 16 | data[3] << 24);
        }
    }
}
