using XREngine.Components;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Editor;

/// <summary>
/// Derived class of RenderInfo3D that adds editor-specific properties.
/// </summary>
public class EditorRenderInfo3D(IRenderable owner) : RenderInfo3D(owner)
{
    public bool VisibleInEditorOnly { get; set; } = false;
    public EEditorVisibility EditorVisibilityMode { get; set; } = EEditorVisibility.Unchanged;

    public override bool AllowRender(IVolume? cullingVolume, RenderCommandCollection passes, XRCamera camera)
    {
        if ((Owner is CameraComponent ccomp && ccomp.Camera == camera) ||
            (Owner is XRCamera cam && cam == camera))
            return false;

        if (EditorState.InPlayMode && VisibleInEditorOnly)
            return false;

        return base.AllowRender(cullingVolume, passes, camera);
    }
}