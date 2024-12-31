using XREngine.Components;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Data.Components
{
    public class DebugVisualize3DComponent : XRComponent, IRenderable
    {
        private readonly RenderInfo3D _renderInfo;
        private readonly RenderCommandMethod3D _rc;

        public RenderInfo3D RenderInfo => _renderInfo;
        public RenderCommandMethod3D RenderCommand => _rc;

        public delegate void DelDebugRenderCallback(DebugVisualize3DComponent comp, bool shadowPass);
        public event DelDebugRenderCallback? DebugRender;

        public delegate void DelPreRenderCallback(DebugVisualize3DComponent comp, RenderInfo info, RenderCommand command, XRCamera? camera, bool shadowPass);
        public event DelPreRenderCallback? PreRenderCallback;

        public delegate void DelSwapBuffersCallback(DebugVisualize3DComponent comp, RenderInfo info, RenderCommand command, bool shadowPass);
        public event DelSwapBuffersCallback? SwapBuffersCallback;

        protected virtual void Render(bool shadowPass)
            => DebugRender?.Invoke(this, shadowPass);

        public DebugVisualize3DComponent()
        {
            RenderedObjects = [_renderInfo = RenderInfo3D.New(this, _rc = new((int)EDefaultRenderPass.OnTopForward, Render))];
            _renderInfo.PreRenderCallback += RenderInfo_PreRenderCallback;
            _renderInfo.SwapBuffersCallback += RenderInfo_SwapBuffersCallback;
        }
        ~DebugVisualize3DComponent()
        {
            _renderInfo.PreRenderCallback -= RenderInfo_PreRenderCallback;
            _renderInfo.SwapBuffersCallback -= RenderInfo_SwapBuffersCallback;
        }

        protected virtual void RenderInfo_SwapBuffersCallback(RenderInfo info, RenderCommand command, bool shadowPass)
            => SwapBuffersCallback?.Invoke(this, info, command, shadowPass);

        protected virtual void RenderInfo_PreRenderCallback(RenderInfo info, RenderCommand command, XRCamera? camera, bool shadowPass)
            => PreRenderCallback?.Invoke(this, info, command, camera, shadowPass);

        public RenderInfo[] RenderedObjects { get; }
    }
}
