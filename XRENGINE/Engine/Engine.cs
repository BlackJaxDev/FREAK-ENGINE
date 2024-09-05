using Silk.NET.Maths;
using Silk.NET.Windowing;
using XREngine.Rendering;
using XREngine.Scene;

namespace XREngine
{
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
            GameSettings = startupSettings;
            UserSettings = GameSettings.DefaultUserSettings;
            Time.Initialize(GameSettings, UserSettings);
            if (state.Worlds is not null)
                _worldInstances.AddRange(state.Worlds);
            CreateWindows(startupSettings.StartupWindows);
        }

        public static void CreateWindows(List<GameWindowStartupSettings> windows)
        {
            foreach (var windowSettings in windows)
                CreateWindow(windowSettings);
        }

        public static void CreateWindow(GameWindowStartupSettings windowSettings)
        {
            WindowOptions options = new(
                isVisible: true,
                new Vector2D<int>(50, 50),
                new Vector2D<int>(1280, 720),
                0.0,
                0.0,
                 UserSettings.RenderLibrary == ERenderLibrary.Vulkan 
                 ? new GraphicsAPI(ContextAPI.Vulkan, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(1, 1))
                 : new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(4, 6)),
                windowSettings.WindowTitle ?? "",
                WindowState.Normal,
                WindowBorder.Resizable,
                isVSync: true,
                shouldSwapAutomatically: true,
                VideoMode.Default);

            SetWindowOptions(windowSettings, ref options);

            XRWindow window = new(options);

            var targetWorld = windowSettings.TargetWorld;
            if (targetWorld is not null)
                window.Renderer.TargetWorldInstance = GetOrInitWorld(targetWorld);
            
            _windows.Add(window);
        }

        public static XRWorldInstance GetOrInitWorld(XRWorld targetWorld)
        {
            if (XRWorldInstance.WorldInstances.TryGetValue(targetWorld, out var instance))
                return instance;
            
            XRWorldInstance.WorldInstances.Add(targetWorld, instance = new XRWorldInstance(targetWorld));
            instance.BeginPlay();
            return instance;
        }

        private static void SetWindowOptions(GameWindowStartupSettings windowSettings, ref WindowOptions options)
        {
            options.Title = windowSettings.WindowTitle ?? string.Empty;
            options.Size = new Vector2D<int>(windowSettings.Width, windowSettings.Height);
            options.FramesPerSecond = UserSettings.TargetFramesPerSecond ?? 0.0f;
            options.UpdatesPerSecond = 0.0f; //Updates are handled by the engine completely separately from windows

            switch (windowSettings.WindowState)
            {
                case EWindowState.Fullscreen:
                    options.WindowState = WindowState.Fullscreen;
                    options.WindowBorder = WindowBorder.Hidden;
                    break;
                case EWindowState.Windowed:
                    options.WindowState = WindowState.Normal;
                    options.WindowBorder = WindowBorder.Resizable;
                    break;
                case EWindowState.Borderless:
                    options.WindowState = WindowState.Normal;
                    options.WindowBorder = WindowBorder.Hidden;
                    options.Position = new Vector2D<int>(0, 0);
                    int primaryX = Native.NativeMethods.GetSystemMetrics(0);
                    int primaryY = Native.NativeMethods.GetSystemMetrics(1);
                    options.Size = new Vector2D<int>(primaryX, primaryY);
                    break;
            }
        }

        public static void Run()
        {
            StartingUp = true;
            Time.Timer.Run();
            StartingUp = false;

            //VRState.Initialize();

            while (Windows.Count > 0)
                Time.Timer.RenderThread();
        }

        /// <summary>
        /// Stops the engine and disposes of all allocated data.
        /// </summary>
        public static void ShutDown()
        {
            ShuttingDown = true;
            Time.Timer.Stop();
            foreach (var window in _windows)
                window.Window.Close();
            _windows.Clear();
            Assets.Dispose();
            ShuttingDown = false;
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
