namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_Switch : ViewportStateRenderCommand<VPRC_PopRenderArea>
    {
        public Func<int>? SwitchEvaluator { get; set; }
        public Dictionary<int, ViewportRenderCommandContainer>? Cases { get; set; }
        public ViewportRenderCommandContainer? DefaultCase { get; set; }

        //private int _lastSwitch = 0;

        protected override void Execute()
        {
            int sw = SwitchEvaluator?.Invoke() ?? -1;
            //if (NeedsCollecVisible)
            //    sw = _lastSwitch;
            //else
            //{
            //    if (SwitchEvaluator is null)
            //        return;

            //    sw = SwitchEvaluator();
            //}

            if (Cases?.TryGetValue(sw, out var commands) ?? false)
                commands.Execute();
            else
                DefaultCase?.Execute();
        }

        //public override bool NeedsCollecVisible
        //    => (Cases?.Values.Any(c => c.CollecVisibleCommands.Count > 0) ?? false) || DefaultCase?.CollecVisibleCommands.Count > 0;

        //public override void CollectVisible()
        //{
        //    if (SwitchEvaluator is not null && (Cases?.TryGetValue(_lastSwitch = SwitchEvaluator.Invoke(), out var commands) ?? false))
        //        commands.Execute();
        //    else
        //        DefaultCase?.Execute();
        //}

        //public override void SwapBuffers()
        //{
        //    if (SwitchEvaluator is not null && (Cases?.TryGetValue(_lastSwitch, out var commands) ?? false))
        //        commands.SwapBuffers();
        //    else
        //        DefaultCase?.SwapBuffers();
        //}
    }
}
