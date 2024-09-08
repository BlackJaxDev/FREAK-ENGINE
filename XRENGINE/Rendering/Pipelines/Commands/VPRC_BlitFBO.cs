namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Renders an FBO quad to another FBO.
    /// Useful for transforming every pixel of previous FBO.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    public class VPRC_BlitFBO(XRRenderPipeline pipeline) : ViewportRenderCommand(pipeline)
    {
        public XRQuadFrameBuffer? Source { get; set; }
        public XRFrameBuffer? Destination { get; set; }

        public void SetTargets(XRQuadFrameBuffer source, XRFrameBuffer destination)
        {
            Source = source;
            Destination = destination;
        }

        protected override void Execute()
        {
            if (Destination is null)
                return;

            Source?.Render(Destination);
        }
    }
}
