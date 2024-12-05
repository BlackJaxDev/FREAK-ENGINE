namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_IfElse : ViewportStateRenderCommand<VPRC_PopRenderArea>
    {
        public Func<bool>? ConditionEvaluator { get; set; }
        public ViewportRenderCommandContainer? TrueCommands { get; set; }
        public ViewportRenderCommandContainer? FalseCommands { get; set; }

        //private bool _lastCondition = false;

        protected override void Execute()
        {
            bool cond = ConditionEvaluator?.Invoke() ?? false;
            //if (NeedsCollecVisible)
            //    cond = _lastCondition;
            //else
            //{
            //    if (ConditionEvaluator is null)
            //        return;

            //    cond = ConditionEvaluator();
            //}

            if (cond)
                TrueCommands?.Execute();
            else
                FalseCommands?.Execute();
        }

        //public override bool NeedsCollecVisible
        //    => TrueCommands?.CollecVisibleCommands.Count > 0 || FalseCommands?.CollecVisibleCommands.Count > 0;

        //public override void CollectVisible()
        //{
        //    if (ConditionEvaluator is null)
        //        return;

        //    if (_lastCondition = ConditionEvaluator())
        //        TrueCommands?.CollectVisible();
        //    else
        //        FalseCommands?.CollectVisible();
        //}

        //public override void SwapBuffers()
        //{
        //    if (_lastCondition)
        //        TrueCommands?.SwapBuffers();
        //    else
        //        FalseCommands?.SwapBuffers();
        //}
    }
}
