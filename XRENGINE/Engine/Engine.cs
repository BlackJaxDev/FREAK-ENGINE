using Silk.NET.Maths;
using Silk.NET.Windowing;
using XREngine.Rendering;
using XREngine.Scene;

namespace XREngine
{
    /// <summary>
    /// The root static class for the engine.
    /// Contains all the necessary functions to run the engine and manage its components.
    /// Organized with several static subclasses for managing different parts of the engine.
    /// You can use these subclasses without typing the whole path every time in your code by adding "using static XREngine.Engine.<path>;" at the top of the file.
    /// </summary>
    public static partial class Engine
    {
        private static readonly EventList<XRWorldInstance> _worldInstances = [];
        private static readonly EventList<XRWindow> _windows = [];
        private static readonly EventList<GenericRenderObject> _renderObjects = [];

        static Engine()
        {
            UserSettings = new UserSettings();
            GameSettings = new GameStartupSettings();
            Networking = new NetworkingManager();
            Assets = new AssetManager();
        }

        /// <summary>
        /// Indicates the engine is currently starting up and might be still initializing objects.
        /// </summary>
        public static bool StartingUp { get; private set; }
        /// <summary>
        /// Indicates the engine is currently shutting down and might be disposing of objects.
        /// </summary>
        public static bool ShuttingDown { get; private set; }
        /// <summary>
        /// User-defined settings, such as graphical and audio options.
        /// </summary>
        public static UserSettings UserSettings { get; set; }
        /// <summary>
        /// Game-defined settings, such as initial world and libraries.
        /// </summary>
        public static GameStartupSettings GameSettings { get; set; }
        /// <summary>
        /// All networking-related functions.
        /// </summary>
        public static NetworkingManager Networking { get; }
        /// <summary>
        /// All active world instances. 
        /// These are separate from the windows to allow for multiple windows to display the same world.
        /// They are also not the same as XRWorld, which is just the data for a world.
        /// </summary>
        public static IEventListReadOnly<XRWorldInstance> WorldInstances => _worldInstances;
        /// <summary>
        /// The list of currently active and rendering windows.
        /// </summary>
        public static IEventListReadOnly<XRWindow> Windows => _windows;
        /// <summary>
        /// The list of all active render objects being utilized for rendering.
        /// Each generic render object has a list of API-specific render objects that represent it for each window.
        /// </summary>
        public static IEventListReadOnly<GenericRenderObject> RenderObjects => _renderObjects;
        /// <summary>
        /// Manages all assets loaded into the engine.
        /// </summary>
        public static AssetManager Assets { get; }
        /// <summary>
        /// Easily accessible random number generator.
        /// </summary>
        public static Random Random { get; } = new Random();

        /// <summary>
        /// Initializes the engine with settings for the game it will run.
        /// </summary>
        public static void Initialize(
            GameStartupSettings startupSettings,
            GameState state)
        {
            StartingUp = true;

            GameSettings = startupSettings;
            UserSettings = GameSettings.DefaultUserSettings;

            Time.Initialize(GameSettings, UserSettings);

            if (state.Worlds is not null)
                _worldInstances.AddRange(state.Worlds);

            CreateWindows(startupSettings.StartupWindows);

            //VRState.Initialize();

            StartingUp = false;
        }

        public static void CreateWindows(List<GameWindowStartupSettings> windows)
        {
            foreach (var windowSettings in windows)
                CreateWindow(windowSettings);
        }

        public static void CreateWindow(GameWindowStartupSettings windowSettings)
        {
            XRWindow window = new(GetWindowOptions(windowSettings));
            CreateViewports(windowSettings.LocalPlayers, window.Renderer);
            window.UpdateViewportSizes();
            SetWorld(windowSettings.TargetWorld, window.Renderer);
            _windows.Add(window);
        }

        private static void SetWorld(XRWorld? targetWorld, AbstractRenderer renderer)
        {
            if (targetWorld is not null)
                renderer.TargetWorldInstance = GetOrInitWorld(targetWorld);
        }

        private static void CreateViewports(ELocalPlayerIndexMask localPlayerMask, AbstractRenderer renderer)
        {
            if (localPlayerMask == 0)
                return;
            
            for (int i = 0; i < 4; i++)
            {
                if (((int)localPlayerMask & (1 << i)) > 0)
                    renderer.RegisterLocalPlayer((ELocalPlayerIndex)i, false);
                renderer.ResizeAllViewportsAccordingToPlayers();
            }
        }

        private static WindowOptions GetWindowOptions(GameWindowStartupSettings windowSettings)
        {
            WindowState windowState;
            WindowBorder windowBorder;
            Vector2D<int> position = new(windowSettings.X, windowSettings.Y);
            Vector2D<int> size = new(windowSettings.Width, windowSettings.Height);
            switch (windowSettings.WindowState)
            {
                case EWindowState.Fullscreen:
                    windowState = WindowState.Fullscreen;
                    windowBorder = WindowBorder.Hidden;
                    break;
                default:
                case EWindowState.Windowed:
                    windowState = WindowState.Normal;
                    windowBorder = WindowBorder.Resizable;
                    break;
                case EWindowState.Borderless:
                    windowState = WindowState.Normal;
                    windowBorder = WindowBorder.Hidden;
                    position = new Vector2D<int>(0, 0);
                    int primaryX = Native.NativeMethods.GetSystemMetrics(0);
                    int primaryY = Native.NativeMethods.GetSystemMetrics(1);
                    size = new Vector2D<int>(primaryX, primaryY);
                    break;
            }

            return new(
                isVisible: true,
                position,
                size,
                0.0,
                0.0,
                UserSettings.RenderLibrary == ERenderLibrary.Vulkan
                    ? new GraphicsAPI(ContextAPI.Vulkan, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(1, 1))
                    : new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 6)),
                windowSettings.WindowTitle ?? string.Empty,
                windowState,
                windowBorder,
                isVSync: windowSettings.VSync,
                shouldSwapAutomatically: true,
                VideoMode.Default);
        }

        public static XRWorldInstance GetOrInitWorld(XRWorld targetWorld)
        {
            if (XRWorldInstance.WorldInstances.TryGetValue(targetWorld, out var instance))
                return instance;
            
            XRWorldInstance.WorldInstances.Add(targetWorld, instance = new XRWorldInstance(targetWorld));
            instance.BeginPlay();
            return instance;
        }

        private static bool RunAsLongAs()
            => Windows.Count > 0;

        public static void Run()
            => Time.Timer.Run(RunAsLongAs);

        /// <summary>
        /// Stops the engine, disposes of all allocated data, and closes all windows.
        /// </summary>
        public static void ShutDown()
        {
            ShuttingDown = true;
            Time.Timer.Stop();
            _windows.Clear();
            Assets.Dispose();
            ShuttingDown = false;
            foreach (var window in _windows)
                window.Window.Close();
        }

        public delegate int DelBeginOperation(
            string operationMessage,
            string finishedMessage,
            out Progress<float> progress,
            out CancellationTokenSource cancel,
            TimeSpan? maxOperationTime = null);

        public delegate void DelEndOperation(int operationId);
    }
}
