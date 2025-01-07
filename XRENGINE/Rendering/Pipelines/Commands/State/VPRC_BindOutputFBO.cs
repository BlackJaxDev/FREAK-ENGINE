namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Binds the FBO that is set as the output FBO in the pipeline.
    /// This FBO may be null if the pipeline is not rendering to a framebuffer.
    /// </summary>
    /// <param name="pipeline"></param>
    public class VPRC_BindOutputFBO : ViewportStateRenderCommand<VPRC_UnbindFBO>
    {
        public bool Write { get; set; } = true;

        protected override void Execute()
        {
            var fbo = Pipeline.RenderState.OutputFBO;
            if (fbo is null)
                return;
            
            if (Write)
                fbo.BindForWriting();
            else
                fbo.BindForReading();

            PopCommand.FrameBuffer = fbo;
            PopCommand.Write = Write;
        }
    }
}
