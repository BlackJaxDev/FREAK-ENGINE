using XREngine.Scene;

namespace XREngine.Editor;
public static class Selection
{
    public static SceneNode[] SelectedNodes { get; set; } = [];
    public static SceneNode? Selected { get; set; } = null;
}
