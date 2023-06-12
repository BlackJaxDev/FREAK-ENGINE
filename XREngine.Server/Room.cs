namespace XREngine.Server
{
    public class Room
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
    }
}
