using XREngine.Data.Server;
using XREngine.Data.Server.Commands;
using XREngine.Networking.Commands.Database;

namespace XREngine.Networking.Commands
{
    public abstract partial class CommandProcessor(DatabaseConnection db)
    {
        public DatabaseConnection Database { get; } = db;

        public async Task<(EServerResponseCode code, byte[]? responseData)> Process(ServerCommandObject command)
        {
            string commandName = command.Command;

            if (string.IsNullOrWhiteSpace(commandName) || !Enum.TryParse(commandName, out EDefaultServerCommand cmd))
                return (EServerResponseCode.InvalidCommand, null);

            return cmd switch
            {
                EDefaultServerCommand.Auth => await Auth.Run(command.Data, Database),
                EDefaultServerCommand.Account => await Account(command),
                EDefaultServerCommand.Instances => await Instances(command),
                EDefaultServerCommand.Groups => await Groups(command),
                EDefaultServerCommand.Worlds => await Worlds(command),
                EDefaultServerCommand.Users => await Users(command),
                EDefaultServerCommand.Avatars => await Avatars(command),
                EDefaultServerCommand.Input => await Input(command),
                EDefaultServerCommand.Sync => await Sync(command),
                _ => (EServerResponseCode.InvalidCommand, null),
            };
        }

        private async Task<(EServerResponseCode code, byte[]? responseData)> Sync(ServerCommandObject command)
        {
            throw new NotImplementedException();
        }

        private async Task<(EServerResponseCode code, byte[]? responseData)> Input(ServerCommandObject command)
        {
            throw new NotImplementedException();
        }

        private async Task<(EServerResponseCode code, byte[]? responseData)> Avatars(ServerCommandObject command)
        {
            throw new NotImplementedException();
        }

        private async Task<(EServerResponseCode code, byte[]? responseData)> Users(ServerCommandObject command)
        {
            throw new NotImplementedException();
        }

        private async Task<(EServerResponseCode code, byte[]? responseData)> Worlds(ServerCommandObject command)
        {
            throw new NotImplementedException();
        }

        private async Task<(EServerResponseCode code, byte[]? responseData)> Groups(ServerCommandObject command)
        {
            throw new NotImplementedException();
        }

        private async Task<(EServerResponseCode code, byte[]? responseData)> Instances(ServerCommandObject command)
        {
            throw new NotImplementedException();
        }

        private async Task<(EServerResponseCode code, byte[]? responseData)> Account(ServerCommandObject command)
        {
            throw new NotImplementedException();
        }

        //private void JoinInstance(string instanceId, string userId)
        //{
        //    var roomId = Guid.Parse(message.Split(':')[1]);
        //    var roomInfo = _loadBalancer.RequestInstanceServer(roomId);

        //    if (roomInfo.Server != null && roomInfo.Room != null)
        //    {
        //        var response = $"{roomInfo.Server.IP}:{roomInfo.Server.Port}:{roomInfo.Room.Id}";
        //        var responseData = Encoding.UTF8.GetBytes(response);
        //        await stream.WriteAsync(responseData, 0, responseData.Length);
        //    }
        //    else
        //    {
        //        var response = "Room not found";
        //        var responseData = Encoding.UTF8.GetBytes(response);
        //        await stream.WriteAsync(responseData, 0, responseData.Length);
        //    }

        //    if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(userId))
        //        return;
        //    var instance = _loadBalancer.GetInstance(instanceId);
        //    if (instance is null)
        //        return;
        //    instance.AddUser(userId);
        //}
    }
}
