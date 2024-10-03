namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_RenderMeshesPass(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_PopRenderArea>(pipeline)
    {
        public required int RenderPass { get; set; }

        protected override void Execute()
        {
            using (Engine.Rendering.State.PushRenderingCamera(Pipeline.RenderStatus.Camera))
                Pipeline.MeshRenderCommands.Render(RenderPass);
        }
    }
}
