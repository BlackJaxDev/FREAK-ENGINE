using XREngine.Components;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Editor;

public class EditorRenderInfo2D(IRenderable owner) : RenderInfo2D(owner)
{
    public bool VisibleInEditorOnly { get; set; } = false;
    public EEditorVisibility EditorVisibilityMode { get; set; } = EEditorVisibility.Unchanged;

    public override bool AllowRender(BoundingRectangleF? cullingVolume, RenderCommandCollection passes, XRCamera camera)
    {
        if ((Owner is CameraComponent ccomp && ccomp.Camera == camera) ||
            (Owner is XRCamera cam && cam == camera))
            return false;

        if (EditorState.InPlayMode && VisibleInEditorOnly)
            return false;

        return base.AllowRender(cullingVolume, passes, camera);
    }
}
