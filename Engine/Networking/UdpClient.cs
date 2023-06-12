using System.Net;
using System.Net.Sockets;
using System.Text;

namespace XREngine.Networking
{
    public class XUdpClient
    {
        static async Task Main(string[] args)
        {
            int port = 8080;
            string serverAddress = "127.0.0.1";
            using (UdpClient client = new())
            {
                IPEndPoint serverEndpoint = new(IPAddress.Parse(serverAddress), port);

                Console.WriteLine("Connected to the server.");
                Console.WriteLine("Type your messages and press Enter.");
                Console.WriteLine("Type /quit to disconnect.");

                Task receiveTask = ReceiveMessages(client);

                while (true)
                {
                    string message = Console.ReadLine();
                    if (message == "/quit")
                    {
                        break;
                    }

                    byte[] data = Encoding.ASCII.GetBytes(message);
                    await client.SendAsync(data, data.Length, serverEndpoint);

                    client.Close();
                    await receiveTask;
                }
            }

            static async Task ReceiveMessages(UdpClient client)
            {
                while (true)
                {
                    try
                    {
                        UdpReceiveResult result = await client.ReceiveAsync();
                        string responseText = Encoding.ASCII.GetString(result.Buffer);
                        Console.WriteLine($"Received from {result.RemoteEndPoint}: {responseText}");
                    }
                    catch (ObjectDisposedException)
                    {
                        // The UdpClient has been closed, exit the loop
                        break;
                    }
                    catch (SocketException)
                    {
                        // The UdpClient has been closed, exit the loop
                        break;
                    }
                }
            }
        }
    }
}
