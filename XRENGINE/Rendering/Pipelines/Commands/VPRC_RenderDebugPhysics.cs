namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_RenderDebugPhysics : ViewportRenderCommand
    {
        protected override void Execute()
        {
            using (Pipeline.State.PushRenderingCamera(Pipeline.State.SceneCamera))
                Pipeline.State.WindowViewport?.World?.PhysicsScene?.DebugRender();
        }
    }
}