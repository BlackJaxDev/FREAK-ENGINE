using System.ComponentModel;
using XREngine.Scenes;

namespace XREngine.Components.Lights
{
    public enum ELightType
    {
        //Movable. Always calculates light for everything per-frame.
        Dynamic,
        //Moveable. Bakes into shadow maps when not moving.
        DynamicCached,
        //Does not move. Allows baking light into shadow maps.
        Static,
    }
    public abstract class LightComponent : Component//, IEditorPreviewIconRenderable
    {
        //public LightComponent(ColorF3 color, float diffuseIntensity) : base()
        //{
        //    _color = color;
        //    _diffuseIntensity = diffuseIntensity;
        //}

        //protected EventColorF3 _color = (ColorF3)Color.White;
        protected float _diffuseIntensity = 1.0f;
        protected int _lightIndex = -1;

        protected LightComponent(SceneNode node) : base(node)
        {
        }

        //protected RenderPasses _passes = new RenderPasses();
        //protected BoundingRectangle _region = new BoundingRectangle(0, 0, 1024, 1024);

        //[Browsable(false)]
        //public Matrix4 LightMatrix { get; protected set; }
        //[Browsable(false)]
        //public MaterialFrameBuffer ShadowMap { get; protected set; }
        //[Browsable(false)]
        //public TransformableCamera ShadowCamera { get; protected set; }

        [Category("Shadow Map Settings")]
        public bool CastsShadows { get; set; } = true;
        [DisplayName("Exponent Base")]
        [Category("Shadow Map Settings")]
        public float ShadowExponentBase { get; set; } = 0.04f;
        [DisplayName("Exponent")]
        [Category("Shadow Map Settings")]
        public float ShadowExponent { get; set; } = 2.0f;
        [DisplayName("Minimum Bias")]
        [Category("Shadow Map Settings")]
        public float ShadowMinBias { get; set; } = 0.00001f;
        [DisplayName("Maximum Bias")]
        [Category("Shadow Map Settings")]
        public float ShadowMaxBias { get; set; } = 0.1f;

        //[Category("Light Component")]
        //public int ShadowMapResolutionWidth
        //{
        //    get => _region.Width;
        //    set => SetShadowMapResolution(value, _region.Height);
        //}
        //[Category("Light Component")]
        //public int ShadowMapResolutionHeight
        //{
        //    get => _region.Height;
        //    set => SetShadowMapResolution(_region.Width, value);
        //}
        //[Category("Light Component")]
        //public EventColorF3 LightColor
        //{
        //    get => _color;
        //    set => _color = value;
        //}
        [Category("Light Component")]
        public float DiffuseIntensity
        {
            get => _diffuseIntensity;
            set => _diffuseIntensity = value;
        }
        [Category("Light Component")]
        public ELightType Type { get; set; } = ELightType.Dynamic;

//        [Browsable(false)]
//        public IRenderInfo3D RenderInfo { get; } = new RenderInfo3D(true, true)
//        {
//            VisibleInIBLCapture = false,
//#if EDITOR
//            EditorVisibilityMode = EEditorVisibility.VisibleAlways
//#endif
//        };

        //internal void SetShadowUniforms(RenderProgram program)
        //{
        //    program.Uniform("ShadowBase", ShadowExponentBase);
        //    program.Uniform("ShadowMult", ShadowExponent);
        //    program.Uniform("ShadowBiasMin", ShadowMinBias);
        //    program.Uniform("ShadowBiasMax", ShadowMaxBias);
        //}

        //public virtual void SetShadowMapResolution(int width, int height)
        //{
        //    _region.Width = width;
        //    _region.Height = height;
        //    if (ShadowMap is null)
        //        ShadowMap = new MaterialFrameBuffer(GetShadowMapMaterial(width, height));
        //    else
        //        ShadowMap.Resize(width, height);
        //}
        //public abstract void SetUniforms(RenderProgram program, string targetStructName);
        //protected virtual IVolume GetShadowVolume() => ShadowCamera?.Frustum;
        //public abstract TMaterial GetShadowMapMaterial(int width, int height, EDepthPrecision precision = EDepthPrecision.Flt32);
        ////private bool _renderPass, _lastPassRendered;
        //public void CollectShadowMap(BaseScene scene)
        //{
        //    //if (!CastsShadows)
        //    //    return;

        //    //_renderPass = ShadowMap != null; //TODO: distance to camera < max fade distance
        //    IVolume volume = GetShadowVolume();
        //    scene.CollectVisible(_passes, volume, ShadowCamera, true);
        //}
        //internal void SwapBuffers()
        //{
        //    _passes.SwapBuffers();
        //    //THelpers.Swap(ref _renderPass, ref _lastPassRendered);
        //}
        //public void RenderShadowMap(BaseScene scene)
        //{
        //    //if (!_lastPassRendered)
        //    //    return;
        //    if (ShadowMap is null)
        //        return;

        //    Engine.Renderer.MeshMaterialOverride = ShadowMap.Material;
        //    Engine.Renderer.PushRenderArea(_region);

        //    //scene.PreRender(null, ShadowCamera);
        //    scene.Render(_passes, ShadowCamera, null, ShadowMap);
            
        //    Engine.Renderer.PopRenderArea();
        //    Engine.Renderer.MeshMaterialOverride = null;
        //}

        //public static EPixelInternalFormat GetShadowDepthMapFormat(EDepthPrecision precision) 
        //    => precision switch
        //    {
        //        EDepthPrecision.Int16 => EPixelInternalFormat.DepthComponent16,
        //        EDepthPrecision.Int24 => EPixelInternalFormat.DepthComponent24,
        //        EDepthPrecision.Int32 => EPixelInternalFormat.DepthComponent32,
        //        _ => EPixelInternalFormat.DepthComponent32f,
        //    };

#if EDITOR

        protected override void OnWorldTransformChanged(bool recalcChildWorldTransformsNow = true)
        {
            PreviewIconRenderCommand.Position = WorldPoint;
            base.OnWorldTransformChanged(recalcChildWorldTransformsNow);
        }

        [Category("Editor Traits")]
        public bool ScalePreviewIconByDistance { get; set; } = true;
        [Category("Editor Traits")]
        public float PreviewIconScale { get; set; } = 0.05f;

        string IEditorPreviewIconRenderable.PreviewIconName => PreviewIconName;
        protected abstract string PreviewIconName { get; }

        PreviewRenderCommand3D IEditorPreviewIconRenderable.PreviewIconRenderCommand
        {
            get => PreviewIconRenderCommand;
            set => PreviewIconRenderCommand = value;
        }
        private PreviewRenderCommand3D _previewIconRenderCommand;
        private PreviewRenderCommand3D PreviewIconRenderCommand
        {
            get => _previewIconRenderCommand ?? (_previewIconRenderCommand = CreatePreviewRenderCommand(PreviewIconName));
            set => _previewIconRenderCommand = value;
        }

        public void AddRenderables(RenderPasses passes, ICamera camera)
        {
            AddPreviewRenderCommand(PreviewIconRenderCommand, passes, camera, ScalePreviewIconByDistance, PreviewIconScale);
        }
#endif
    }
}
