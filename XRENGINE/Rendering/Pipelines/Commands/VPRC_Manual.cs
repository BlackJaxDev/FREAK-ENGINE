namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_Manual(XRRenderPipeline pipeline) : ViewportRenderCommand(pipeline)
    {
        public required Action ManualAction { get; set; }
        protected override void Execute()
        {
            ManualAction?.Invoke();
        }
    }
}
