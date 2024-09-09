namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_PopRenderArea(ViewportRenderCommandContainer pipeline) : ViewportPopStateRenderCommand(pipeline)
    {
        protected override void Execute()
            => Engine.Rendering.State.RenderAreas.Pop();
    }
}
