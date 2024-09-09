﻿namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Binds the FBO that is set as the output FBO in the pipeline.
    /// This FBO may be null if the pipeline is not rendering to a framebuffer.
    /// </summary>
    /// <param name="pipeline"></param>
    public class VPRC_BindOutputFBO(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_UnbindFBO>(pipeline)
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
