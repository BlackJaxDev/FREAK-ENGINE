namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_RenderQuadFBO(ViewportRenderCommandContainer pipeline) : ViewportRenderCommand(pipeline)
    {
        public required string FrameBufferName { get; set; }
        public string? TargetFrameBufferName { get; set; }

        protected override void Execute()
        {
            var inputFBO = Pipeline.GetFBO<XRQuadFrameBuffer>(FrameBufferName);
            if (inputFBO == null)
                return;

            inputFBO.Render(TargetFrameBufferName != null ? Pipeline.GetFBO<XRFrameBuffer>(TargetFrameBufferName) : null);
        }
    }
}