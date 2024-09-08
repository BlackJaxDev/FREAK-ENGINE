namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_RenderMeshesPass(XRRenderPipeline pipeline) : ViewportStateRenderCommand<VPRC_PopRenderArea>(pipeline)
    {
        public required int RenderPass { get; set; }

        protected override void Execute()
        {
            Pipeline.MeshRenderCommands.Render(RenderPass);
        }
    }
}
