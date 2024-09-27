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
    }
    public class GameState
    {
        public List<GameWindowStartupSettings>? Windows { get; set; } = [];
        public List<XRWorldInstance>? Worlds { get; set; } = [];
    }
}