namespace XREngine.Networking.Commands.Database
{
    public abstract class DatabaseConnection
    {
        public abstract string ConnectionString { get; }
        public string UsersTableName { get; set; } = "Users";
        public string SessionsTableName { get; set; } = "Sessions";
        public string WorldsTableName { get; set; } = "Worlds";
        public string GroupsTableName { get; set; } = "Groups";
        public string AvatarsTableName { get; set; } = "Avatars";
        public string InstancesTableName { get; set; } = "Instances";
    }
}
