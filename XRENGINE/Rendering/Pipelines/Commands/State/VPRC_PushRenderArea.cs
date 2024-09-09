using XREngine.Data.Geometry;

namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_PushRenderArea(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_PopRenderArea>(pipeline)
    {
        public required Func<BoundingRectangle> RegionGetter { get; set; }

        protected override void Execute()
            => Engine.Rendering.State.RenderAreas.Push(RegionGetter());
    }
}
