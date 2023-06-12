using XREngine.Scenes;

namespace XREngine.Components.Lights
{
    public class PointLightComponent : LightComponent
    {
        //[Category("Point Light Component")]
        //public float Radius
        //{
        //    get => _influenceVolume.Radius;
        //    set
        //    {
        //        _influenceVolume.Radius = value;
        //        //foreach (PerspectiveCamera cam in ShadowCameras)
        //        //    cam.FarZ = value;
        //        //LightMatrix = WorldMatrix.Value * Matrix4.CreateScale(Radius);
        //    }
        //}
        //[Category("Point Light Component")]
        //public int ShadowMapResolution
        //{
        //    get => _region.Width;
        //    set => SetShadowMapResolution(value, value);
        //}
        //[Category("Point Light Component")]
        //public float Brightness { get; set; } = 1.0f;

        //[Browsable(false)]
        //public PerspectiveCamera[] ShadowCameras { get; }

        //private Sphere _influenceVolume;

        //public PointLightComponent() 
        //    : this(100.0f, 1.0f, new ColorF3(1.0f, 1.0f, 1.0f), 1.0f) { }
        //public PointLightComponent(float radius, float brightness, ColorF3 color, float diffuseIntensity) 
        //    : base(color, diffuseIntensity)
        //{
        //    _influenceVolume = new Sphere(radius);
        //    Brightness = brightness;

        //    ShadowCameras = new PerspectiveCamera[6];
        //    Rotator[] rotations = new Rotator[]
        //    {
        //        new Rotator(  0.0f, -90.0f, 180.0f), //+X
        //        new Rotator(  0.0f,  90.0f, 180.0f), //-X
        //        new Rotator( 90.0f,   0.0f,   0.0f), //+Y
        //        new Rotator(-90.0f,   0.0f,   0.0f), //-Y
        //        new Rotator(  0.0f, 180.0f, 180.0f), //+Z
        //        new Rotator(  0.0f,   0.0f, 180.0f), //-Z
        //    };
        //    ShadowCameras.FillWith(i => new PerspectiveCamera(Vec3.Zero, rotations[i], 0.01f, radius, 90.0f, 1.0f));
        //    ShadowExponentBase = 1.0f;
        //    ShadowExponent = 2.5f;
        //    ShadowMinBias = 0.05f;
        //    ShadowMaxBias = 10.0f;
        //}
        //protected override void OnWorldTransformChanged(bool recalcChildWorldTransformsNow = true)
        //{
        //    _influenceVolume.SetTransformMatrix(WorldMatrix.Value);
        //    foreach (PerspectiveCamera cam in ShadowCameras)
        //        cam.Translation.Value = WorldMatrix.Value.Translation;
        //    LightMatrix = WorldMatrix.Value * Matrix4.CreateScale(Radius);
        //    base.OnWorldTransformChanged(recalcChildWorldTransformsNow);
        //}

        //protected override void OnSpawned()
        //{
        //    IScene3D s3d = OwningScene3D;
        //    if (s3d != null)
        //    {
        //        if (Type == ELightType.Dynamic)
        //        {
        //            s3d.Lights.Add(this);

        //            if (ShadowMap is null)
        //                SetShadowMapResolution(1024, 1024);
        //        }
        //        _influenceVolume.RenderInfo.LinkScene(_influenceVolume, s3d);
        //    }
        //    base.OnSpawned();
        //}
        //protected override void OnDespawned()
        //{
        //    IScene3D s3d = OwningScene3D;
        //    if (s3d != null)
        //    {
        //        if (Type == ELightType.Dynamic)
        //            s3d.Lights.Remove(this);

        //        _influenceVolume.RenderInfo.UnlinkScene();
        //    }
        //    base.OnDespawned();
        //}

        //protected override IVolume GetShadowVolume() => _influenceVolume;

        ///// <summary>
        ///// This is to set uniforms in the GBuffer lighting shader 
        ///// or in a forward shader that requests lighting uniforms.
        ///// </summary>
        //public override void SetUniforms(RenderProgram program, string targetStructName = null)
        //{
        //    targetStructName = $"{(targetStructName ?? Uniform.LightsStructName)}.";

