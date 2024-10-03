namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_BindFBOByName(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_UnbindFBO>(pipeline)
    {
        public required string FrameBufferName { get; set; }

        protected override void Execute()
        {
            var fbo = Pipeline.GetFBO<XRFrameBuffer>(FrameBufferName);
            if (fbo is null)
                return;
            
            fbo.BindForWriting();
            PopCommand.FrameBuffer = fbo;
        }
    }
}
