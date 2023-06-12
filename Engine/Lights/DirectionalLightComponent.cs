using System.ComponentModel;
using XREngine.Data.Transforms.Vectors;
using XREngine.Scenes;

namespace XREngine.Components.Lights
{
    public class DirectionalLightComponent : LightComponent
    {
        private Vec3 _scale = Vec3.One;

        public DirectionalLightComponent(SceneNode node) : base(node)
        {
        }

        [Category("Transform")]
        public Vec3 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                //if (ShadowCamera != null)
                //{
                //    ShadowCamera.Resize(Scale.X, Scale.Y);
                //    ShadowCamera.FarZ = Scale.Z;
                //    ShadowCamera.Translation.Value = WorldPoint;
                //    ShadowCamera.TranslateRelative(0.0f, 0.0f, Scale.Z * 0.5f);
                //}
                //LightMatrix = WorldMatrix.Value * _scale.AsScaleMatrix();
            }
        }

       // public DirectionalLightComponent()
       //     : this(new ColorF3(1.0f, 1.0f, 1.0f), 1.0f, 0.0f) { }
       // public DirectionalLightComponent(ColorF3 color, float diffuseIntensity)
       //     : base(color, diffuseIntensity)
       // {
       //     Transform.Rotation.Value *= Quat.Euler(-90.0f, 0.0f, 0.0f); //Face down
       //     ShadowExponent = 1.0f;
       // }
       // public DirectionalLightComponent(ColorF3 color, float diffuseIntensity, EventQuat rotation)
       //     : base(color, diffuseIntensity)
       // {
       //     Transform.Rotation = rotation;
       //     ShadowExponent = 1.0f;
       // }
       // public DirectionalLightComponent(ColorF3 color, float diffuseIntensity, Quat rotation)
       //: base(color, diffuseIntensity)
       // {
       //     Transform.Rotation.Value = rotation;
       //     ShadowExponent = 1.0f;
       // }
       // public DirectionalLightComponent(ColorF3 color, float diffuseIntensity, Rotator rotation)
       //     : base(color, diffuseIntensity)
       // {
       //     Transform.Rotation.Value = rotation.ToQuaternion();
       //     ShadowExponent = 1.0f;
       // }
       // public DirectionalLightComponent(ColorF3 color, float diffuseIntensity, Vec3 direction)
       //     : base(color, diffuseIntensity)
       // {
       //     Transform.Rotation.Value = direction.LookatAngles().ToQuaternion();
       //     ShadowExponent = 1.0f;
       // }
       // public DirectionalLightComponent(ColorF3 color, float diffuseIntensity, Vec3 eulerAngles, ERotationOrder eulerOrder)
       //     : base(color, diffuseIntensity)
       // {
       //     Transform.Rotation.Value = Quat.Euler(eulerAngles, eulerOrder);
       //     ShadowExponent = 1.0f;
       // }

       // protected override void OnWorldTransformChanged(bool recalcChildWorldTransformsNow = true)
       // {
       //     if (ShadowCamera != null)
       //     {
       //         ShadowCamera.Translation.Value = WorldPoint;
       //         ShadowCamera.TranslateRelative(0.0f, 0.0f, Scale.Z * 0.5f);
       //     }
            
       //     LightMatrix = WorldMatrix.Value * Scale.AsScaleMatrix();

       //     base.OnWorldTransformChanged(recalcChildWorldTransformsNow);
       // }
       // protected override void OnSpawned()
       // {
       //     IScene3D s3d = OwningScene3D;
       //     if (s3d != null)
       //     {
       //         if (Type == ELightType.Dynamic)
       //         {
       //             s3d.Lights.Add(this);

       //             if (ShadowMap is null)
       //                 SetShadowMapResolution(_region.Width, _region.Height);

       //             ShadowCamera.Translation.Value = WorldPoint;
       //             ShadowCamera.TranslateRelative(0.0f, 0.0f, Scale.Z * 0.5f);
       //         }
       //         ShadowCamera.RenderInfo.LinkScene(ShadowCamera, s3d);
       //     }
       //     base.OnSpawned();
       // }
       // protected override void OnDespawned()
       // {
       //     IScene3D s3D = OwningScene3D;
       //     if (s3D != null)
       //     {
       //         if (Type == ELightType.Dynamic)
       //             s3D.Lights.Remove(this);
                
       //         ShadowCamera.RenderInfo.UnlinkScene();
       //     }
       //     base.OnDespawned();
       // }
       // public override void SetUniforms(RenderProgram program, string targetStructName = null)
       // {
       //     targetStructName = $"{targetStructName ?? Uniform.LightsStructName}.";

       //     program.Uniform($"{targetStructName}Direction", Transform.GetForwardVector());
       //     program.Uniform($"{targetStructName}Color", _color.Raw);
       //     program.Uniform($"{targetStructName}DiffuseIntensity", _diffuseIntensity);
       //     program.Uniform($"{targetStructName}WorldToLightSpaceProjMatrix", ShadowCamera.WorldToCameraProjSpaceMatrix);

       //     program.Sampler("ShadowMap", ShadowMap.Material.Textures[1], 4);
       // }
       // public override void SetShadowMapResolution(int width, int height)
       // {
       //     base.SetShadowMapResolution(width, height);
       //     if (ShadowCamera is null)
       //     {
       //         ShadowCamera = new OrthographicCamera(Vec3.One, Vec3.Zero, Rotator.GetZero(), Vec2.Half, 0.0f, Scale.Z);
       //         ShadowCamera.Rotation.SyncFrom(Transform.Rotation);
       //         ShadowCamera.Resize(Scale.X, Scale.Y);
       //     }
       // }
       // public override TMaterial GetShadowMapMaterial(int width, int height, EDepthPrecision precision = EDepthPrecision.Flt32)
       // {
       //     TexRef2D[] refs = new TexRef2D[]
       //     {
       //         new TexRef2D("DirShadowDepth", width, height,
       //         GetShadowDepthMapFormat(precision), EPixelFormat.DepthComponent, EPixelType.Float)
       //         {
       //             MinFilter = ETexMinFilter.Nearest,
       //             MagFilter = ETexMagFilter.Nearest,
       //             UWrap = ETexWrapMode.ClampToEdge,
       //             VWrap = ETexWrapMode.ClampToEdge,
       //             FrameBufferAttachment = EFramebufferAttachment.DepthAttachment,
       //         },
       //         new TexRef2D("DirShadowColor", width, height,
       //         EPixelInternalFormat.R32f, EPixelFormat.Red, EPixelType.Float)
       //         {
       //             MinFilter = ETexMinFilter.Nearest,
       //             MagFilter = ETexMagFilter.Nearest,
       //             UWrap = ETexWrapMode.ClampToEdge,
       //             VWrap = ETexWrapMode.ClampToEdge,
       //             FrameBufferAttachment = EFramebufferAttachment.ColorAttachment0,
       //             SamplerName = "ShadowMap"
       //         },
       //     };

       //     //This material is used for rendering to the framebuffer.
       //     TMaterial mat = new TMaterial("DirLightShadowMat", new ShaderVar[0], refs, 
       //         new GLSLScript(EGLSLType.Fragment, ShaderHelpers.Frag_DepthOutput));

       //     //No culling so if a light exists inside of a mesh it will shadow everything.
       //     mat.RenderParams.CullMode = ECulling.None;

       //     return mat;
       // }

#if EDITOR
        protected override string PreviewIconName => "PointLightIcon.png";
        protected internal override void OnSelectedChanged(bool selected)
        {
            ShadowCamera.RenderInfo.IsVisible = selected;
        }
#endif
    }
}
