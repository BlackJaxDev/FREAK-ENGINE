using XREngine.Data.Core;
using XREngine.Scene;

namespace XREngine
{
    public class GameWindowStartupSettings : XRBase
    {
        private EWindowState _windowState = EWindowState.Windowed;
        private string? windowTitle;
        private int _width = 1920;
        private int _height = 1080;
        private XRWorld? _targetWorld;
        private ELocalPlayerIndexMask _localPlayers = ELocalPlayerIndexMask.One;

        public ELocalPlayerIndexMask LocalPlayers
        {
            get => _localPlayers;
            set => SetField(ref _localPlayers, value);
        }

        public string? WindowTitle
        {
            get => windowTitle;
            set => SetField(ref windowTitle, value);
        }
        public int Width
        {
            get => _width;
            set => SetField(ref _width, value);
        }
        public int Height
        {
            get => _height;
            set => SetField(ref _height, value);
        }
        public XRWorld? TargetWorld
        {
            get => _targetWorld;
            set => SetField(ref _targetWorld, value);
        }
        public EWindowState WindowState
        {
            get => _windowState;
            set => SetField(ref _windowState, value);
        }
        public int X { get; set; }
        public int Y { get; set; }
        public bool VSync { get; set; } = false;
    }
}