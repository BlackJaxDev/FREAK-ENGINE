namespace XREngine.Networking.LoadBalance
{
    public abstract class LoadBalancer
    {
        protected readonly List<Server> _servers = [];

        public LoadBalancer(IEnumerable<Server> servers)
        {
            foreach (var server in servers)
                AddServer(server);
        }

        public virtual void AddServer(Server server)
        {
            lock (_servers)
                _servers.Add(server);
        }

        public virtual void RemoveServer(Server server)
        {
            lock (_servers)
                _servers.Remove(server);
        }

        public abstract Server? GetNextServer();

        public IEnumerable<Guid> GetAvailableInstances()
        {
            return _servers.SelectMany(s => s.Instances.Where(r => r.CurrentPlayers < r.MaxPlayers));
        }

        public (Server? server, Guid? instance) RequestInstanceServer(Guid roomId)
        {
            foreach (var server in _servers)
            {
                if (!server.Instances.Contains(roomId))
                    continue;

                return (server, roomId);
            }
            return (null, null);
        }
    }
}
