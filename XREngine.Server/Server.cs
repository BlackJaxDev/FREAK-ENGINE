namespace XREngine.Networking
{
    public class Server
    {
        public string? IP { get; set; }
        public int Port { get; set; }
        public int CurrentLoad { get; set; } = 0;
        public List<Guid> Instances { get; set; } = new List<Guid>();
    }
}
