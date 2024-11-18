namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_RenderDebugShapes : ViewportRenderCommand
    {
        protected override void Execute()
        {
            Engine.Rendering.Debug.RenderShapes();
        }
    }
}