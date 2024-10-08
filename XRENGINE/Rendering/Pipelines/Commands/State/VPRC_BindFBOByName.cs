namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_BindFBOByName : ViewportStateRenderCommand<VPRC_UnbindFBO>
    {
        public string? FrameBufferName { get; set; }
        public bool Write { get; set; } = true;

        public void SetOptions(string frameBufferName, bool write)
        {
            FrameBufferName = frameBufferName;
            Write = write;
        }

        protected override void Execute()
        {
            if (FrameBufferName is null)
                return;

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
