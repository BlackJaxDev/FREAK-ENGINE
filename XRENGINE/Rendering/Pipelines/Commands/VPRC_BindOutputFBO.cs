namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_BindOutputFBO(XRRenderPipeline pipeline) : ViewportStateRenderCommand<VPRC_UnbindFBO>(pipeline)
    {
        protected override void Execute()
        {
            var fbo = Pipeline.RenderStatus.OutputFBO;
            if (fbo != null)
            {
                fbo.Bind();
                PopCommand.FrameBuffer = fbo;
            }
        }
    }
}
