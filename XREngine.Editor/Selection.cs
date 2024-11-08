using XREngine.Scene;

namespace XREngine.Editor;

/// <summary>
/// Tracks the current selection of scene nodes in the editor.
/// </summary>
public static class Selection
{
    public static event Action<SceneNode[]>? SelectionChanged;

    private static SceneNode[] _sceneNodes = [];
    public static SceneNode[] SceneNodes
    {
        get => _sceneNodes;
        set
        {
            _sceneNodes = value;
            SelectionChanged?.Invoke(value);
        }
    }

    public static SceneNode? SceneNode
    {
        get => SceneNodes.Length > 0 ? SceneNodes[0] : null;
        set => SceneNodes = value is not null ? [value] : [];
    }
}
