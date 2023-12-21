using Extensions;
using System.ComponentModel;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;
using static XREngine.Data.XMath;

namespace XREngine.Components.Lights
{
    public class SpotLightComponent : LightComponent
    {
        private float _outerCutoff, _innerCutoff, _distance;

        public float Distance
        {
            get => _distance;
            set
            {
                _distance = value;
                UpdateCones();
            }
        }
        public float Exponent { get; set; }
        public float Brightness { get; set; }
        public float OuterCutoffAngleDegrees
        {
            get => RadToDeg((float)Math.Acos(_outerCutoff));
            set => SetCutoffs(InnerCutoffAngleDegrees, value, true);
        }
        public float InnerCutoffAngleDegrees
        {
            get => RadToDeg((float)Math.Acos(_innerCutoff));
            set => SetCutoffs(value, OuterCutoffAngleDegrees, false);
        }

        private void SetCutoffs(float innerDegrees, float outerDegrees, bool settingOuter)
        {
            innerDegrees = innerDegrees.Clamp(0.0f, 90.0f);
            outerDegrees = outerDegrees.Clamp(0.0f, 90.0f);

            if (outerDegrees < innerDegrees)
            {
                float bias = 0.0001f;
                if (settingOuter)
                    innerDegrees = outerDegrees - bias;
                else
                    outerDegrees = innerDegrees + bias;
            }
            
            float radOuter = DegToRad(outerDegrees);
            _outerCutoff = MathF.Cos(radOuter);
            OuterCone.Radius = MathF.Tan(radOuter) * _distance;

            float radInner = DegToRad(innerDegrees);
            _innerCutoff = MathF.Cos(radInner);
            InnerCone.Radius = MathF.Tan(radInner) * _distance;

            //if (ShadowCamera != null)
            //    ((PerspectiveCamera)ShadowCamera).VerticalFieldOfView = Math.Max(outerDegrees, innerDegrees) * 2.0f;

            UpdateCones();
        }
        private void UpdateCones()
        {
            Vec3 dir = Transform.WorldMatrix.Forward;
            Vec3 coneOrigin = Translation + dir * (_distance * 0.5f);

            OuterCone.UpAxis = -dir;
            OuterCone.Center.Value = coneOrigin;
            OuterCone.Height = _distance;
            OuterCone.Radius = (float)Math.Tan(DegToRad(OuterCutoffAngleDegrees)) * _distance;

            InnerCone.UpAxis = -dir;
            InnerCone.Center.Value = coneOrigin;
            InnerCone.Height = _distance;
            InnerCone.Radius = (float)Math.Tan(DegToRad(InnerCutoffAngleDegrees)) * _distance;

            //if (ShadowCamera != null)
            //    ShadowCamera.FarZ = _distance;

            Vec3 lightMeshOrigin = dir * (_distance * 0.5f);
            Matrix t = lightMeshOrigin.AsTranslationMatrix();
            Matrix s = Matrix.CreateScale(OuterCone.Radius, OuterCone.Radius, OuterCone.Height);
            LightMatrix = t * WorldMatrix.Value * s;
        }

        [Browsable(false)]
        [ReadOnly(true)]
        [Category("Spotlight Component")]
        public Cone OuterCone { get; }
        [Browsable(false)]
        [ReadOnly(true)]
        [Category("Spotlight Component")]
        public Cone InnerCone { get; }
        
        public SpotLightComponent()
            : this(100.0f, new ColorF3(0.0f, 0.0f, 0.0f), 1.0f, Vec3.Down, 60.0f, 30.0f, 1.0f, 1.0f) { }

        public SpotLightComponent(
            float distance, ColorF3 color, float diffuseIntensity,
            Vec3 direction, float outerCutoffDeg, float innerCutoffDeg, float brightness, float exponent) 
            : base(color, diffuseIntensity)
        {
            OuterCone = new Cone(Vec3.Zero, Vec3.UnitZ, MathF.Tan(DegToRad(outerCutoffDeg)) * distance, distance);
            InnerCone = new Cone(Vec3.Zero, Vec3.UnitZ, MathF.Tan(DegToRad(innerCutoffDeg)) * distance, distance);

            _outerCutoff = (float)Math.Cos(DegToRad(outerCutoffDeg));
            _innerCutoff = (float)Math.Cos(DegToRad(innerCutoffDeg));
            _distance = distance;
            Brightness = brightness;
            Exponent = exponent;
            Transform.Rotation.Value = direction.LookatAngles().ToQuaternion();
        }
        public SpotLightComponent(
            float distance, ColorF3 color, float diffuseIntensity,
            Quat rotation, float outerCutoffDeg, float innerCutoffDeg, float brightness, float exponent)
            : base(color, diffuseIntensity)
        {
            OuterCone = new Cone(Vec3.Zero, Vec3.UnitZ, (float)Math.Tan(DegToRad(outerCutoffDeg)) * distance, distance);
            InnerCone = new Cone(Vec3.Zero, Vec3.UnitZ, (float)Math.Tan(DegToRad(innerCutoffDeg)) * distance, distance);

            _outerCutoff = (float)Math.Cos(DegToRad(outerCutoffDeg));
            _innerCutoff = (float)Math.Cos(DegToRad(innerCutoffDeg));
            _distance = distance;
            Brightness = brightness;
            Exponent = exponent;
            Transform.Rotation.Value = rotation;
        }

