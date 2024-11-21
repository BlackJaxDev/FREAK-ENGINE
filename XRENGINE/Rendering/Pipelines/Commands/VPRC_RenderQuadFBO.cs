namespace XREngine.Rendering.Pipelines.Commands
{
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