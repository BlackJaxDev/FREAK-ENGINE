﻿using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.UI;

namespace XREngine.Data.Components
{
    public class DebugVisualize2DComponent : UIComponent, IRenderable
    {
        private readonly RenderInfo2D _renderInfo;
        private readonly RenderCommandMethod2D _rc;

        public RenderInfo2D RenderInfo => _renderInfo;
        public RenderCommandMethod2D RenderCommand => _rc;

        public delegate void DelDebugRenderCallback(DebugVisualize2DComponent comp);
        public event DelDebugRenderCallback? DebugRender;

        public delegate void DelPreRenderCallback(DebugVisualize2DComponent comp, RenderInfo info, RenderCommand command, XRCamera? camera, bool shadowPass);
        public event DelPreRenderCallback? PreRenderCallback;

        public delegate void DelSwapBuffersCallback(DebugVisualize2DComponent comp, RenderInfo info, RenderCommand command, bool shadowPass);
        public event DelSwapBuffersCallback? SwapBuffersCallback;

        protected virtual void Render()
            => DebugRender?.Invoke(this);

        public DebugVisualize2DComponent()
        {
            RenderedObjects = [_renderInfo = RenderInfo2D.New(this, _rc = new((int)EDefaultRenderPass.OnTopForward, Render))];
            _renderInfo.CollectedForRenderCallback += RenderInfo_PreRenderCallback;
            _renderInfo.SwapBuffersCallback += RenderInfo_SwapBuffersCallback;
        }
        ~DebugVisualize2DComponent()
        {
            _renderInfo.CollectedForRenderCallback -= RenderInfo_PreRenderCallback;
            _renderInfo.SwapBuffersCallback -= RenderInfo_SwapBuffersCallback;
        }

        protected virtual void RenderInfo_PreRenderCallback(RenderInfo info, RenderCommand command, XRCamera? camera, bool shadowPass)
            => PreRenderCallback?.Invoke(this, info, command, camera, shadowPass);

        protected virtual void RenderInfo_SwapBuffersCallback(RenderInfo info, RenderCommand command, bool shadowPass)
            => SwapBuffersCallback?.Invoke(this, info, command, shadowPass);

        public RenderInfo[] RenderedObjects { get; }
    }
}
