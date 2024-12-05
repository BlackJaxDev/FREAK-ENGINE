namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_PushViewportRenderArea : ViewportStateRenderCommand<VPRC_PopRenderArea>
    {
        /// <summary>
        /// If true, the internal resolution region of the viewport is used.
        /// Otherwise, the region of the viewport is used.
        /// Defaults to true.
        /// </summary>
        public bool UseInternalResolution { get; set; } = true;

        protected override void Execute()
        {
            var vp = Pipeline.RenderState.WindowViewport;
            if (vp is null)
            {
                PopCommand.ShouldExecute = false;
                return;
            }

            Pipeline.RenderState.PushRenderArea(
                UseInternalResolution
                    ? vp.InternalResolutionRegion
                    : vp.Region);
        }
    }
}
