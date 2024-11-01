using XREngine.Rendering.UI;

namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Render's the camera's screen space UI to the current viewport.
    /// </summary>
    public class VPRC_RenderUI : ViewportRenderCommand
    {
        public string UserInterfaceFBOName { get; set; } = string.Empty;

        protected override void Execute()
        {
            var ui = Pipeline.State.UserInterface;
            var vp = Pipeline.State.RenderingViewport;
            if (ui is null || vp is null)
                return;
            
            var fbo = Pipeline.GetFBO<XRQuadFrameBuffer>(UserInterfaceFBOName);
            if (fbo is not null)
                ui?.RenderScreenSpace(vp, fbo);
        }
    }
    public class VPRC_RenderQuadFBO : ViewportRenderCommand
    {
        public string? FrameBufferName { get; set; }
        public string? TargetFrameBufferName { get; set; }

        protected override void Execute()
        {
            if (FrameBufferName is null)
                return;

            var inputFBO = Pipeline.GetFBO<XRQuadFrameBuffer>(FrameBufferName);
            if (inputFBO is null)
                return;

            inputFBO.Render(TargetFrameBufferName != null ? Pipeline.GetFBO<XRFrameBuffer>(TargetFrameBufferName) : null);
        }
    }
}