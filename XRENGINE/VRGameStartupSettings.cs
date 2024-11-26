using OpenVR.NET.Manifest;

namespace XREngine
{
    public interface IVRGameStartupSettings
    {
        VrManifest VRManifest { get; set; }
        IActionManifest ActionManifest { get; }
    }

    public class VRGameStartupSettings<TCategory, TAction> : GameStartupSettings, IVRGameStartupSettings
        where TCategory : struct, Enum
        where TAction : struct, Enum
    {
        private VrManifest _vrManifest = new();
        private ActionManifest<TCategory, TAction> _actionManifest = new();
        private (Environment.SpecialFolder folder, string relativePath)[] _gameSearchPaths = [];
        private string _gameName = "FreakEngineGame";

        /// <summary>
        /// The name of the process to search for when running in client mode.
        /// </summary>
        public string GameName
        {
            get => _gameName;
            set => SetField(ref _gameName, value);
        }
        /// <summary>
        /// Paths to search for game exe server when running in client mode.
        /// </summary>
        public (Environment.SpecialFolder folder, string relativePath)[] GameSearchPaths
        {
            get => _gameSearchPaths;
            set => SetField(ref _gameSearchPaths, value);
        }
        public VrManifest VRManifest
        {
            get => _vrManifest;
            set => SetField(ref _vrManifest, value);
        }
        public ActionManifest<TCategory, TAction> ActionManifest
        {
            get => _actionManifest;
            set => SetField(ref _actionManifest, value);
        }
        IActionManifest IVRGameStartupSettings.ActionManifest { get; } = new ActionManifest<TCategory, TAction>();
    }
}