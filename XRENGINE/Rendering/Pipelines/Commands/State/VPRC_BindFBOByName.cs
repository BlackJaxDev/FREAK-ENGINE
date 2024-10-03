namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_BindFBOByName(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_UnbindFBO>(pipeline)
    {
        public required string FrameBufferName { get; set; }
        public bool Write { get; set; } = true;

        public void SetOptions(string frameBufferName, bool write)
        {
            FrameBufferName = frameBufferName;
            Write = write;
        }

        protected override void Execute()
        {
            var fbo = Pipeline.GetFBO<XRFrameBuffer>(FrameBufferName);
            if (fbo is null)
                return;

            if (Write)
                fbo.BindForWriting();
            else
                fbo.BindForReading();

            PopCommand.FrameBuffer = fbo;
        }
    }
}
