namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_IfElse : ViewportStateRenderCommand<VPRC_PopRenderArea>
    {
        public Func<bool>? ConditionEvaluator { get; set; }
        public ViewportRenderCommandContainer? TrueCommands { get; set; }
        public ViewportRenderCommandContainer? FalseCommands { get; set; }

        protected override void Execute()
        {
            if (ConditionEvaluator is null)
                return;

            if (ConditionEvaluator())
                TrueCommands?.Execute();
            else
                FalseCommands?.Execute();
        }
    }
}
