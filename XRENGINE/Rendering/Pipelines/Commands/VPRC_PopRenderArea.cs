namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_PopRenderArea(XRRenderPipeline pipeline) : ViewportPopStateRenderCommand(pipeline)
    {
        protected override void Execute()
            => Engine.Rendering.State.RenderAreas.Pop();
    }
}
