namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_RenderDebugShapes : ViewportRenderCommand
    {
        protected override void Execute()
        {
            using (Pipeline.State.PushRenderingCamera(Pipeline.State.SceneCamera))
                Engine.Rendering.Debug.RenderShapes();
        }
    }
}