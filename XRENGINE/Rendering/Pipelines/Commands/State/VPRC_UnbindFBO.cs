namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_UnbindFBO(ViewportRenderCommandContainer pipeline) : ViewportPopStateRenderCommand(pipeline)
    {
        /// <summary>
        /// The framebuffer to unbind. This should be set by bind command, and will be set to null after execution.
        /// </summary>
        public XRFrameBuffer? FrameBuffer { get; set; }
        public bool Write { get; set; } = true;

        public void SetOptions(XRFrameBuffer frameBuffer, bool write)
        {
            FrameBuffer = frameBuffer;
            Write = write;
        }

        protected override void Execute()
        {
            if (FrameBuffer is null)
                return;

            if (Write)
                FrameBuffer.UnbindFromWriting();
            else
                FrameBuffer.UnbindFromReading();

            FrameBuffer = null;
        }
    }
}
