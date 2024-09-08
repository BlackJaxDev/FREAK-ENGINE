namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_IfElse(XRRenderPipeline pipeline) : ViewportStateRenderCommand<VPRC_PopRenderArea>(pipeline)
    {
        public required Func<bool> ConditionEvaluator { get; set; }
        public ViewportRenderCommandContainer? TrueCommands { get; set; }
        public ViewportRenderCommandContainer? FalseCommands { get; set; }

        protected override void Execute()
        {
            if (ConditionEvaluator())
                TrueCommands?.Execute();
            else
                FalseCommands?.Execute();
        }
    }
}
