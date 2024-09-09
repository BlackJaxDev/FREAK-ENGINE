namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_BindFBO(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_UnbindFBO>(pipeline)
    {
        public required XRFrameBuffer FrameBuffer { get; set; }

        protected override void Execute()
        {
            FrameBuffer.Bind();
            PopCommand.FrameBuffer = FrameBuffer;
        }
    }
}
