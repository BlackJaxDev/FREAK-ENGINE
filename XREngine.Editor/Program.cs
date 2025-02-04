using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using OpenVR.NET.Manifest;
using System.Xml.Linq;
using XREngine;
using XREngine.Editor;
using XREngine.Native;
using XREngine.Rendering;
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
        var world = EditorWorld.CreateTestWorld();
        startup.StartupWindows[0].TargetWorld = world;

        Engine.Assets.GameFileChanged += VerifyCodeAsset;
        Engine.Assets.GameFileCreated += VerifyCodeAsset;
        Engine.Assets.GameFileDeleted += VerifyCodeAsset;
        Engine.Assets.GameFileRenamed += VerifyCodeAsset;
        XRWindow.AnyWindowFocusChanged += AnyWindowFocusChanged;
        Engine.Run(startup, Engine.LoadOrGenerateGameState());
    }

    private static void AnyWindowFocusChanged(XRWindow window, bool focused)
    {
        if (focused && _isGameClientInvalid)
            CompileGameClient();
    }

    private static void VerifyCodeAsset(FileSystemEventArgs e)
    {
        if (e.FullPath.EndsWith(".cs"))
            _isGameClientInvalid = true;
    }

    private static bool _isGameClientInvalid = false;

    private static void CompileGameClient()
    {
        string sourceFolder = Engine.Assets.GameAssetsPath;
        string outputFolder = Engine.Assets.LibrariesPath;
        string projectName = Engine.GameSettings.Name ?? "GeneratedProject";
        string projectFilePath = Path.Combine(sourceFolder, $"{projectName}.csproj");

        CreateCsprojFile(sourceFolder, projectFilePath);
        CompileProject(projectFilePath, outputFolder);
    }

    private static void CreateCsprojFile(string sourceFolder, string projectFilePath)
    {
        var project = new XDocument(
            new XElement("Project",
                new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("PropertyGroup",
                    new XElement("OutputType", "Library"),
                    new XElement("TargetFramework", "net9.0")
                ),
                new XElement("ItemGroup",
                    Directory.GetFiles(sourceFolder, "*.cs", SearchOption.AllDirectories)
                        .Select(file => new XElement("Compile", new XAttribute("Include", file)))
                )
            )
        );
        project.Save(projectFilePath);
    }

    private static void CompileProject(string projectFilePath, string outputFolder)
    {
        var projectCollection = new ProjectCollection();
        var buildParameters = new BuildParameters(projectCollection)
        {
            Loggers = [new ConsoleLogger(LoggerVerbosity.Minimal)]
        };

        var buildRequest = new BuildRequestData(projectFilePath, new Dictionary<string, string?>(), null, ["Build"], null);
        var buildResult = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);

        if (buildResult.OverallResult == BuildResultCode.Success)
        {
            Debug.Out("Build succeeded.");
            _isGameClientInvalid = false;
        }
        else
        {
            Debug.Out("Build failed.");
            foreach (var message in buildResult.ResultsByTarget.Values.SelectMany(x => x.Items))
            {
                //Debug.Out(message.ItemType.ToString());
                Debug.Out(message.ItemSpec);
                Debug.Out(message.GetMetadata("Message"));
            }
        }
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
        float updateHz = 60.0f;
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