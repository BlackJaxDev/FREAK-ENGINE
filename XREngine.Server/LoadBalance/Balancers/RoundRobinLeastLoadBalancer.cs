namespace XREngine.Server.LoadBalance.Balancers
{
    public class RoundRobinLeastLoadBalancer : LoadBalancer
    {
        private int _currentIndex;
        private int _roundRobinThreshold;

        public RoundRobinLeastLoadBalancer(IEnumerable<Server> servers, int roundRobinThreshold = 10) : base(servers)
        {
            _currentIndex = 0;
            _roundRobinThreshold = roundRobinThreshold;
        }

        public override Server? GetNextServer()
        {
            lock (_servers)
            {
                if (_servers.Count == 0)
                    return null;

                // Use Round Robin for servers with load less than the threshold
                var roundRobinServers = _servers.Where(s => s.CurrentLoad < _roundRobinThreshold).ToList();
                if (roundRobinServers.Any())
                {
                    var server = roundRobinServers[_currentIndex];
                    _currentIndex = (_currentIndex + 1) % roundRobinServers.Count;
                    return server;
                }

                // Use Least Connections for servers with load equal or greater than the threshold
                return _servers.OrderBy(s => s.CurrentLoad).FirstOrDefault();
            }
        }
    }
}