        protected override void OnWorldTransformChanged(bool recalcChildWorldTransformsNow = true)
        {
            UpdateCones();
            base.OnWorldTransformChanged(recalcChildWorldTransformsNow);
        }
        protected override void OnSpawned()
        {
            IScene3D s3d = OwningScene3D;
            if (s3d != null)
            {
                if (Type == ELightType.Dynamic)
                {
                    s3d.Lights.Add(this);

                    if (ShadowMap is null)
                        SetShadowMapResolution(_region.Width, _region.Height);

                    //ShadowCamera.LocalPoint.Raw = WorldPoint;
                    //ShadowCamera.TranslateRelative(0.0f, 0.0f, Scale.Z * 0.5f);
                }
                InnerCone.RenderInfo.LinkScene(InnerCone, s3d);
                OuterCone.RenderInfo.LinkScene(OuterCone, s3d);
            }
            base.OnSpawned();
        }
        protected override void OnDespawned()
        {
            IScene3D s3d = OwningScene3D;
            if (s3d != null)
            {
                if (Type == ELightType.Dynamic)
                    s3d.Lights.Remove(this);
                
                InnerCone.RenderInfo.UnlinkScene();
                OuterCone.RenderInfo.UnlinkScene();
            }
            base.OnDespawned();
        }
        public override void SetUniforms(RenderProgram program, string targetStructName = null)
        {
            targetStructName = $"{targetStructName ?? Uniform.LightsStructName}.";

            program.Uniform($"{targetStructName}Direction", Transform.WorldMatrix.Forward);
            program.Uniform($"{targetStructName}OuterCutoff", _outerCutoff);
            program.Uniform($"{targetStructName}InnerCutoff", _innerCutoff);
            program.Uniform($"{targetStructName}Position", WorldPoint);
            program.Uniform($"{targetStructName}Radius", _distance);
            program.Uniform($"{targetStructName}Brightness", Brightness);
            program.Uniform($"{targetStructName}Exponent", Exponent);
            program.Uniform($"{targetStructName}Color", _color.Raw);
            program.Uniform($"{targetStructName}DiffuseIntensity", _diffuseIntensity);
            program.Uniform($"{targetStructName}WorldToLightSpaceProjMatrix", ShadowCamera.WorldToCameraProjSpaceMatrix);

            program.Sampler("ShadowMap", ShadowMap.Material.Textures[1], 4);
        }
        public override void SetShadowMapResolution(int width, int height)
        {
            base.SetShadowMapResolution(width, height);
            if (ShadowCamera is null)
            {
                float cutoff = Math.Max(OuterCutoffAngleDegrees, InnerCutoffAngleDegrees);
                ShadowCamera = new PerspectiveCamera(1.0f, _distance, cutoff * 2.0f, 1.0f);
                ShadowCamera.Rotation.SyncFrom(Transform.Rotation);
                ShadowCamera.Translation.Sync(Transform.Translation);
            }
        }
        
        public override TMaterial GetShadowMapMaterial(int width, int height, EDepthPrecision precision = EDepthPrecision.Flt32)
        {
            TexRef2D[] refs = new TexRef2D[]
            {
                new TexRef2D("SpotShadowDepth", width, height, 
                GetShadowDepthMapFormat(precision), EPixelFormat.DepthComponent, EPixelType.Float)
                {
                    MinFilter = ETexMinFilter.Nearest,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    FrameBufferAttachment = EFramebufferAttachment.DepthAttachment,
                },
                new TexRef2D("SpotShadowColor", width, height, 
                EPixelInternalFormat.R32f, EPixelFormat.Red, EPixelType.Float)
                {
                    MinFilter = ETexMinFilter.Nearest,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    FrameBufferAttachment = EFramebufferAttachment.ColorAttachment0,
                    SamplerName = "ShadowMap"
                },
            };

            //This material is used for rendering to the framebuffer.
            TMaterial mat = new TMaterial("SpotLightShadowMat", new ShaderVar[0], refs, 
                new GLSLScript(EGLSLType.Fragment, ShaderHelpers.Frag_DepthOutput));

            //No culling so if a light exists inside of a mesh it will shadow everything.
            mat.RenderParams.CullMode = ECulling.None;

            return mat;
        }

#if EDITOR
        protected override string PreviewIconName => "SpotLightIcon.png";
        protected internal override void OnSelectedChanged(bool selected)
        {
            OuterCone.RenderInfo.IsVisible = selected;
            InnerCone.RenderInfo.IsVisible = selected;
            base.OnSelectedChanged(selected);
        }
#endif
    }
}
