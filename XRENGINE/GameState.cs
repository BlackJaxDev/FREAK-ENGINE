using XREngine.Core.Files;
using XREngine.Rendering;

namespace XREngine
{
    public class GameState : XRAsset
    {
        private List<GameWindowStartupSettings>? _windows = [];
        private List<XRWorldInstance>? _worlds = [];

        public List<GameWindowStartupSettings>? Windows
        {
            get => _windows;
            set => SetField(ref _windows, value);
        }
        public List<XRWorldInstance>? Worlds
        {
            get => _worlds;
            set => SetField(ref _worlds, value);
        }
    }
}