using XREngine.Data.Rendering;
using XREngine.Rendering;

namespace XREngine.Components.Lights
{
    public class SceneCaptureComponent : XRComponent
    {
        private uint _colorResolution;
        private uint _depthResolution;
        private bool _captureDepthCubeMap = false;

        public uint ColorResolution
        {
            get => _colorResolution;
            set => SetField(ref _colorResolution, value);
        }
        public uint DepthResolution
        {
            get => _depthResolution;
            set => SetField(ref _depthResolution, value);
        }
        public bool CaptureDepthCubeMap
        {
            get => _captureDepthCubeMap;
            set => SetField(ref _captureDepthCubeMap, value);
        }

        protected XRViewport? XPosVP => Viewports[0];
        protected XRViewport? XNegVP => Viewports[1];
        protected XRViewport? YPosVP => Viewports[2];
        protected XRViewport? YNegVP => Viewports[3];
        protected XRViewport? ZPosVP => Viewports[4];
        protected XRViewport? ZNegVP => Viewports[5];

        public XRViewport?[] Viewports { get; } = new XRViewport?[6];

        protected XRTextureCube? _environmentTextureCubemap;
        protected XRTextureCube? _environmentDepthTextureCubemap;
        protected XRRenderBuffer? _tempDepth;
        private XRCubeFrameBuffer? _renderFBO;

        public XRTextureCube? EnvironmentTextureCubemap
        {
            get => _environmentTextureCubemap;
            set => SetField(ref _environmentTextureCubemap, value);
        }

        public XRTextureCube? EnvironmentDepthTextureCubemap => _environmentDepthTextureCubemap;
        protected XRCubeFrameBuffer? RenderFBO => _renderFBO;

        public void SetCaptureResolution(uint colorResolution, bool captureDepth = false, uint depthResolution = 1u)
        {
            ColorResolution = colorResolution;
            DepthResolution = depthResolution;
            CaptureDepthCubeMap = captureDepth;
            InitializeForCapture();
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            InitializeForCapture();
        }

        protected virtual void InitializeForCapture()
        {
            _environmentTextureCubemap?.Destroy();
            _environmentTextureCubemap = new XRTextureCube(ColorResolution, EPixelInternalFormat.Rgb8, EPixelFormat.Rgb, EPixelType.UnsignedByte, false)
            {
                MinFilter = ETexMinFilter.NearestMipmapLinear,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
                Resizable = false,
                SizedInternalFormat = ESizedInternalFormat.Rgb8,
                Name = "SceneCaptureEnvColor",
                AutoGenerateMipmaps = false,
                //FrameBufferAttachment = EFrameBufferAttachment.ColorAttachment0,
            };
            //_envTex.Generate();

            if (CaptureDepthCubeMap)
            {
                _environmentDepthTextureCubemap?.Destroy();
                _environmentDepthTextureCubemap = new XRTextureCube(DepthResolution, EPixelInternalFormat.DepthComponent24, EPixelFormat.DepthStencil, EPixelType.UnsignedInt248, false)
                {
                    MinFilter = ETexMinFilter.NearestMipmapLinear,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    WWrap = ETexWrapMode.ClampToEdge,
                    Resizable = false,
                    SizedInternalFormat = ESizedInternalFormat.Depth24Stencil8,
                    Name = "SceneCaptureEnvDepth",
                    AutoGenerateMipmaps = false,
                    //FrameBufferAttachment = EFrameBufferAttachment.DepthAttachment,
                };
                //_envDepthTex.Generate();
            }
            else
            {
                _tempDepth = new XRRenderBuffer(DepthResolution, DepthResolution, ERenderBufferStorage.Depth24Stencil8);
                //_tempDepth.Generate();
                //_tempDepth.Allocate();
            }

            _renderFBO = new XRCubeFrameBuffer(null, Transform, 0.1f, 10000.0f, true);
            //_renderFBO.Generate();

            int i = 0;
            foreach (XRCamera cam in _renderFBO)
            {
                Viewports[i++] = new XRViewport(null, ColorResolution, ColorResolution)
                {
                    WorldInstanceOverride = World,
                    Camera = cam,
                    RenderPipeline = new DefaultRenderPipeline(),
                    SetRenderPipelineFromCamera = false,
                    AutomaticallyCollectVisible = false,
                    AutomaticallySwapBuffers = false,
                    AllowUIRender = false,
                };
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
        public virtual void Render()
        {
            if (World is null)
                return;

            if (RenderFBO is null)
                SetCaptureResolution(1024);

            IFrameBufferAttachement depthAttachment;
            int[] depthLayers;
            if (CaptureDepthCubeMap)
            {
                depthAttachment = _environmentDepthTextureCubemap!;
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
                    (_environmentTextureCubemap!, EFrameBufferAttachment.ColorAttachment0, 0, i),
                    (depthAttachment, EFrameBufferAttachment.DepthStencilAttachment, 0, depthLayers[i]));

                Viewports[i]!.Render(RenderFBO, null, null, false, null);
            }

            if (_environmentTextureCubemap is not null)
            {
                _environmentTextureCubemap.Bind();
                _environmentTextureCubemap.GenerateMipmapsGPU();
            }
        }

        public virtual void CollectVisible()
        {
            if (_renderFBO is null)
                SetCaptureResolution(1024);

            for (int i = 0; i < 6; ++i)
                Viewports[i]!.CollectVisible(null, null, false);
        }

        public virtual void SwapBuffers()
        {
            if (_renderFBO is null)
                SetCaptureResolution(1024);

            for (int i = 0; i < 6; ++i)
                Viewports[i]!.SwapBuffers(false);
        }
    }
}
