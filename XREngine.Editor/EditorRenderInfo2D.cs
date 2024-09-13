﻿using XREngine.Components;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Editor;

public class EditorRenderInfo2D(IRenderable owner, params RenderCommand[] renderCommands) : RenderInfo2D(owner, renderCommands)
{
    private bool _visibleInEditorOnly = false;
    private EEditorVisibility _editorVisibilityMode = EEditorVisibility.Unchanged;

    public bool VisibleInEditorOnly
    {
        get => _visibleInEditorOnly;
        set => SetField(ref _visibleInEditorOnly, value);
    }

    public EEditorVisibility EditorVisibilityMode
    {
        get => _editorVisibilityMode;
        set => SetField(ref _editorVisibilityMode, value);
    }

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
