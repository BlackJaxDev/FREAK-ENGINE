using System.Diagnostics;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Native;
using XREngine.Rendering.Models.Materials;
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
            Engine.Run(Engine.LoadOrGenerateGameSettings(() => GetEngineSettings(CreateServerDebugWorld())), Engine.LoadOrGenerateGameState());
        }

        static XRWorld CreateServerDebugWorld()
        {
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var scene = new XRScene("ServerScene");
            var rootNode = new SceneNode(scene) { Name = "ServerRootNode" };

            var uiNode = new SceneNode(rootNode, "ServerCameraNode");

            var cameraComp = uiNode.AddComponent<CameraComponent>()!;
            cameraComp.SetOrthographic(1920, 1080, -0.5f, 0.5f);

            var uiCanvas = uiNode.AddComponent<UICanvasComponent>()!;
            var canvasTfm = uiCanvas.CanvasTransform;
            canvasTfm.DrawSpace = ECanvasDrawSpace.Screen;
            canvasTfm.Width = 1920.0f;
            canvasTfm.Height = 1080.0f;
            canvasTfm.CameraDrawSpaceDistance = 10.0f;
            canvasTfm.Padding = new Vector4(0.0f);

            var outputLogNode = uiNode.NewChild(out UIMaterialComponent outputLogBackground);
            outputLogBackground.Material = BackgroundMaterial;

            var logTextNode = outputLogNode.NewChild(out VirtualizedConsoleUIComponent outputLogComp);
            outputLogComp.AddItem("Test Item");
            var logTfm = outputLogComp.BoundableTransform;
            logTfm.MinAnchor = new Vector2(0.0f, 0.0f);
            logTfm.MaxAnchor = new Vector2(1.0f, 1.0f);

            Trace.Listeners.Add(new OutputLogListener(outputLogComp!));

            return new XRWorld("ServerWorld", scene);
        }

        private static XRMaterial? _backgroundMaterial;
        public static XRMaterial BackgroundMaterial
        {
            get => _backgroundMaterial ??= MakeBackgroundMaterial();
            set => _backgroundMaterial = value;
        }

        private static XRMaterial MakeBackgroundMaterial()
        {
            var floorShader = ShaderHelper.LoadEngineShader("UI\\GrabpassGaussian.frag");
            ShaderVar[] floorUniforms =
            [
                new ShaderVector4(new ColorF4(0.0f, 1.0f), "MatColor"),
                new ShaderFloat(10.0f, "BlurStrength"),
                new ShaderInt(30, "SampleCount"),
            ];
            XRTexture2D grabTex = XRTexture2D.CreateGrabPassTextureResized(1.0f, EReadBufferMode.Back, true, false, false, false);
            var floorMat = new XRMaterial(floorUniforms, [grabTex], floorShader);
            floorMat.RenderOptions.CullMode = ECullMode.None;
            floorMat.RenderOptions.RequiredEngineUniforms = EUniformRequirements.Camera;
            floorMat.RenderPass = (int)EDefaultRenderPass.TransparentForward;
            return floorMat;
        }

        static GameStartupSettings GetEngineSettings(XRWorld? targetWorld = null)
        {
            int w = 1920;
            int h = 1080;
            float updateHz = 90.0f;
            float renderHz = 30.0f;
            float fixedHz = 90.0f;

            int primaryX = NativeMethods.GetSystemMetrics(0);
            int primaryY = NativeMethods.GetSystemMetrics(1);

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