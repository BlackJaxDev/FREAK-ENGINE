namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_PushViewportRenderArea(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_PopRenderArea>(pipeline)
    {
        /// <summary>
        /// If true, the internal resolution region of the viewport is used.
        /// Otherwise, the region of the viewport is used.
        /// Defaults to true.
        /// </summary>
        public bool UseInternalResolution { get; set; } = true;

        protected override void Execute()
        {
            var vp = Pipeline.RenderStatus.Viewport;
            if (vp is null)
            {
                PopCommand.ShouldExecute = false;
                return;
            }

            Engine.Rendering.State.RenderAreas.Push(
                UseInternalResolution
                    ? vp.InternalResolutionRegion
                    : vp.Region);
        }
    }
}
