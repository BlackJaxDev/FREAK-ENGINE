using XREngine.Native;
using XREngine.Networking.Commands;
using XREngine.Networking.LoadBalance.Balancers;
using XREngine.Scene;

namespace XREngine.Networking
{
    /// <summary>
    /// There will be several of these programs running on different machines.
    /// The user is first directed to a load balancer server, which will then redirect them to a game host server.
    /// </summary>
    public class Program
    {
        //private static readonly CommandServer _loadBalancer;

        //static Program()
        //{
        //    _loadBalancer = new CommandServer(
        //        8000,
        //        new RoundRobinLeastLoadBalancer(new[]
        //        {
        //            new Server { IP = "192.168.0.2", Port = 8001 },
        //            new Server { IP = "192.168.0.3", Port = 8002 },
        //            new Server { IP = "192.168.0.4", Port = 8003 },
        //        }),
        //        new Authenticator(""));
        //}

        //public static async Task Main()
        //{
        //    await _loadBalancer.Start();
        //}

        private static void Main(string[] args)
        {
            Engine.Initialize(GetEngineSettings(), GetGameState());
            Engine.Run();
            Engine.ShutDown();
        }


        static GameState GetGameState()
        {
            return new GameState()
            {

            };
        }

        static GameStartupSettings GetEngineSettings(XRWorld? targetWorld = null)
        {
            int w = 1920;
            int h = 1080;
            float update = 60.0f;
            float render = 10.0f;

            int primaryX = NativeMethods.GetSystemMetrics(0);
            int primaryY = NativeMethods.GetSystemMetrics(1);

            //TODO: read from init file if it exists
            return new GameStartupSettings()
            {
                StartupWindows =
                [
                    new()
                    {
                        WindowTitle = "XRE Server",
                        TargetWorld = null,
                        WindowState = EWindowState.Windowed,
                        X = primaryX / 2 - w / 2,
                        Y = primaryY / 2 - h / 2,
                        Width = w,
                        Height = h,
                    }
                ],
                OutputVerbosity = EOutputVerbosity.Verbose,
                IsServer = true,
                DefaultUserSettings = new UserSettings()
                {
                    TargetFramesPerSecond = render,
                    TargetUpdatesPerSecond = update,
                    VSync = EVSyncMode.Off,
                }
            };
        }
    }
}