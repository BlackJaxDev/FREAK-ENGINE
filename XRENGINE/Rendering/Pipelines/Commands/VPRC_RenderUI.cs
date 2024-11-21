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
                ui?.Render(vp, fbo);
        }
    }
}