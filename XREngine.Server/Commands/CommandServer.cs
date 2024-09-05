using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using XREngine.Data.Server;
using XREngine.Data.Server.Commands;

namespace XREngine.Networking.Commands
{
    public class CommandServer(int port, CommandProcessor commandProcessor, Authenticator authenticator)
    {
        private readonly TcpListener _tcpListener = new(IPAddress.Any, port);
        private readonly Authenticator _authenticator = authenticator;

        public CommandProcessor CommandProcessor { get; } = commandProcessor;

        public bool IsRunning { get; set; } = false;
        public void Stop() => IsRunning = false;

        public async Task Start()
        {
            _tcpListener.Start();
            while (IsRunning)
            {
                using var client = await _tcpListener.AcceptTcpClientAsync();
                await ProcessClientRequest(client);
            }
            _tcpListener.Stop();
        }

        private async Task ProcessClientRequest(TcpClient client)
        {
            var stream = client.GetStream();
            try
            {
                var command = await ReadCommand(stream);
                if (command == null)
                {
                    await Reply(stream, new(EServerResponseCode.InvalidTokenJson));
                    return;
                }
                var user = _authenticator.ValidateToken(command.SessionToken);
                if (user is null)
                {
                    await Reply(stream, new(EServerResponseCode.InvalidToken));
                    return;
                }
                if (CommandProcessor is null)
                {
                    await Reply(stream, new(EServerResponseCode.InternalServerError));
                    return;
                }
                await CommandProcessor.Process(command);
            }
            catch
            {
                await Reply(stream, new(EServerResponseCode.InternalServerError));
            }
        }

        private static async Task<ServerCommandObject?> ReadCommand(NetworkStream stream)
        {
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer);
            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            return JsonConvert.DeserializeObject<ServerCommandObject>(message);
        }

        private static async Task Reply(NetworkStream stream, ServerResponseObject response)
        {
            string json = JsonConvert.SerializeObject(response);
            var bytes = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(bytes);
        }
    }
}
