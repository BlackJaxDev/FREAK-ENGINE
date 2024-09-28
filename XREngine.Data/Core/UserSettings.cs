using XREngine.Data.Core;
using XREngine.Data.Vectors;

namespace XREngine
{
    [Serializable]
    public class UserSettings : XRBase
    {
        private EWindowState _windowState = EWindowState.Windowed;
        private EVSyncMode _vSyncMode = EVSyncMode.Adaptive;
        private EEngineQuality _textureQuality = EEngineQuality.Highest;
        private EEngineQuality _modelQuality = EEngineQuality.Highest;
        private EEngineQuality _soundQuality = EEngineQuality.Highest;

        //Preferred libraries - will use whichever is available if the preferred one is not.
        private ERenderLibrary _renderLibrary = ERenderLibrary.OpenGL;
        private EAudioLibrary _audioLibrary = EAudioLibrary.OpenAL;
        private EInputLibrary _inputLibrary = EInputLibrary.XInput;
        private EPhysicsLibrary _physicsLibrary = EPhysicsLibrary.PhysX;

        private float? _targetUpdatesPerSecond = 90.0f;
        private float? _targetFramesPerSecond = 90.0f;
        private IVector2 _windowedResolution = new(1920, 1080);

        public EVSyncMode VSync
        {
            get => _vSyncMode;
            set => SetField(ref _vSyncMode, value);
        }
        public EEngineQuality TextureQuality
        {
            get => _textureQuality;
            set => SetField(ref _textureQuality, value);
        }
        public EEngineQuality ModelQuality
        {
            get => _modelQuality;
            set => SetField(ref _modelQuality, value);
        }
        public EEngineQuality SoundQuality
        {
            get => _soundQuality;
            set => SetField(ref _soundQuality, value);
        }
        public ERenderLibrary RenderLibrary
        {
            get => _renderLibrary;
            set => SetField(ref _renderLibrary, value);
        }
        public EAudioLibrary AudioLibrary
        {
            get => _audioLibrary;
            set => SetField(ref _audioLibrary, value);
        }
        public EInputLibrary InputLibrary
        {
            get => _inputLibrary;
            set => SetField(ref _inputLibrary, value);
        }
        public EPhysicsLibrary PhysicsLibrary
        {
            get => _physicsLibrary;
            set => SetField(ref _physicsLibrary, value);
        }
        public float? TargetUpdatesPerSecond
        {
            get => _targetUpdatesPerSecond;
            set => SetField(ref _targetUpdatesPerSecond, value);
        }
        public float? TargetFramesPerSecond
        {
            get => _targetFramesPerSecond;
            set => SetField(ref _targetFramesPerSecond, value);
        }
        public double DebugOutputRecencySeconds { get; set; } = 60.0;
    }
}
