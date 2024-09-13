using MIConvexHull;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Rendering;
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

        public LightProbeComponent() : base()
            => RenderedObjects = [RenderInfo = RenderInfo3D.New(this, _rc = new RenderCommandMesh3D(0))];

        private readonly RenderCommandMesh3D _rc;
        public RenderInfo3D RenderInfo { get; }
        public RenderInfo[] RenderedObjects { get; }

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

        private bool _showPrefilterTexture = false;
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
            if (_rc != null && IrradianceSphere != null)
            {
                IrradianceSphere.Parameter<ShaderVector3>(0)!.Value = Transform.WorldTranslation;
                _rc.WorldMatrix = Transform.WorldMatrix;
            }
            //if (IsSpawned && _envTex != null)
            //{
            //    Capture();
            //    GenerateIrradianceMap();
            //    GeneratePrefilterMap();
            //}
            base.OnTransformWorldMatrixChanged(transform);
        }
        protected override void InitializeForCapture()
        {
            base.InitializeForCapture();

            //Irradiance texture doesn't need to be very high quality, 
            //linear filtering on low resolution will do fine
            IrradianceTexture = new XRTextureCube(64, EPixelInternalFormat.Rgb8, EPixelFormat.Rgb, EPixelType.UnsignedByte)
            {
                MinFilter = ETexMinFilter.Linear,
                MagFilter = ETexMagFilter.Linear,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
            };

            PrefilterTex = new XRTextureCube(ColorResolution, EPixelInternalFormat.Rgb16f, EPixelFormat.Rgb, EPixelType.HalfFloat)
            {
                MinFilter = ETexMinFilter.LinearMipmapLinear,
                MagFilter = ETexMagFilter.Linear,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
                WWrap = ETexWrapMode.ClampToEdge,
            };

            ShaderVar[] prefilterVars =
            [
                new ShaderFloat(0.0f, "Roughness"),
                new ShaderInt(ColorResolution, "CubemapDim"),
            ];

            XRShader irrShader = ShaderHelper.LoadShader("Common/Scene/IrradianceConvolution.fs", EShaderType.Fragment);
            XRShader prefShader = ShaderHelper.LoadShader("Common/Scene/Prefilter.fs", EShaderType.Fragment);

            RenderingParameters r = new();
            r.DepthTest.Enabled = ERenderParamUsage.Disabled;
            XRMaterial irrMat = new([], new XRTexture[] { _envTex! }, irrShader);
            XRMaterial prefMat = new(prefilterVars, [_envTex!], prefShader);

            _irradianceFBO = new XRCubeFrameBuffer(irrMat, null, 0.0f, 1.0f, false);
            _prefilterFBO = new XRCubeFrameBuffer(prefMat, null, 0.0f, 1.0f, false);
        }

        public void FullCapture(int colorResolution, bool captureDepth, int depthResolution)
        {
            if (_hasCaptured)
                return;
            
            _hasCaptured = true;
            SetCaptureResolution(colorResolution, captureDepth, depthResolution);
            CreatePreviewSphere();
            //}
            //if (DateTime.Now - _lastUpdateTime > TimeSpan.FromSeconds(1.0))
            //{
            _lastUpdateTime = DateTime.Now;
            Capture();
            GenerateIrradianceMap();
            GeneratePrefilterMap();
        }

        public void GenerateIrradianceMap()
        {
            if (IrradianceTexture is null)
                return;

            int res = IrradianceTexture.CubeExtent;
            using (Engine.Rendering.State.PushRenderArea(new BoundingRectangle(IVector2.Zero, new IVector2(res, res))))
            {
                for (int i = 0; i < 6; ++i)
                {
                    _irradianceFBO!.SetRenderTargets((IrradianceTexture, EFrameBufferAttachment.ColorAttachment0, 0, i));
                    using (_irradianceFBO.BindForWriting())
                    {
                        Engine.Rendering.State.Clear(EFrameBufferTextureType.Color);
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

                using (Engine.Rendering.State.PushRenderArea(new BoundingRectangle(IVector2.Zero, new IVector2(mipWidth, mipHeight))))
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        _prefilterFBO.SetRenderTargets((PrefilterTex, EFrameBufferAttachment.ColorAttachment0, mip, i));
                        using (_prefilterFBO.BindForWriting())
                        {
                            Engine.Rendering.State.Clear(EFrameBufferTextureType.Color);
                            _prefilterFBO.RenderFullscreen(ECubemapFace.PosX + i);
                        }
                    }
                }
            }
        }

        private void CreatePreviewSphere()
        {
            XRShader shader = XRShader.EngineShader("CubeMapSphereMesh.fs", EShaderType.Fragment);
            XRMaterial mat = new([new ShaderVector3(Vector3.Zero, "SphereCenter")], [_showPrefilterTexture ? PrefilterTex! : IrradianceTexture!], shader);
            IrradianceSphere = new XRMeshRenderer(XRMesh.Shapes.SolidSphere(Vector3.Zero, 1.0f, 20u), mat);
            _rc.Mesh = IrradianceSphere;
            IrradianceSphere.Parameter<ShaderVector3>(0)!.Value = Transform.WorldTranslation;
            _rc.WorldMatrix = Transform.WorldMatrix;
        }

        protected internal override void Start()
        {
            base.Start();

            if (!_hasCaptured)
                FullCapture(
                    Engine.Rendering.Settings.LightProbeDefaultColorResolution, 
                    Engine.Rendering.Settings.ShouldLightProbesCaptureDepth,
                    Engine.Rendering.Settings.LightProbeDefaultDepthResolution);

            if (World?.VisualScene is VisualScene3D scene3D)
                scene3D.Lights.LightProbes.Add(this);
            else
                Debug.LogWarning("LightProbeComponent must be in a VisualScene3D to function properly.");
        }

        protected internal override void Stop()
        {
            base.Stop();
            if (World?.VisualScene is VisualScene3D scene3D)
                scene3D.Lights.LightProbes.Remove(this);
        }
    }
}
