namespace XREngine.Data.Server.Commands
{
    public class AuthCommandObject(string token, EAuthServerCommand subCommand) : DefaultServerCommandObject(token, EDefaultServerCommand.Auth)
    {
        public EAuthServerCommand SubCommand { get; } = subCommand;
    }
}
