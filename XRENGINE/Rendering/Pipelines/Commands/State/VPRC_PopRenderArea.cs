namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_PopRenderArea : ViewportPopStateRenderCommand
    {
        protected override void Execute()
        {
            Pipeline.RenderState.PopRenderArea();
            Pipeline.RenderState.PopCropArea();
        }
    }
}
