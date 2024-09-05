using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace XREngine.Networking
{
    public class XUdpServer
    {
        private static ConcurrentDictionary<IPEndPoint, DateTime> clients = new ConcurrentDictionary<IPEndPoint, DateTime>();

        static async Task Main(string[] args)
        {
            int port = 8080;
            using (UdpClient server = new UdpClient(port))
            {
                Console.WriteLine("Server started...");

                while (true)
                {
                    UdpReceiveResult result = await server.ReceiveAsync();
                    string message = Encoding.ASCII.GetString(result.Buffer);
                    Console.WriteLine($"Received from {result.RemoteEndPoint}: {message}");

                    clients.AddOrUpdate(result.RemoteEndPoint, DateTime.UtcNow, (k, v) => DateTime.UtcNow);

                    if (message.StartsWith("/"))
                    {
                        HandleCommand(message, server);
                    }
                    else
                    {
                        BroadcastMessage($"Client {result.RemoteEndPoint}: {message}", server);
                    }
                }
            }
        }

        static void HandleCommand(string command, UdpClient server)
        {
            switch (command)
            {
                case "/list":
                    StringBuilder sb = new StringBuilder("Connected clients:\n");
                    foreach (var client in clients.Keys)
                    {
                        sb.AppendLine(client.ToString());
                    }
                    Console.WriteLine(sb.ToString());
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }

        static void BroadcastMessage(string message, UdpClient server)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            foreach (var client in clients.Keys)
            {
                server.Send(data, data.Length, client);
            }
        }
    }
}
