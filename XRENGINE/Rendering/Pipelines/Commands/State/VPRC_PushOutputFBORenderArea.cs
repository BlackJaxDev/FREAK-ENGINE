using XREngine.Data.Geometry;

namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_PushOutputFBORenderArea(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_PopRenderArea>(pipeline)
    {
        protected override void Execute()
        {
            var fbo = Pipeline.RenderStatus.OutputFBO;
            if (fbo is null)
            {
                PopCommand.ShouldExecute = false;
                return;
            }

            Engine.Rendering.State.RenderAreas.Push(new BoundingRectangle(0, 0, (int)fbo.Width, (int)fbo.Height));
        }
    }
}
