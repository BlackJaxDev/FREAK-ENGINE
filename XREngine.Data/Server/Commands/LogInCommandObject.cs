namespace XREngine.Data.Server.Commands
{
    public class LogInCommandObject : AuthCommandObject
    {
        public string Username { get; }
        public string Password { get; }
        public LogInCommandObject(string token, string username, string password) : base(token, EAuthServerCommand.Login)
        {
            Username = username;
            Password = password;
        }
    }
}
