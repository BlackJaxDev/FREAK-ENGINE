using XREngine.Data.Rendering;

namespace XREngine.Rendering.UI
{
    /// <summary>
    /// Houses a viewport that renders a scene from a designated camera.
    /// </summary>
    public class UIViewportComponent : UIMaterialComponent
    {
        public event DelSetUniforms? SettingUniforms;

        private readonly XRMaterialFrameBuffer _fbo;

        //These bools are to prevent infinite pre-rendering recursion
        private bool _updating = false;
        private bool _swapping = false;
        private bool _rendering = false;

        public UIViewportComponent() : base(GetViewportMaterial())
        {
            _fbo = new XRMaterialFrameBuffer(Material);

            if (RenderCommand3D.Mesh is not null)
                RenderCommand3D.Mesh.SettingUniforms += SetUniforms;

            Engine.Time.Timer.SwapBuffers += SwapBuffers;
            //Engine.Time.Timer.UpdateFrame += Update;
            Engine.Time.Timer.RenderFrame += Render;
        }

        private static XRMaterial GetViewportMaterial()
            => new([XRTexture2D.CreateFrameBufferTexture(1u, 1u,
                    EPixelInternalFormat.Rgba16f,
                    EPixelFormat.Rgba,
                    EPixelType.HalfFloat,
                    EFrameBufferAttachment.ColorAttachment0)],
                XRShader.EngineShader(Path.Combine("Common", "UnlitTexturedForward.fs"), EShaderType.Fragment));

        private void SetUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
            => SettingUniforms?.Invoke(materialProgram);

        public XRViewport Viewport { get; private set; } = new XRViewport(null, 1, 1);
        //protected override void OnResizeLayout(BoundingRectangle parentRegion)
        //{
        //    base.OnResizeLayout(parentRegion);

        //    int
        //        w = (int)ActualWidth.ClampMin(1.0f),
        //        h = (int)ActualHeight.ClampMin(1.0f);

        //    Viewport.Resize(w, h);
        //    _fbo.Resize(w, h);
        //}

        public void Update(XRCamera camera)
        {
            if (!IsActive || _updating)
                return;

            _updating = true;
            //Viewport.PreRenderUpdate();
            _updating = false;
        }
        public void SwapBuffers()
        {
            if (!IsActive || _swapping)
                return;

            _swapping = true;
            //Viewport.PreRenderSwap();
            _swapping = false;
        }
        public void Render()
        {
            if (!IsActive || _rendering)
                return;

            _rendering = true;
            //Viewport.Render(_fbo);
            _rendering = false;
        }
    }
}
