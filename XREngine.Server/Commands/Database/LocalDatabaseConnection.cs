namespace XREngine.Networking.Commands.Database
{
    public class LocalDatabaseConnection(string databaseName) : DatabaseConnection
    {
        public string DatabaseName { get; set; } = databaseName;
        public override string ConnectionString => $"Data Source={DatabaseName}.db;Version=3;";
    }
}
