using XREngine.Core.Files;

namespace XREngine.Scene
{
    /// <summary>
    /// Manages all 3D scene data for a particular consistent instance.
    /// For example, multiple viewports can point to cameras in this world and see the same 3D scene from different viewpoints.
    /// </summary>
    [Serializable]
    public class XRWorld : XRAsset
    {
        public List<XRScene> Scenes { get; set; } = [];
        public GameMode? DefaultGameMode { get; set; } = null;
        public WorldSettings Settings { get; set; } = new WorldSettings();
    }
}