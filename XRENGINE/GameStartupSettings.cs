using OpenVR.NET.Manifest;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Rendering;

namespace XREngine
{
    public class GameStartupSettings : XRBase
    {
        public List<GameWindowStartupSettings> StartupWindows { get; set; } = [];
        public EOutputVerbosity OutputVerbosity { get; set; } = EOutputVerbosity.Verbose;
        public bool UseIntegerWeightingIds { get; set; } = true;
        public UserSettings DefaultUserSettings { get; set; } = new UserSettings();
        public ETwoPlayerPreference TwoPlayerViewportPreference { get; set; }
        public EThreePlayerPreference ThreePlayerViewportPreference { get; set; }
        public string TexturesFolder { get; set; } = "";
        public bool IsServer { get; set; } = false;
        public bool IsClient { get; set; } = true;
        public bool IsVR { get; set; } = false;
        public IActionManifest? VRActionManifest { get; set; }
        public VrManifest? VRManifest { get; set; }
    }
    public class GameState
    {
        public List<GameWindowStartupSettings>? Windows { get; set; } = [];
        public List<XRWorldInstance>? Worlds { get; set; } = [];
    }
}