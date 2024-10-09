using MIConvexHull;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Lights
{
    public class LightProbeComponent : SceneCaptureComponent, IRenderable, IVertex
    {
        /// <summary>
        /// Position for delaunay triangulation
        /// </summary>
        double[] IVertex.Position => [Transform.WorldTranslation.X, Transform.WorldTranslation.Y, Transform.WorldTranslation.Z];

        private void EnsureCaptured(bool shadowPass)
        {
            if (shadowPass)
                return;

            //if (!_hasCaptured)
            //{
            //    FullCapture(
            //        ColorResolution,
            //        CaptureDepthCubeMap,
            //        DepthResolution);
            //}
            //else
            if (RealTime && (RealTimeUpdateInterval is null || DateTime.Now - _lastUpdateTime >= RealTimeUpdateInterval))
            {
                _lastUpdateTime = DateTime.Now;
                Capture();
            }
        }

        public LightProbeComponent() : base()
        {
            RenderedObjects = 
            [
                VisualRenderInfo = RenderInfo3D.New(this, _visualRC = new RenderCommandMesh3D((int)EDefaultRenderPass.OpaqueForward)),
                PreRenderInfo = RenderInfo3D.New(this, _preRenderRC = new RenderCommandMethod3D((int)EDefaultRenderPass.PreRender, EnsureCaptured))
            ];
        }

        private readonly RenderCommandMesh3D _visualRC;
        private readonly RenderCommandMethod3D _preRenderRC;

        public RenderInfo3D VisualRenderInfo { get; }
        public RenderInfo3D PreRenderInfo { get; }
        public RenderInfo[] RenderedObjects { get; }

        private bool _realTime = false;
        /// <summary>
        /// If true, the light probe will update in real time.
        /// </summary>
        public bool RealTime
        {
            get => _realTime;
            set => SetField(ref _realTime, value);
        }

        private TimeSpan? _realTimeUpdateInterval = TimeSpan.FromMilliseconds(1000.0f);
        public TimeSpan? RealTimeUpdateInterval 
        {
            get => _realTimeUpdateInterval;
            set => SetField(ref _realTimeUpdateInterval, value);
        }

        private XRCubeFrameBuffer? _irradianceFBO;
        private XRCubeFrameBuffer? _prefilterFBO;
        private bool _hasCaptured = false;
        private DateTime _lastUpdateTime = DateTime.Now;
        private XRTextureCube? _irradianceTexture;
        private XRTextureCube? _prefilterTexture;
        private XRMeshRenderer? _irradianceSphere;

        public XRTextureCube? IrradianceTexture
        {
            get => _irradianceTexture;
            private set => SetField(ref _irradianceTexture, value);
        }
        public XRTextureCube? PrefilterTex
        {
            get => _prefilterTexture;
            private set => SetField(ref _prefilterTexture, value);
        }
        public XRMeshRenderer? IrradianceSphere
        {
            get => _irradianceSphere;
            private set => SetField(ref _irradianceSphere, value);
        }

        private bool _showPrefilterTexture = true;
        public bool ShowPrefilterTexture
        {
            get => _showPrefilterTexture;
            set
            {
                SetField(ref _showPrefilterTexture, value);
                SetDisplayedPreviewTexture();
            }
        }

        private void SetDisplayedPreviewTexture()
        {
            if (IrradianceSphere?.Material is null)
                return;
            
            var tex = _showPrefilterTexture ? PrefilterTex : IrradianceTexture;
            if (tex != null)
                IrradianceSphere.Material.Textures[0] = tex;
        }

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            if (_visualRC != null && IrradianceSphere != null)
            {
                IrradianceSphere.Parameter<ShaderVector3>(0)!.Value = Transform.WorldTranslation;
                _visualRC.WorldMatrix = Transform.WorldMatrix;
            }
            base.OnTransformWorldMatrixChanged(transform);
        }
        protected override void InitializeForCapture()
        {
            base.InitializeForCapture();

            //Irradiance texture doesn't need to be very high quality, 
            //linear filtering on low resolution will do fine
            IrradianceTexture = new XRTextureCube(64, EPixelInternalFormat.Rgb8, EPixelFormat.Rgb, EPixelType.UnsignedByte, false)
            {
                MinFilter = ETexMinFilter.Linear,
                MagFilter = ETexMagFilter.Linear,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
                Resizable = false,
                SizedInternalFormat = ESizedInternalFormat.Rgb8,
                AutoGenerateMipmaps = true,
            };

            PrefilterTex = new XRTextureCube(ColorResolution, EPixelInternalFormat.Rgb16f, EPixelFormat.Rgb, EPixelType.HalfFloat, false)
            {
                MinFilter = ETexMinFilter.LinearMipmapLinear,
                MagFilter = ETexMagFilter.Linear,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
                Resizable = false,
                SizedInternalFormat = ESizedInternalFormat.Rgb16f,
                AutoGenerateMipmaps = true,
            };

            ShaderVar[] prefilterVars =
            [
                new ShaderFloat(0.0f, "Roughness"),
                new ShaderInt((int)ColorResolution, "CubemapDim"),
            ];

            XRShader irrShader = ShaderHelper.LoadEngineShader("Scene3D\\IrradianceConvolution.fs", EShaderType.Fragment);
            XRShader prefShader = ShaderHelper.LoadEngineShader("Scene3D\\Prefilter.fs", EShaderType.Fragment);

            RenderingParameters r = new();
            r.DepthTest.Enabled = ERenderParamUsage.Disabled;
            r.CullMode = ECulling.None;
            XRTexture[] texArray = [_envTex!];
            XRMaterial irrMat = new([], texArray, irrShader);
            XRMaterial prefMat = new(prefilterVars, texArray, prefShader);

            _irradianceFBO = new XRCubeFrameBuffer(irrMat, null, 0.0f, 1.0f, false);
            _prefilterFBO = new XRCubeFrameBuffer(prefMat, null, 0.0f, 1.0f, false);

            CachePreviewSphere();
        }

        public void FullCapture(uint colorResolution, bool captureDepth, uint depthResolution)
        {
            if (_hasCaptured)
                return;
            
            _hasCaptured = true;
            SetCaptureResolution(colorResolution, captureDepth, depthResolution);
            Capture();
        }

        public override void Capture()
        {
            base.Capture();
            GenerateIrradianceMap();
            GeneratePrefilterMap();
        }

        public void GenerateIrradianceMap()
        {
            if (IrradianceTexture is null)
                return;

            uint res = IrradianceTexture.Extent;
            using (Engine.Rendering.State.PipelineState?.PushRenderArea(new BoundingRectangle(IVector2.Zero, new IVector2((int)res, (int)res))))
            {
                for (int i = 0; i < 6; ++i)
                {
                    _irradianceFBO!.SetRenderTargets((IrradianceTexture, EFrameBufferAttachment.ColorAttachment0, 0, i));
                    using (_irradianceFBO.BindForWriting())
                    {
                        Engine.Rendering.State.Clear(true, false, false);
                        Engine.Rendering.State.EnableDepthTest(false);
                        _irradianceFBO.RenderFullscreen(ECubemapFace.PosX + i);
                    }
                }
            }
        }

        public void GeneratePrefilterMap()
        {
            if (PrefilterTex is null)
                return;

            PrefilterTex.Bind();
            PrefilterTex.GenerateMipmapsGPU();

            int maxMipLevels = 5;
            int res = _prefilterFBO!.Material!.Parameter<ShaderInt>(1)!.Value;
            for (int mip = 0; mip < maxMipLevels; ++mip)
            {
                int mipWidth = (int)(res * Math.Pow(0.5, mip));
                int mipHeight = (int)(res * Math.Pow(0.5, mip));
                float roughness = (float)mip / (maxMipLevels - 1);

                _prefilterFBO.Material.Parameter<ShaderFloat>(0)!.Value = roughness;

                AbstractRenderer.Current?.SetRenderArea(new BoundingRectangle(IVector2.Zero, new IVector2(mipWidth, mipHeight)));
                for (int i = 0; i < 6; ++i)
                {
                    _prefilterFBO.SetRenderTargets((PrefilterTex, EFrameBufferAttachment.ColorAttachment0, mip, i));
                    using (_prefilterFBO.BindForWriting())
                    {
                        Engine.Rendering.State.Clear(true, false, false);
                        _prefilterFBO.RenderFullscreen(ECubemapFace.PosX + i);
                    }
                }
            }
        }

        private void CachePreviewSphere()
        {
            if (IrradianceSphere is not null)
                return;

            XRShader shader = XRShader.EngineShader("CubeMapSphereMesh.fs", EShaderType.Fragment);
            XRMaterial mat = new([new ShaderVector3(Vector3.Zero, "SphereCenter")], [_showPrefilterTexture ? PrefilterTex! : IrradianceTexture!], shader)
            {
                RenderPass = (int)EDefaultRenderPass.OpaqueForward
            };
            IrradianceSphere = new XRMeshRenderer(XRMesh.Shapes.SolidSphere(Vector3.Zero, 1.0f, 20u), mat);
            _visualRC.Mesh = IrradianceSphere;
            _visualRC.WorldMatrix = Transform.WorldMatrix;
            //VisualRenderInfo.CullingVolume = new Sphere(Vector3.Zero, 1.0f);
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            if (World?.VisualScene is VisualScene3D scene3D)
                scene3D.Lights.LightProbes.Add(this);
            else
                Debug.LogWarning("LightProbeComponent must be in a VisualScene3D to function properly.");
        }

        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();
            if (World?.VisualScene is VisualScene3D scene3D)
                scene3D.Lights.LightProbes.Remove(this);
        }
    }
}
