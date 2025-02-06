using OpenVR.NET.Manifest;
using XREngine;
using XREngine.Editor;
using XREngine.Native;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene;
using XREngine.VRClient;
using ActionType = OpenVR.NET.Manifest.ActionType;

internal class Program
{
    /// <summary>
    /// This project serves as a hardcoded game client for development purposes.
    /// This editor will autogenerate the client exe csproj to compile production games.
    /// </summary>
    /// <param name="args"></param>
    private static void Main(string[] args)
    {
        RenderInfo2D.ConstructorOverride = RenderInfo2DConstructor;
        RenderInfo3D.ConstructorOverride = RenderInfo3DConstructor;

        var startup =/* Engine.LoadOrGenerateGameSettings(() => */GetEngineSettings(new XRWorld());//);
        var world = EditorWorld.CreateUnitTestWorld();
        startup.StartupWindows[0].TargetWorld = world;

        CodeManager.Instance.CompileOnChange = true;
        Engine.Run(startup, Engine.LoadOrGenerateGameState());
    }

    static EditorRenderInfo2D RenderInfo2DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);
    static EditorRenderInfo3D RenderInfo3DConstructor(IRenderable owner, RenderCommand[] commands)
        => new(owner, commands);

    static GameState GetGameState()
    {
        return new GameState()
        {

        };
    }

    private static VRGameStartupSettings<EVRActionCategory, EVRGameAction> GetEngineSettings(XRWorld targetWorld)
    {
        int w = 1920;
        int h = 1080;
        float updateHz = 120.0f;
        float renderHz = 0.0f;
        float fixedHz = 30.0f;

        int primaryX = NativeMethods.GetSystemMetrics(0);
        int primaryY = NativeMethods.GetSystemMetrics(1);

        var settings = new VRGameStartupSettings<EVRActionCategory, EVRGameAction>()
        {
            StartupWindows =
            [
                new()
                {
                    WindowTitle = "XRE Editor",
                    TargetWorld = targetWorld,
                    WindowState = EWindowState.Windowed,
                    X = primaryX / 2 - w / 2,
                    Y = primaryY / 2 - h / 2,
                    Width = w,
                    Height = h,
                }
            ],
            OutputVerbosity = EOutputVerbosity.Verbose,
            DefaultUserSettings = new UserSettings()
            {
                TargetFramesPerSecond = renderHz,
                VSync = EVSyncMode.Off,
            },
            TargetUpdatesPerSecond = updateHz,
            FixedFramesPerSecond = fixedHz,
        };
        if (EditorWorld.VRPawn)
        {
            //https://github.com/ValveSoftware/openvr/wiki/Action-manifest
            settings.ActionManifest = new ActionManifest<EVRActionCategory, EVRGameAction>()
            {
                Actions = GetActions(),
                ActionSets = GetActionSets(),
                //DefaultBindings = [new DefaultBinding() { ControllerType = "knuckles", Path = "" }],
            };
            settings.VRManifest = new VrManifest()
            {
                AppKey = "XRE.VR.Test",
                IsDashboardOverlay = false,
                WindowsPath = Environment.ProcessPath,
                WindowsArguments = "",
            };
        }
        return settings;
    }

    #region VR Actions
    private static List<ActionSet<EVRActionCategory, EVRGameAction>> GetActionSets()
    {
        return
        [
            new()
            {
                Name = EVRActionCategory.Global,
                Type = ActionSetType.LeftRight,
                LocalizedNames = new Dictionary<string, string> { { "en_us", "English" } },
            },
            new()
            {
                Name = EVRActionCategory.OneHanded,
                Type = ActionSetType.Single,
            },
            new()
            {
                Name = EVRActionCategory.QuickMenu,
                Type = ActionSetType.Single,
            },
            new()
            {
                Name = EVRActionCategory.Menu,
                Type = ActionSetType.Single,
            },
            new()
            {
                Name = EVRActionCategory.AvatarMenu,
                Type = ActionSetType.Single,
            },
        ];
    }
    private static List<OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>> GetActions() =>
    [
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Interact,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Jump,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Suggested,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleMute,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Optional,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Grab,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.PlayspaceDragLeft,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Optional,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.PlayspaceDragRight,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Optional,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleMenu,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleQuickMenu,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Suggested,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleAvatarMenu,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.LeftHandPose,
            Category = EVRActionCategory.Global,
            Type = ActionType.Pose,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.RightHandPose,
            Category = EVRActionCategory.Global,
            Type = ActionType.Pose,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Locomote,
            Category = EVRActionCategory.Global,
            Type = ActionType.Vector2,
            Requirement = Requirement.Mandatory,
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Turn,
            Category = EVRActionCategory.Global,
            Type = ActionType.Scalar,
            Requirement = Requirement.Mandatory,
        },
    ];
    #endregion
}