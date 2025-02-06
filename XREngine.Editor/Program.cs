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
        float updateHz = 90.0f;
        float renderHz = 0.0f;
        float fixedHz = 45.0f;

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
                LocalizedNames = new Dictionary<string, string>
                {
                    { "en_us", "Global" },
                    { "fr", "Global" },
                    { "de", "Global" },
                    { "es", "Global" },
                    { "it", "Global" },
                    { "ja", "グローバル" },
                    { "ko", "글로벌" },
                    { "nl", "Globaal" },
                    { "pl", "Globalny" },
                    { "pt", "Global" },
                    { "ru", "Глобальный" },
                    { "zh", "全球" },
                },
            },
            new()
            {
                Name = EVRActionCategory.OneHanded,
                Type = ActionSetType.Single,
                LocalizedNames = new Dictionary<string, string>
                {
                    { "en_us", "One Handed" },
                    { "fr", "À une main" },
                    { "de", "Einhändig" },
                    { "es", "De una mano" },
                    { "it", "A una mano" },
                    { "ja", "片手" },
                    { "ko", "한 손" },
                    { "nl", "Eenhandig" },
                    { "pl", "Jednoręki" },
                    { "pt", "De uma mão" },
                    { "ru", "Однорукий" },
                    { "zh", "单手" },
                },
            },
            new()
            {
                Name = EVRActionCategory.QuickMenu,
                Type = ActionSetType.Single,
                LocalizedNames = new Dictionary<string, string>
                {
                    { "en_us", "Quick Menu" },
                    { "fr", "Menu rapide" },
                    { "de", "Schnellmenü" },
                    { "es", "Menú rápido" },
                    { "it", "Menu rapido" },
                    { "ja", "クイックメニュー" },
                    { "ko", "빠른 메뉴" },
                    { "nl", "Snelmenu" },
                    { "pl", "Szybkie menu" },
                    { "pt", "Menu rápido" },
                    { "ru", "Быстрое меню" },
                    { "zh", "快速菜单" },
                },
            },
            new()
            {
                Name = EVRActionCategory.Menu,
                Type = ActionSetType.Single,
                LocalizedNames = new Dictionary<string, string>
                {
                    { "en_us", "Menu" },
                    { "fr", "Menu" },
                    { "de", "Menü" },
                    { "es", "Menú" },
                    { "it", "Menu" },
                    { "ja", "メニュー" },
                    { "ko", "메뉴" },
                    { "nl", "Menu" },
                    { "pl", "Menu" },
                    { "pt", "Menu" },
                    { "ru", "Меню" },
                    { "zh", "菜单" },
                },
            },
            new()
            {
                Name = EVRActionCategory.AvatarMenu,
                Type = ActionSetType.Single,
                LocalizedNames = new Dictionary<string, string>
                {
                    { "en_us", "Avatar Menu" },
                    { "fr", "Menu de l'avatar" },
                    { "de", "Avatar-Menü" },
                    { "es", "Menú de avatar" },
                    { "it", "Menu avatar" },
                    { "ja", "アバターメニュー" },
                    { "ko", "아바타 메뉴" },
                    { "nl", "Avatar-menu" },
                    { "pl", "Menu awatara" },
                    { "pt", "Menu de avatar" },
                    { "ru", "Меню аватара" },
                    { "zh", "头像菜单" },
                },
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
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Interact" },
                { "fr", "Interagir" },
                { "de", "Interagieren" },
                { "es", "Interactuar" },
                { "it", "Interagire" },
                { "ja", "相互作用" },
                { "ko", "상호 작용" },
                { "nl", "Interactie" },
                { "pl", "Wzajemne oddziaływanie" },
                { "pt", "Interagir" },
                { "ru", "взаимодействовать" },
                { "zh", "互动" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Jump,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Suggested,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Jump" },
                { "fr", "Sauter" },
                { "de", "Springen" },
                { "es", "Saltar" },
                { "it", "Saltare" },
                { "ja", "ジャンプ" },
                { "ko", "점프" },
                { "nl", "Springen" },
                { "pl", "Skok" },
                { "pt", "Pular" },
                { "ru", "Прыгать" },
                { "zh", "跳" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleMute,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Optional,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Toggle Mute" },
                { "fr", "Activer/Désactiver le son" },
                { "de", "Stummschaltung umschalten" },
                { "es", "Activar/Desactivar silencio" },
                { "it", "Attiva/Disattiva muto" },
                { "ja", "ミュートの切り替え" },
                { "ko", "음소거 전환" },
                { "nl", "Geluid dempen in-/uitschakelen" },
                { "pl", "Przełącz wyciszenie" },
                { "pt", "Alternar mudo" },
                { "ru", "Переключить звук" },
                { "zh", "切换静音" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Grab,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Grab" },
                { "fr", "Saisir" },
                { "de", "Greifen" },
                { "es", "Agarrar" },
                { "it", "Afferrare" },
                { "ja", "つかむ" },
                { "ko", "잡다" },
                { "nl", "Grijpen" },
                { "pl", "Chwycić" },
                { "pt", "Agarrar" },
                { "ru", "Захват" },
                { "zh", "抓" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.PlayspaceDragLeft,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Optional,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Playspace Drag Left" },
                { "fr", "Glisser l'espace de jeu à gauche" },
                { "de", "Playspace nach links ziehen" },
                { "es", "Arrastrar el espacio de juego a la izquierda" },
                { "it", "Trascina lo spazio di gioco a sinistra" },
                { "ja", "プレイスペースを左にドラッグ" },
                { "ko", "플레이 스페이스를 왼쪽으로 드래그" },
                { "nl", "Playspace naar links slepen" },
                { "pl", "Przeciągnij obszar gry w lewo" },
                { "pt", "Arrastar o espaço de jogo para a esquerda" },
                { "ru", "Перетащить игровое пространство влево" },
                { "zh", "将游戏空间拖到左边" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.PlayspaceDragRight,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Optional,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Playspace Drag Right" },
                { "fr", "Glisser l'espace de jeu à droite" },
                { "de", "Playspace nach rechts ziehen" },
                { "es", "Arrastrar el espacio de juego a la derecha" },
                { "it", "Trascina lo spazio di gioco a destra" },
                { "ja", "プレイスペースを右にドラッグ" },
                { "ko", "플레이 스페이스를 오른쪽으로 드래그" },
                { "nl", "Playspace naar rechts slepen" },
                { "pl", "Przeciągnij obszar gry w prawo" },
                { "pt", "Arrastar o espaço de jogo para a direita" },
                { "ru", "Перетащить игровое пространство вправо" },
                { "zh", "将游戏空间拖到右边" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleMenu,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Toggle Menu" },
                { "fr", "Basculer le menu" },
                { "de", "Menü umschalten" },
                { "es", "Alternar menú" },
                { "it", "Attiva/Disattiva menu" },
                { "ja", "メニューの切り替え" },
                { "ko", "메뉴 전환" },
                { "nl", "Menu in-/uitschakelen" },
                { "pl", "Przełącz menu" },
                { "pt", "Alternar menu" },
                { "ru", "Переключить меню" },
                { "zh", "切换菜单" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleQuickMenu,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Suggested,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Toggle Quick Menu" },
                { "fr", "Basculer le menu rapide" },
                { "de", "Schnellmenü umschalten" },
                { "es", "Alternar menú rápido" },
                { "it", "Attiva/Disattiva menu rapido" },
                { "ja", "クイックメニューの切り替え" },
                { "ko", "빠른 메뉴 전환" },
                { "nl", "Snelmenu in-/uitschakelen" },
                { "pl", "Przełącz szybkie menu" },
                { "pt", "Alternar menu rápido" },
                { "ru", "Переключить быстрое меню" },
                { "zh", "切换快速菜单" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.ToggleAvatarMenu,
            Category = EVRActionCategory.Global,
            Type = ActionType.Boolean,
            Requirement = Requirement.Mandatory,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Toggle Avatar Menu" },
                { "fr", "Basculer le menu de l'avatar" },
                { "de", "Avatar-Menü umschalten" },
                { "es", "Alternar menú de avatar" },
                { "it", "Attiva/Disattiva menu avatar" },
                { "ja", "アバターメニューの切り替え" },
                { "ko", "아바타 메뉴 전환" },
                { "nl", "Avatar-menu in-/uitschakelen" },
                { "pl", "Przełącz menu awatara" },
                { "pt", "Alternar menu de avatar" },
                { "ru", "Переключить меню аватара" },
                { "zh", "切换头像菜单" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.LeftHandPose,
            Category = EVRActionCategory.Global,
            Type = ActionType.Pose,
            Requirement = Requirement.Mandatory,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Left Hand Pose" },
                { "fr", "Pose de la main gauche" },
                { "de", "Linke Hand Pose" },
                { "es", "Pose de la mano izquierda" },
                { "it", "Posa della mano sinistra" },
                { "ja", "左手のポーズ" },
                { "ko", "왼손 포즈" },
                { "nl", "Linkerhandpose" },
                { "pl", "Pozycja lewej ręki" },
                { "pt", "Pose da mão esquerda" },
                { "ru", "Поза левой руки" },
                { "zh", "左手姿势" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.RightHandPose,
            Category = EVRActionCategory.Global,
            Type = ActionType.Pose,
            Requirement = Requirement.Mandatory,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Right Hand Pose" },
                { "fr", "Pose de la main droite" },
                { "de", "Rechte Hand Pose" },
                { "es", "Pose de la mano derecha" },
                { "it", "Posa della mano destra" },
                { "ja", "右手のポーズ" },
                { "ko", "오른손 포즈" },
                { "nl", "Rechterhandpose" },
                { "pl", "Pozycja prawej ręki" },
                { "pt", "Pose da mão direita" },
                { "ru", "Поза правой руки" },
                { "zh", "右手姿势" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Locomote,
            Category = EVRActionCategory.Global,
            Type = ActionType.Vector2,
            Requirement = Requirement.Mandatory,
            LocalizedNames = new Dictionary<string, string>
            {
                { "en_us", "Locomote" },
                { "fr", "Se déplacer" },
                { "de", "Fortbewegen" },
                { "es", "Desplazarse" },
                { "it", "Muoversi" },
                { "ja", "移動" },
                { "ko", "이동" },
                { "nl", "Verplaatsen" },
                { "pl", "Poruszanie się" },
                { "pt", "Locomover" },
                { "ru", "Перемещение" },
                { "zh", "移动" },
            },
        },
        new OpenVR.NET.Manifest.Action<EVRActionCategory, EVRGameAction>()
        {
            Name = EVRGameAction.Turn,
            Category = EVRActionCategory.Global,
            Type = ActionType.Scalar,
            Requirement = Requirement.Mandatory,
            LocalizedNames = new Dictionary<string, string> 
            { 
                { "en_us", "Turn" },
                { "fr", "Tourner" },
                { "de", "Drehen" },
                { "es", "Girar" },
                { "it", "Girare" },
                { "ja", "回転" },
                { "ko", "회전" },
                { "nl", "Draaien" },
                { "pl", "Obrót" },
                { "pt", "Girar" },
                { "ru", "Поворот" },
                { "zh", "转" },
            },
        },
    ];
    #endregion
}