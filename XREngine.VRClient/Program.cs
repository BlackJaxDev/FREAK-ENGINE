using OpenVR.NET.Manifest;
using XREngine.Native;
using XREngine.Scene;

namespace XREngine.VRClient
{
    internal class Program
    {
        public static VrManifest VrManifest { get; } = new();
        public static ActionManifest<ActionCategory, GameAction> ActionManifest { get; } = new();

        static void Main(string[] args)
        {
            GetActions();
            Engine.VRState.Initialize(ActionManifest, new VrManifest()
            {
                AppKey = "XRE.VRClient.Test",
                IsDashboardOverlay = false,
                WindowsPath = Environment.ProcessPath,
                WindowsArguments = "",
            });
            Engine.Initialize(GetEngineSettings(), GetGameState());
            Engine.Run();
            Engine.ShutDown();
        }

        private static void GetActions()
        {
            ActionManifest.Actions =
            [
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.Interact,
                    Category = ActionCategory.Gameplay,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.Jump,
                    Category = ActionCategory.Gameplay,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.ToggleMute,
                    Category = ActionCategory.Gameplay,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.Grab,
                    Category = ActionCategory.Gameplay,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.PlayspaceDragLeft,
                    Category = ActionCategory.Gameplay,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Optional,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.PlayspaceDragRight,
                    Category = ActionCategory.Gameplay,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Optional,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.ToggleMenu,
                    Category = ActionCategory.Menus,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.ToggleMiniMenu,
                    Category = ActionCategory.Menus,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.LeftHandPose,
                    Category = ActionCategory.Controllers,
                    Type = ActionType.Pose,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.RightHandPose,
                    Category = ActionCategory.Controllers,
                    Type = ActionType.Pose,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.LeftHandGrip,
                    Category = ActionCategory.Controllers,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.RightHandGrip,
                    Category = ActionCategory.Controllers,
                    Type = ActionType.Boolean,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.LeftHandTrigger,
                    Category = ActionCategory.Controllers,
                    Type = ActionType.Scalar,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.RightHandTrigger,
                    Category = ActionCategory.Controllers,
                    Type = ActionType.Scalar,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.LeftHandTrackpad,
                    Category = ActionCategory.Controllers,
                    Type = ActionType.Vector2,
                    Requirement = Requirement.Mandatory,
                },
                new OpenVR.NET.Manifest.Action<ActionCategory, GameAction>()
                {
                    Name = GameAction.RightHandTrackpad,
                    Category = ActionCategory.Controllers,
                    Type = ActionType.Vector2,
                    Requirement = Requirement.Mandatory,
                },
            ];
        }

        public enum GameAction
        {
            Interact,
            Jump,
            ToggleMute,
            Grab,
            PlayspaceDragLeft,
            PlayspaceDragRight,
            ToggleMenu,
            ToggleMiniMenu,
            LeftHandPose,
            RightHandPose,
            LeftHandGrip,
            RightHandGrip,
            LeftHandTrigger,
            RightHandTrigger,
            LeftHandTrackpad,
            RightHandTrackpad,
            LeftHandTrackpadTouch,
            RightHandTrackpadTouch,
            LeftHandThumbstick,
            RightHandThumbstick,
            LeftHandA,
            RightHandA,
            LeftHandB,
            RightHandB,
            LeftHandX,
            RightHandX,
            LeftHandY,
            RightHandY,
        }
        public enum ActionCategory
        {
            Default,
            Controllers,
            Trackers,
            Menus,
            Gameplay,
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
            float render = 90.0f;

            int primaryX = NativeMethods.GetSystemMetrics(0);
            int primaryY = NativeMethods.GetSystemMetrics(1);

            //TODO: read from init file if it exists
            return new GameStartupSettings()
            {
                StartupWindows =
                [
                    new()
                    {
                        WindowTitle = "XREngine VRClient",
                        TargetWorld = targetWorld ?? new XRWorld(),
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
                    TargetUpdatesPerSecond = update,
                    VSync = EVSyncMode.Off,
                }
            };
        }
    }
}
