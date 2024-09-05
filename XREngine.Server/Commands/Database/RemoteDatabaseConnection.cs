namespace XREngine.Networking.Commands.Database
{
    public class RemoteDatabaseConnection(string remoteServer, string databaseName, string serverUsername, string serverPassword) : DatabaseConnection
    {
        public string RemoteServer { get; set; } = remoteServer;
        public string DatabaseName { get; set; } = databaseName;
        public string ServerUsername { get; set; } = serverUsername;
        public string ServerPassword { get; set; } = serverPassword;
        public override string ConnectionString => $"Server={RemoteServer};Database={DatabaseName};User ID={ServerUsername};Password={ServerPassword};";
    }
}
