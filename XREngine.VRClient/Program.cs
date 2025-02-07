using OpenVR.NET.Manifest;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using XREngine.Native;
using XREngine.Scene;

namespace XREngine.VRClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var settings = Engine.LoadOrGenerateGameSettings(GenerateGameSettings);
            //var processes = Process.GetProcesses();

            //Check if this is already running
            var exists = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()?.Location)).Length > 1;
            if (exists)
            {
                Debug.LogWarning("VR client is already running.");
                Console.In.ReadLine();
                return;
            }

            //Find or start the main game process
            if (!VerifyMainGameRunning(null, settings, out string? gamePath))
            {
                Debug.LogWarning("Could not find main game process.");
                Console.In.ReadLine();
                return;
            }

            //Initialize VR
            //Engine.VRState.InitializeLocal(settings.ActionManifest, settings.VRManifest, GetEyeTextureHandle);

            //Run the game
            //We don't need to load a game state because this app only sends inputs to the game and receives renders
            Engine.Run(settings, new GameState());
        }
        private static nint GetEyeTextureHandle()
        {
            return 0;
        }

        private static bool VerifyMainGameRunning(Process[]? processes, IVRGameStartupSettings settings, out string? resultPath)
        {
            resultPath = null;
            string name = settings.GameName;
            string fileName = $"{name}.exe";

            //Check if the game is already running
            //TODO: also check via IPC
            //if (processes is not null)
            //    foreach (var p in processes)
            //    {
            //        if (p.ProcessName == name)
            //            return true;
                
            //        string? path = GetMainModuleFilepath(p.Id);
            //        if (path?.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase) ?? false)
            //        {
            //            resultPath = path;
            //            return true;
            //        }
            //    }
            var gameProcess = Process.GetProcessesByName(name).FirstOrDefault();
            if (gameProcess != null)
            {
                //resultPath = GetMainModuleFilepath(gameProcess.Id);
                return true;
            }

            //Check if the game is installed
            var searchPaths = settings.GameSearchPaths;
            foreach (var (folder, relativePath) in searchPaths)
            {
                string rootDir = Environment.GetFolderPath(folder);
                string fullPath = Path.Combine(rootDir, relativePath, fileName);
                if (File.Exists(fullPath))
                {
                    resultPath = fullPath;
                    Process.Start(fullPath);
                    return true;
                }
            }
            resultPath = null;
            return false;
        }

        private static string? GetMainModuleFilepath(int processId)
        {
            string wmiQueryString = $"SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE ProcessId = {processId}";
            using var searcher = new ManagementObjectSearcher(wmiQueryString);
            using var results = searcher.Get();
            ManagementObject mo = results.Cast<ManagementObject>().FirstOrDefault();
            return mo != null ? (string)mo["ExecutablePath"] : null;
        }

        private static VRGameStartupSettings<TActionCategory, TGameAction> GenerateGameSettings<TActionCategory, TGameAction>()
            where TActionCategory : struct, Enum
            where TGameAction : struct, Enum
        {
            VRGameStartupSettings<TActionCategory, TGameAction>? settings;
            int w = 1920;
            int h = 1080;
            float update = 90.0f;
            float render = 90.0f;

            int primaryX = NativeMethods.GetSystemMetrics(0);
            int primaryY = NativeMethods.GetSystemMetrics(1);

            settings = new VRGameStartupSettings<TActionCategory, TGameAction>()
            {
                Name = "startup",
                StartupWindows =
                [
                    new()
                    {
                        WindowTitle = "XREngine VRClient",
                        TargetWorld = CreateFillerWorld(),
                        WindowState = EWindowState.Windowed,
                        X = primaryX / 2 - w / 2,
                        Y = primaryY / 2 - h / 2,
                        Width = w,
                        Height = h,
                    }
                ],
                OutputVerbosity = EOutputVerbosity.Verbose,
                UseIntegerWeightingIds = true,
                DefaultUserSettings = new UserSettings()
                {
                    TargetFramesPerSecond = render,
                    VSync = EVSyncMode.Off,
                },
                TargetUpdatesPerSecond = update,
                ActionManifest = new ActionManifest<TActionCategory, TGameAction>()
                {
                    Actions = GetActions<TActionCategory, TGameAction>(),
                },
                VRManifest = new VrManifest()
                {
                    AppKey = "XRE.VRClient.Test",
                    IsDashboardOverlay = false,
                    WindowsPath = Environment.ProcessPath,
                    WindowsArguments = "",
                },
            };
            return settings;
        }

        private static List<OpenVR.NET.Manifest.Action<TActionCategory, TGameAction>> GetActions<TActionCategory, TGameAction>()
            where TActionCategory : struct, Enum
            where TGameAction : struct, Enum
        {
            return [];
        }

        private static XRWorld CreateFillerWorld()
        {
            var world = new XRWorld();
            var scene = new XRScene() { Name = "FillerScene" };
            world.Scenes.Add(scene);
            return world;
        }
    }
}
