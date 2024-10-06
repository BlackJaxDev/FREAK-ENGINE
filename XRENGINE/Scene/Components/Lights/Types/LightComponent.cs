using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene;

namespace XREngine.Components.Lights
{
    public abstract class LightComponent : XRComponent, IRenderable
    {
        protected ColorF3 _color = new(1.0f, 1.0f, 1.0f);
        protected float _diffuseIntensity = 1.0f;

        protected int _lightIndex = -1;
        private XRMaterialFrameBuffer? _shadowMap;

        protected BoundingRectangle _shadowMapRenderRegion = new(1024, 1024);
        private ELightType _type = ELightType.Dynamic;
        private bool _castsShadows = true;
        private Matrix4x4 _lightMatrix = Matrix4x4.Identity;

        private readonly NearToFarRenderCommandSorter _nearToFarSorter = new();
        private readonly FarToNearRenderCommandSorter _farToNearSorter = new();

        private float _shadowMaxBias = 0.1f;
        private float _shadowMinBias = 0.00001f;
        private float _shadowExponent = 1.0f;
        private float _shadowExponentBase = 0.04f;

        public LightComponent() : base()
        {
            //This can have a limited number of render passes compared to a normal, non-shadow pass render
            //No sorting is needed either
            RenderInfo = RenderInfo3D.New(this);
            RenderInfo.VisibleInLightingProbes = false;
            RenderedObjects = [RenderInfo];
            ShadowRenderPipeline = new ShadowRenderPipeline();
        }

        public Matrix4x4 LightMatrix
        {
            get => _lightMatrix;
            protected set => SetField(ref _lightMatrix, value);
        }

        protected abstract void RecalcLightMatrix();

        public XRMaterialFrameBuffer? ShadowMap
        {
            get => _shadowMap;
            protected set => SetField(ref _shadowMap, value);
        }

        private XRCamera? _shadowCamera;
        public XRCamera? ShadowCamera => _shadowCamera ??= GetShadowCamera();

        protected abstract XRCamera GetShadowCamera();

        public bool CastsShadows
        {
            get => _castsShadows;
            set => SetField(ref _castsShadows, value);
        }

        public float ShadowExponentBase 
        {
            get => _shadowExponentBase;
            set => SetField(ref _shadowExponentBase, value);
        }

        public float ShadowExponent
        {
            get => _shadowExponent;
            set => SetField(ref _shadowExponent, value);
        }

        public float ShadowMinBias
        {
            get => _shadowMinBias;
            set => SetField(ref _shadowMinBias, value);
        }

        public float ShadowMaxBias
        {
            get => _shadowMaxBias;
            set => SetField(ref _shadowMaxBias, value);
        }

        public uint ShadowMapResolutionWidth
        {
            get => (uint)_shadowMapRenderRegion.Width;
            set => SetShadowMapResolution(value, (uint)_shadowMapRenderRegion.Height);
        }

        public uint ShadowMapResolutionHeight
        {
            get => (uint)_shadowMapRenderRegion.Height;
            set => SetShadowMapResolution((uint)_shadowMapRenderRegion.Width, value);
        }

        public ColorF3 Color
        {
            get => _color;
            set => SetField(ref _color, value);
        }

        public float Intensity
        {
            get => _diffuseIntensity;
            set => SetField(ref _diffuseIntensity, value);
        }

        public ELightType Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        public RenderInfo3D RenderInfo { get; }
        public RenderInfo[] RenderedObjects { get; }

        public virtual void SetShadowMapResolution(uint width, uint height)
        {
            _shadowMapRenderRegion.Width = (int)width;
            _shadowMapRenderRegion.Height = (int)height;

            if (ShadowMap is null)
                ShadowMap = new XRMaterialFrameBuffer(GetShadowMapMaterial(width, height));
            else
                ShadowMap.Resize(width, height);
        }

        public virtual void SetUniforms(XRRenderProgram program, string? targetStructName = null)
        {
            program.Uniform(Engine.Rendering.Constants.ShadowExponentBaseUniform, ShadowExponentBase);
            program.Uniform(Engine.Rendering.Constants.ShadowExponentUniform, ShadowExponent);
            program.Uniform(Engine.Rendering.Constants.ShadowBiasMinUniform, ShadowMinBias);
            program.Uniform(Engine.Rendering.Constants.ShadowBiasMaxUniform, ShadowMaxBias);
        }

        protected virtual IVolume? GetShadowVolume()
            => ShadowCamera?.WorldFrustum();

        public abstract XRMaterial GetShadowMapMaterial(uint width, uint height, EDepthPrecision precision = EDepthPrecision.Flt32);

        private readonly XRRenderPipelineInstance _shadowRenderPipeline = new();
        public RenderPipeline? ShadowRenderPipeline
        {
            get => _shadowRenderPipeline.Pipeline;
            set => _shadowRenderPipeline.Pipeline = value;
        }

        public void CollectVisibleItems(VisualScene scene)
        {
            if (!CastsShadows || ShadowCamera is null)
                return;

            scene.CollectRenderedItems(_shadowRenderPipeline.MeshRenderCommands, GetShadowVolume(), ShadowCamera);
        }

        public void RenderShadowMap(VisualScene scene, bool collectVisibleNow = false)
        {
            if (!CastsShadows || ShadowCamera is null || ShadowMap?.Material is null)
                return;

            scene.CollectRenderedItems(_shadowRenderPipeline.MeshRenderCommands, GetShadowVolume(), ShadowCamera);
            scene.PreRender(ShadowCamera);
            scene.SwapBuffers();

            _shadowRenderPipeline.Render(scene, ShadowCamera, null, ShadowMap, null, true, ShadowMap.Material);
        }

        public static EPixelInternalFormat GetShadowDepthMapFormat(EDepthPrecision precision)
            => precision switch
            {
                EDepthPrecision.Int16 => EPixelInternalFormat.DepthComponent16,
                EDepthPrecision.Int24 => EPixelInternalFormat.DepthComponent24,
                EDepthPrecision.Int32 => EPixelInternalFormat.DepthComponent32,
                _ => EPixelInternalFormat.DepthComponent32f,
            };
    }
}
