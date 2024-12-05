namespace XREngine.Rendering.Pipelines.Commands
{
    /// <summary>
    /// Render's the camera's screen space UI to the current viewport.
    /// </summary>
    public class VPRC_RenderUI : ViewportRenderCommand
    {
        public string UserInterfaceFBOName { get; set; } = string.Empty;
        //public override bool NeedsCollecVisible => true;

        //public override void CollectVisible()
        //{
        //    var ui = Pipeline.RenderState.UserInterface;
        //    var vp = Pipeline.RenderState.RenderingViewport;
        //    if (ui is null || vp is null)
        //        return;

        //    var fbo = Pipeline.GetFBO<XRQuadFrameBuffer>(UserInterfaceFBOName);
        //    if (fbo is not null)
        //        ui?.CollectRenderedItems(vp);
        //}

        //public override void SwapBuffers()
        //{
        //    var ui = Pipeline.RenderState.UserInterface;
        //    var vp = Pipeline.RenderState.RenderingViewport;
        //    if (ui is null || vp is null)
        //        return;

        //    var fbo = Pipeline.GetFBO<XRQuadFrameBuffer>(UserInterfaceFBOName);
        //    if (fbo is not null)
        //        ui?.SwapBuffers();
        //}

        protected override void Execute()
        {
            var ui = Pipeline.RenderState.UserInterface;
            if (ui is null)
                return;
            
            var fbo = Pipeline.GetFBO<XRQuadFrameBuffer>(UserInterfaceFBOName);
            if (fbo is not null)
                ui?.Render(fbo);
        }
    }
}