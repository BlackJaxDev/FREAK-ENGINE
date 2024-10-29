using OpenVR.NET.Manifest;
using System.Net;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Rendering;

namespace XREngine
{
    public class GameStartupSettings : XRBase
    {
        private List<GameWindowStartupSettings> _startupWindows = [];
        private EOutputVerbosity _outputVerbosity = EOutputVerbosity.Verbose;
        private bool _useIntegerWeightingIds = true;
        private UserSettings _defaultUserSettings = new();
        private ETwoPlayerPreference _twoPlayerViewportPreference;
        private EThreePlayerPreference _threePlayerViewportPreference;
        private string _texturesFolder = "";
        private EAppType _appType = EAppType.Client;
        private bool _isClient = true;
        private bool _isVR = false;
        private IActionManifest? _vrActionManifest;
        private VrManifest? _vrManifest;

        public List<GameWindowStartupSettings> StartupWindows
        {
            get => _startupWindows;
            set => SetField(ref _startupWindows, value);
        }
        public EOutputVerbosity OutputVerbosity
        {
            get => _outputVerbosity;
            set => SetField(ref _outputVerbosity, value);
        }
        public bool UseIntegerWeightingIds
        {
            get => _useIntegerWeightingIds;
            set => SetField(ref _useIntegerWeightingIds, value);
        }
        public UserSettings DefaultUserSettings
        {
            get => _defaultUserSettings;
            set => SetField(ref _defaultUserSettings, value);
        }
        public ETwoPlayerPreference TwoPlayerViewportPreference
        {
            get => _twoPlayerViewportPreference;
            set => SetField(ref _twoPlayerViewportPreference, value);
        }
        public EThreePlayerPreference ThreePlayerViewportPreference
        {
            get => _threePlayerViewportPreference;
            set => SetField(ref _threePlayerViewportPreference, value);
        }
        public string TexturesFolder
        {
            get => _texturesFolder;
            set => SetField(ref _texturesFolder, value);
        }
        public enum EAppType
        {
            Server,
            Client,
            P2PClient,
            VR,
        }
        public EAppType AppType
        {
            get => _appType;
            set => SetField(ref _appType, value);
        }
        public bool IsVR
        {
            get => _isVR;
            set => SetField(ref _isVR, value);
        }
        public IActionManifest? VRActionManifest
        {
            get => _vrActionManifest;
            set => SetField(ref _vrActionManifest, value);
        }
        public VrManifest? VRManifest
        {
            get => _vrManifest;
            set => SetField(ref _vrManifest, value);
        }
        public IPAddress UdpMulticastGroupIP { get; set; } = IPAddress.Parse("239.0.0.222");
        public int UdpMulticastServerPort { get; set; } = 5000;
        public IPAddress TcpListenerIP { get; set; } = IPAddress.Any;
        public int TcpListenerPort { get; set; } = 5001;
        public IPAddress ServerIP { get; set; } = IPAddress.Parse("127.0.0.1");
    }
    public class GameState
    {
        public List<GameWindowStartupSettings>? Windows { get; set; } = [];
        public List<XRWorldInstance>? Worlds { get; set; } = [];
    }
}