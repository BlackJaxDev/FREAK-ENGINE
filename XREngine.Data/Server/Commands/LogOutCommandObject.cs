namespace XREngine.Data.Server.Commands
{
    public class LogOutCommandObject(string token) : AuthCommandObject(token, EAuthServerCommand.Logout) { }
}
