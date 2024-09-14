﻿namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_Switch(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_PopRenderArea>(pipeline)
    {
        public required Func<int> SwitchEvaluator { get; set; }
        public required Dictionary<int, ViewportRenderCommandContainer> Cases { get; set; }
        public ViewportRenderCommandContainer? DefaultCase { get; set; }

        protected override void Execute()
        {
            if (Cases.TryGetValue(SwitchEvaluator(), out var commands))
                commands.Execute();
            else
                DefaultCase?.Execute();
        }
    }
}