namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_RenderDebugShapes : ViewportRenderCommand
    {
        protected override void Execute()
        {
            using (Pipeline.RenderState.PushRenderingCamera(Pipeline.RenderState.SceneCamera))
                Engine.Rendering.Debug.RenderShapes();
        }
    }
}