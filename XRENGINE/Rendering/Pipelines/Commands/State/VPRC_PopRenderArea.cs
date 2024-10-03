namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_PopRenderArea(ViewportRenderCommandContainer pipeline) : ViewportPopStateRenderCommand(pipeline)
    {
        protected override void Execute()
            => Pipeline.State.PopRenderArea();
    }
}
