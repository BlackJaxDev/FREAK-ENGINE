using System.Diagnostics;
using XREngine.Components;
using XREngine.Native;
using XREngine.Rendering;
using XREngine.Rendering.UI;
using XREngine.Scene;
using static XREngine.GameStartupSettings;

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
            Engine.Run(GetEngineSettings(CreateServerDebugWorld()), GetGameState());
        }

        static XRWorld CreateServerDebugWorld()
        {
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var world = new XRWorld() { Name = "ServerWorld" };
            var scene = new XRScene() { Name = "ServerScene" };
            var rootNode = new SceneNode(scene) { Name = "ServerRootNode" };

            var cameraNode = new SceneNode(scene) { Name = "ServerCameraNode" };
            var cameraComp = cameraNode.AddComponent<CameraComponent>();
            cameraComp!.Camera.Parameters = new XROrthographicCameraParameters(1920, 1080, -0.5f, 0.5f);
            var uiCanvas = cameraNode.AddComponent<UICanvasComponent>()!;
            uiCanvas.CanvasTransform.CameraDrawSpaceDistance = 0.0f;
            uiCanvas.CanvasTransform.DrawSpace = ECanvasDrawSpace.Screen;

            var outputLogNode = new SceneNode(scene) { Name = "ServerOutputLogNode" };
            var outputLogComp = outputLogNode.AddComponent<VirtualizedListUIComponent>();
            var tfm = outputLogNode.SetTransform<UIBoundableTransform>();
            tfm.Width = 400;
            //tfm.Height = 100 %;

            Trace.Listeners.Add(new OutputLogListener(outputLogComp!));

            return world;
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
            float updateHz = 120.0f;
            float renderHz = 30.0f;
            float fixedHz = 30.0f;

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
                        TargetWorld = targetWorld ?? new XRWorld(),
                        WindowState = EWindowState.Windowed,
                        X = primaryX / 2 - w / 2,
                        Y = primaryY / 2 - h / 2,
                        Width = w,
                        Height = h,
                    }
                ],
                OutputVerbosity = EOutputVerbosity.Verbose,
                AppType = EAppType.Server,
                DefaultUserSettings = new UserSettings()
                {
                    TargetFramesPerSecond = renderHz,
                    VSync = EVSyncMode.Off,
                },
                TargetUpdatesPerSecond = updateHz,
                FixedFramesPerSecond = fixedHz,
            };
        }
    }
}