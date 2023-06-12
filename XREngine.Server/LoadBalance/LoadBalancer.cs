namespace XREngine.Server.LoadBalance
{
    public abstract class LoadBalancer
    {
        protected readonly List<Server> _servers = new List<Server>();

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

        public IEnumerable<Room> GetAvailableRooms()
        {
            return _servers.SelectMany(s => s.Rooms.Where(r => r.CurrentPlayers < r.MaxPlayers));
        }

        public (Server? Server, Room? Room) GetRoomInfo(Guid roomId)
        {
            foreach (var server in _servers)
            {
                var room = server.Rooms.FirstOrDefault(r => r.Id == roomId);
                if (room != null)
                {
                    return (server, room);
                }
            }
            return (null, null);
        }
    }
}