        //    program.Uniform($"{targetStructName}Color", _color.Raw);
        //    program.Uniform($"{targetStructName}DiffuseIntensity", _diffuseIntensity);
        //    program.Uniform($"{targetStructName}Position", _influenceVolume.Center);
        //    program.Uniform($"{targetStructName}Radius", Radius);
        //    program.Uniform($"{targetStructName}Brightness", Brightness);

        //    ShadowMap.Material.Textures[1].SampleIn(program, 4);
        //}
        //public override void SetShadowMapResolution(int width, int height)
        //{
        //    bool wasNull = ShadowMap is null;
        //    int res = Math.Max(width, height);
        //    base.SetShadowMapResolution(res, res);
        //    if (wasNull)
        //        ShadowMap.Material.SettingUniforms += SetShadowDepthUniforms;
        //}
        ///// <summary>
        ///// This is to set special uniforms each time any mesh is rendered 
        ///// with the shadow depth shader during the shadow pass.
        ///// </summary>
        //private void SetShadowDepthUniforms(RenderProgram program)
        //{
        //    program.Uniform("FarPlaneDist", Radius);
        //    program.Uniform("LightPos", _influenceVolume.Center);
        //    for (int i = 0; i < ShadowCameras.Length; ++i)
        //        program.Uniform($"ShadowMatrices[{i}]", ShadowCameras[i].WorldToCameraProjSpaceMatrix);
        //}
        //public override TMaterial GetShadowMapMaterial(int width, int height, EDepthPrecision precision = EDepthPrecision.Flt32)
        //{
        //    int cubeExtent = Math.Max(width, height);
        //    TexRefCube[] refs = new TexRefCube[]
        //    {
        //        new TexRefCube("PointDepth", cubeExtent, GetShadowDepthMapFormat(precision), EPixelFormat.DepthComponent, EPixelType.Float)
        //        {
        //            MinFilter = ETexMinFilter.Nearest,
        //            MagFilter = ETexMagFilter.Nearest,
        //            UWrap = ETexWrapMode.ClampToEdge,
        //            VWrap = ETexWrapMode.ClampToEdge,
        //            WWrap = ETexWrapMode.ClampToEdge,
        //            FrameBufferAttachment = EFramebufferAttachment.DepthAttachment,
        //        },
        //        new TexRefCube("PointColor", cubeExtent, EPixelInternalFormat.R32f, EPixelFormat.Red, EPixelType.Float)
        //        {
        //            MinFilter = ETexMinFilter.Nearest,
        //            MagFilter = ETexMagFilter.Nearest,
        //            UWrap = ETexWrapMode.ClampToEdge,
        //            VWrap = ETexWrapMode.ClampToEdge,
        //            WWrap = ETexWrapMode.ClampToEdge,
        //            FrameBufferAttachment = EFramebufferAttachment.ColorAttachment0,
        //            SamplerName = "ShadowMap"
        //        },
        //    };

        //    //This material is used for rendering to the framebuffer.
        //    GLSLScript fragShader = Engine.Files.Shader("PointLightShadowDepth.fs", EGLSLType.Fragment);
        //    GLSLScript geomShader = Engine.Files.Shader("PointLightShadowDepth.gs", EGLSLType.Geometry);
        //    TMaterial mat = new TMaterial("PointLightShadowMat", new ShaderVar[0], refs, fragShader, geomShader);

        //    //No culling so if a light exists inside of a mesh it will shadow everything.
        //    mat.RenderParams.CullMode = ECulling.None;

        //    return mat;
        //}
#if EDITOR
        protected override string PreviewIconName => "PointLightIcon.png";
        protected internal override void OnSelectedChanged(bool selected)
            => _influenceVolume.RenderInfo.IsVisible = selected;
#endif
        public PointLightComponent(SceneNode node) : base(node)
        {
        }
    }
}
