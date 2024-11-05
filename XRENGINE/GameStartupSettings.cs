using OpenVR.NET.Manifest;
using System.Net;
using XREngine.Core.Files;
using XREngine.Data.Rendering;

namespace XREngine
{
    public class GameStartupSettings : XRAsset
    {
        private EAppType _appType = EAppType.Client;
        private List<GameWindowStartupSettings> _startupWindows = [];
        private ETwoPlayerPreference _twoPlayerViewportPreference;
        private EThreePlayerPreference _threePlayerViewportPreference;
        private EOutputVerbosity _outputVerbosity = EOutputVerbosity.Verbose;
        private bool _useIntegerWeightingIds = true;
        private UserSettings _defaultUserSettings = new();
        private string _texturesFolder = "";
        private float? _targetUpdatesPerSecond = 90.0f;
        private float _fixedFramesPerSecond = 90.0f;

        private IActionManifest? _vrActionManifest;
        private VrManifest? _vrManifest;

        private string _udpMulticastGroupIP = "239.0.0.222";
        private int _udpMulticastServerPort = 5000;
        private string _tcpListenerIP = "0.0.0.0";
        private int _tcpListenerPort = 5001;
        private string _serverIP = "127.0.0.1";

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
        public string UdpMulticastGroupIP
        {
            get => _udpMulticastGroupIP;
            set => SetField(ref _udpMulticastGroupIP, value);
        }
        public int UdpMulticastServerPort
        {
            get => _udpMulticastServerPort;
            set => SetField(ref _udpMulticastServerPort, value);
        }
        public string TcpListenerIP
        {
            get => _tcpListenerIP;
            set => SetField(ref _tcpListenerIP, value);
        }
        public int TcpListenerPort
        {
            get => _tcpListenerPort;
            set => SetField(ref _tcpListenerPort, value);
        }
        public string ServerIP
        {
            get => _serverIP;
            set => SetField(ref _serverIP, value);
        }
        public float? TargetUpdatesPerSecond
        {
            get => _targetUpdatesPerSecond;
            set => SetField(ref _targetUpdatesPerSecond, value);
        }
        public float FixedFramesPerSecond
        {
            get => _fixedFramesPerSecond;
            set => SetField(ref _fixedFramesPerSecond, value);
        }
    }
}