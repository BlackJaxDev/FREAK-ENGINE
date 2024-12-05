namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_RenderDebugPhysics : ViewportRenderCommand
    {
        protected override void Execute()
        {
            using (Pipeline.RenderState.PushRenderingCamera(Pipeline.RenderState.SceneCamera))
                Pipeline.RenderState.WindowViewport?.World?.PhysicsScene?.DebugRender();
        }
    }
}