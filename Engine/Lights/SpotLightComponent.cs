using Extensions;
using System.ComponentModel;
using XREngine.Data;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Components.Lights
{
    public class SpotLightComponent : LightComponent
    {
        private float _outerCutoff, _innerCutoff, _distance;

        [Category("Spot Light Component")]
        public float Distance
        {
            get => _distance;
            set
            {
                _distance = value;
                UpdateCones();
            }
        }
        [Category("Spot Light Component")]
        public float Exponent { get; set; }
        [Category("Spot Light Component")]
        public float Brightness { get; set; }
        [Category("Spot Light Component")]
        public float OuterCutoffAngleDegrees
        {
            get => XMath.RadToDeg((float)Math.Acos(_outerCutoff));
            set => SetCutoffs(InnerCutoffAngleDegrees, value, true);
        }
        [Category("Spot Light Component")]
        public float InnerCutoffAngleDegrees
        {
            get => XMath.RadToDeg((float)Math.Acos(_innerCutoff));
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
            
            float radOuter = XMath.DegToRad(outerDegrees);
            _outerCutoff = XMath.Cosf(radOuter);
            OuterCone.Radius = XMath.Tanf(radOuter) * _distance;

            float radInner = XMath.DegToRad(innerDegrees);
            _innerCutoff = XMath.Cosf(radInner);
            InnerCone.Radius = XMath.Tanf(radInner) * _distance;

            //if (ShadowCamera != null)
            //    ((PerspectiveCamera)ShadowCamera).VerticalFieldOfView = Math.Max(outerDegrees, innerDegrees) * 2.0f;

            UpdateCones();
        }
        private void UpdateCones()
        {
            Vec3 dir = Transform.GetForwardVector();
            Vec3 coneOrigin = Translation + dir * (_distance * 0.5f);

            OuterCone.UpAxis = -dir;
            OuterCone.Center.Value = coneOrigin;
            OuterCone.Height = _distance;
            OuterCone.Radius = (float)Math.Tan(XMath.DegToRad(OuterCutoffAngleDegrees)) * _distance;

            InnerCone.UpAxis = -dir;
            InnerCone.Center.Value = coneOrigin;
            InnerCone.Height = _distance;
            InnerCone.Radius = (float)Math.Tan(XMath.DegToRad(InnerCutoffAngleDegrees)) * _distance;

            //if (ShadowCamera != null)
            //    ShadowCamera.FarZ = _distance;

            Vec3 lightMeshOrigin = dir * (_distance * 0.5f);
            Matrix t = lightMeshOrigin.AsTranslationMatrix();
            Matrix s = Matrix4.CreateScale(OuterCone.Radius, OuterCone.Radius, OuterCone.Height);
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
            OuterCone = new Cone(Vec3.Zero, Vec3.UnitZ, (float)Math.Tan(TMath.DegToRad(outerCutoffDeg)) * distance, distance);
            InnerCone = new Cone(Vec3.Zero, Vec3.UnitZ, (float)Math.Tan(TMath.DegToRad(innerCutoffDeg)) * distance, distance);

            _outerCutoff = (float)Math.Cos(TMath.DegToRad(outerCutoffDeg));
            _innerCutoff = (float)Math.Cos(TMath.DegToRad(innerCutoffDeg));
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
            OuterCone = new Cone(Vec3.Zero, Vec3.UnitZ, (float)Math.Tan(TMath.DegToRad(outerCutoffDeg)) * distance, distance);
            InnerCone = new Cone(Vec3.Zero, Vec3.UnitZ, (float)Math.Tan(TMath.DegToRad(innerCutoffDeg)) * distance, distance);

            _outerCutoff = (float)Math.Cos(TMath.DegToRad(outerCutoffDeg));
            _innerCutoff = (float)Math.Cos(TMath.DegToRad(innerCutoffDeg));
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

            program.Uniform($"{targetStructName}Direction", Transform.GetForwardVector());
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
