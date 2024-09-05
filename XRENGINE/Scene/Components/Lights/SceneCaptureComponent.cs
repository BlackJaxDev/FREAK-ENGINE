using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Scene;

namespace XREngine.Components.Lights
{
    public class SceneCaptureComponent : XRComponent
    {
        private int _colorResolution;
        private int _depthResolution;
        private bool _captureDepthCubeMap = true;

        protected int ColorResolution
        {
            get => _colorResolution;
            set => SetField(ref _colorResolution, value);
        }
        protected int DepthResolution
        {
            get => _depthResolution;
            set => SetField(ref _depthResolution, value);
        }
        public bool CaptureDepthCubeMap
        {
            get => _captureDepthCubeMap;
            set => SetField(ref _captureDepthCubeMap, value);
        }

        protected XRViewport? _viewport;
        protected XRTextureCube? _envTex;
        protected XRTextureCube? _envDepthTex;
        protected XRRenderBuffer? _tempDepth;
        private XRCubeFrameBuffer? _renderFBO;

        public XRTextureCube? ResultTexture => _envTex;
        public XRTextureCube? ResultDepthTexture => _envDepthTex;
        protected XRCubeFrameBuffer? RenderFBO => _renderFBO;

        public void SetCaptureResolution(int colorResolution, bool captureDepth = false, int depthResolution = 1)
        {
            ColorResolution = colorResolution;
            DepthResolution = depthResolution;
            _captureDepthCubeMap = captureDepth;
            InitializeForCapture();
        }

        protected virtual void InitializeForCapture()
        {
            _viewport = new XRViewport(ColorResolution, ColorResolution);

            _envTex = new XRTextureCube(ColorResolution, EPixelInternalFormat.Rgb8, EPixelFormat.Rgb, EPixelType.UnsignedByte)
            {
                MinFilter = ETexMinFilter.NearestMipmapLinear,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
                SamplerName = "SceneTex"
            };

            if (CaptureDepthCubeMap)
                _envDepthTex = new XRTextureCube(DepthResolution, EPixelInternalFormat.Rgb16f, EPixelFormat.Rgb, EPixelType.HalfFloat)
                {
                    MinFilter = ETexMinFilter.NearestMipmapLinear,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    WWrap = ETexWrapMode.ClampToEdge,
                    SamplerName = "SceneDepthTex"
                };

            _tempDepth = new XRRenderBuffer
            {
                Storage = ERenderBufferStorage.DepthComponent32f,
                Width = ColorResolution,
                Height = ColorResolution
            };

            _renderFBO = new XRCubeFrameBuffer(null, null, 0.1f, 10000.0f, true);

            foreach (XRCamera cam in _renderFBO)
            {
                cam.PostProcessing = new PostProcessingSettings();
                cam.PostProcessing.ColorGrading.AutoExposure = false;
                cam.PostProcessing.ColorGrading.Exposure = 1.0f;
            }
        }
        //protected override void OnWorldTransformChanged()
        //{
        //    base.OnWorldTransformChanged();
        //    //RenderFBO?.SetTransform(WorldPoint);
        //}
        /// <summary>
        /// Renders the scene to the ResultTexture cubemap.
        /// </summary>
        public void Capture()
        {
            //_cubeTex = new TexRefCube("", 512, new CubeMipmap(Engine.LoadEngineTexture2D("skybox.png")));

            if (RenderFBO is null)
                SetCaptureResolution(512);

            if (World?.VisualScene is not VisualScene3D scene3D)
                return;

            var lights = scene3D.Lights;
            lights.CollectShadowMaps();
            lights.SwapBuffers();
            lights.RenderShadowMaps();

            int depthLayer;
            IFrameBufferAttachement depthAttachment;

            for (int i = 0; i < 6; ++i)
            {
                if (CaptureDepthCubeMap)
                {
                    depthLayer = i;
                    depthAttachment = _envDepthTex!;
                }
                else
                {
                    depthLayer = 0;
                    depthAttachment = _tempDepth!;
                }

                RenderFBO!.SetRenderTargets(
                    (_envTex!, EFrameBufferAttachment.ColorAttachment0, 0, i),
                    (depthAttachment, EFrameBufferAttachment.DepthAttachment, 0, depthLayer));

                _viewport!.Camera = RenderFBO.Cameras[i];
                _viewport.RenderPipeline.Render(scene3D, _viewport.Camera, _viewport, RenderFBO);
            }

            if (_envTex is not null)
            {
                _envTex.Bind();
                _envTex.GenerateMipmapsGPU();
            }
        }
    }
}
