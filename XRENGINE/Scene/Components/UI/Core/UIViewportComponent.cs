using XREngine.Data.Rendering;
using XREngine.Scene;

namespace XREngine.Rendering.UI
{
    /// <summary>
    /// Houses a viewport that renders a scene from a designated camera.
    /// </summary>
    public class UIViewportComponent : UIMaterialComponent, IRenderable, IPreRendered
    {
        public event DelSetUniforms SettingUniforms;

        //private MaterialFrameBuffer _fbo;

        //These bools are to prevent infinite pre-rendering recursion
        private bool _updating = false;
        private bool _swapping = false;
        private bool _rendering = false;

        public UIViewportComponent() : base(GetViewporXRMaterial())
        {
            //_fbo = new MaterialFrameBuffer(Material);
            //RenderCommand.Mesh.SettingUniforms += SetUniforms;
        }

        private static XRMaterial GetViewporXRMaterial()
            => new(
                [
                    XRTexture2D.CreateFrameBufferTexture(1u, 1u,
                        EPixelInternalFormat.Rgba16f,
                        EPixelFormat.Rgba, EPixelType.HalfFloat,
                        EFrameBufferAttachment.ColorAttachment0),
                ],
                XRShader.EngineShader(Path.Combine("Common", "UnlitTexturedForward.fs"), EShaderType.Fragment));

        private void SetUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
            => SettingUniforms?.Invoke(materialProgram);

        //public bool PreRenderEnabled => IsVisible && ViewportCamera?.OwningComponent?.OwningScene != null;

        public XRViewport Viewport { get; private set; } = new XRViewport(1, 1);
        public bool PreRenderEnabled { get; }

        //protected override void OnResizeLayout(BoundingRectangleF parentRegion)
        //{
        //    base.OnResizeLayout(parentRegion);

        //    int
        //        w = (int)ActualWidth.ClampMin(1.0f),
        //        h = (int)ActualHeight.ClampMin(1.0f);

        //    Viewport.Resize(w, h);
        //    _fbo.Resize(w, h);
        //}

        public void PreRenderUpdate(XRCamera camera)
        {
            if (!IsVisible || _updating)
                return;

            _updating = true;
            //Viewport.PreRenderUpdate();
            _updating = false;
        }
        public void PreRenderSwap()
        {
            if (!IsVisible || _swapping)
                return;

            _swapping = true;
            //Viewport.PreRenderSwap();
            _swapping = false;
        }
        public void PreRender(XRViewport viewport, XRCamera camera)
        {
            if (!IsVisible || _rendering)
                return;

            _rendering = true;
            //Viewport.Render(_fbo);
            _rendering = false;
        }
    }
}
