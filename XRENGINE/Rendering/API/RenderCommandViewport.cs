//using System.Numerics;
//using XREngine.Data.Rendering;

//namespace XREngine.Rendering
//{
//    public class RenderCommandViewport : RenderCommandMesh2D
//    {
//        public XRViewport Viewport { get; set; }
//        public XRMaterialFrameBuffer Framebuffer { get; set; }

//        //public RenderCommandViewport() : base() { }
//        public RenderCommandViewport(ERenderPass renderPass) : base(renderPass) { }
//        public RenderCommandViewport(
//            ERenderPass renderPass,
//            XRViewport viewport,
//            XRMesh quad,
//            XRMaterialFrameBuffer viewportFBO,
//            Matrix4x4 worldMatrix,
//            int zIndex)
//            : base(renderPass, quad, worldMatrix, zIndex, null)
//        {
//            Viewport = viewport;
//            Framebuffer = viewportFBO;
//        }

//        public override void Render(bool shadowPass)
//        {
//            //TODO: viewport renders all viewed items to the framebuffer,
//            //But this method is called within the parent's rendering to its framebuffer.
//            Viewport.Render(Framebuffer);
//            XRFrameBuffer.CurrentlyBound?.Bind(EFramebufferTarget.DrawFramebuffer);
//            base.Render(shadowPass);
//        }
//    }
//}
