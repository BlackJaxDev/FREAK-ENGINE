using XREngine.Data.Geometry;

namespace XREngine.Rendering.Pipelines.Commands
{
    public class VPRC_PushOutputFBORenderArea(ViewportRenderCommandContainer pipeline) : ViewportStateRenderCommand<VPRC_PopRenderArea>(pipeline)
    {
        protected override void Execute()
        {
            var fbo = Pipeline.State.OutputFBO;
            if (fbo is null)
            {
                PopCommand.ShouldExecute = false;
                return;
            }

            Pipeline.State.PushRenderArea((int)fbo.Width, (int)fbo.Height);
        }
    }
}
