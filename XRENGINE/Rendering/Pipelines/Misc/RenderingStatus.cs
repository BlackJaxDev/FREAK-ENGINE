using XREngine.Scene;

namespace XREngine.Rendering;

public sealed partial class XRRenderPipelineInstance
{
    public class RenderingStatus
    {
        /// <summary>
        /// The viewport being rendered to.
        /// May be null if rendering directly to a framebuffer.
        /// </summary>
        public XRViewport? Viewport { get; private set; }
        /// <summary>
        /// The scene being rendered.
        /// </summary>
        public VisualScene? Scene { get; private set; }
        /// <summary>
        /// The camera this render pipeline is rendering the scene through.
        /// </summary>
        public XRCamera? Camera { get; private set; }
        /// <summary>
        /// The output FBO target for the render pass.
        /// May be null if rendering to the screen.
        /// </summary>
        public XRFrameBuffer? OutputFBO { get; private set; }
        /// <summary>
        /// If this pipeline is rendering a shadow pass.
        /// Shadow passes do not need to execute all rendering commands.
        /// </summary>
        public bool ShadowPass { get; private set; }

        public void Set(XRViewport? viewport, VisualScene? scene, XRCamera? camera, XRFrameBuffer? target, bool shadowPass)
        {
            Viewport = viewport;
            Scene = scene;
            Camera = camera;
            OutputFBO = target;
            ShadowPass = shadowPass;
        }

        public void Clear()
        {
            Viewport = null;
            Scene = null;
            Camera = null;
            OutputFBO = null;
            ShadowPass = false;
        }
    }
}