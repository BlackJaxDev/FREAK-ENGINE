using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Scene;

namespace XREngine.Components.Lights
{
    public class SceneCaptureComponent : XRComponent
    {
        private uint _colorResolution;
        private uint _depthResolution;
        private bool _captureDepthCubeMap = false;

        protected uint ColorResolution
        {
            get => _colorResolution;
            set => SetField(ref _colorResolution, value);
        }
        protected uint DepthResolution
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

        public void SetCaptureResolution(uint colorResolution, bool captureDepth = false, uint depthResolution = 1u)
        {
            ColorResolution = colorResolution;
            DepthResolution = depthResolution;
            CaptureDepthCubeMap = captureDepth;
            InitializeForCapture();
        }

        protected virtual void InitializeForCapture()
        {
            _viewport = new XRViewport(null, ColorResolution, ColorResolution) { WorldInstanceOverride = World };

            _envTex = new XRTextureCube(ColorResolution, EPixelInternalFormat.Rgb8, EPixelFormat.Rgb, EPixelType.UnsignedByte)
            {
                MinFilter = ETexMinFilter.NearestMipmapLinear,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
                Resizable = false,
                SizedInternalFormat = ESizedInternalFormat.Rgb8,
                SamplerName = "SceneTex",
                Name = "SceneCaptureEnvColor",
                AutoGenerateMipmaps = false,
            };

            if (CaptureDepthCubeMap)
            {
                _envDepthTex = new XRTextureCube(DepthResolution, EPixelInternalFormat.DepthComponent24, EPixelFormat.DepthComponent, EPixelType.UnsignedInt248)
                {
                    MinFilter = ETexMinFilter.NearestMipmapLinear,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    WWrap = ETexWrapMode.ClampToEdge,
                    Resizable = false,
                    SizedInternalFormat = ESizedInternalFormat.DepthComponent24,
                    SamplerName = "SceneDepthTex",
                    Name = "SceneCaptureEnvDepth",
                    AutoGenerateMipmaps = false,
                };
            }
            else
            {
                _tempDepth = new XRRenderBuffer(ColorResolution, ColorResolution, ERenderBufferStorage.DepthComponent24);
                _tempDepth.Allocate();
            }

            _renderFBO = new XRCubeFrameBuffer(null, Transform, 0.1f, 10000.0f, true);

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
            if (RenderFBO is null)
                SetCaptureResolution(1024);

            if (World?.VisualScene is not VisualScene3D scene3D)
                return;

            scene3D.Lights.RenderShadowMaps(true);

            IFrameBufferAttachement depthAttachment;
            int[] depthLayers;
            if (CaptureDepthCubeMap)
            {
                depthAttachment = _envDepthTex!;
                depthLayers = [0, 1, 2, 3, 4, 5];
            }
            else
            {
                depthAttachment = _tempDepth!;
                depthLayers = [0, 0, 0, 0, 0, 0];
            }

            for (int i = 0; i < 6; ++i)
            {
                RenderFBO!.SetRenderTargets(
                    (_envTex!, EFrameBufferAttachment.ColorAttachment0, 0, i),
                    (depthAttachment, EFrameBufferAttachment.DepthAttachment, 0, depthLayers[i]));

                _viewport!.Camera = RenderFBO.Cameras[i];
                _viewport.Render(RenderFBO, World.VisualScene);
            }

            if (_envTex is not null)
            {
                _envTex.Bind();
                _envTex.GenerateMipmapsGPU();
            }
        }
    }
}
