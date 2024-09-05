namespace XREngine.Data.Server.Commands
{
    public abstract class ServerCommandObject(string sessionToken, string data)
    {
        public string SessionToken { get; } = sessionToken;
        public string Data { get; } = data;
        public abstract string Command { get; }
    }
}
