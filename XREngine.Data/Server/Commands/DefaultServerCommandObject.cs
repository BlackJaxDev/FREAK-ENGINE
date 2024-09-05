namespace XREngine.Data.Server.Commands
{
    public class DefaultServerCommandObject(string token, EDefaultServerCommand command) : ServerCommandObject(token, "")
    {
        public override string Command { get; } = command.ToString();
    }
}
