using Extensions;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;
using XREngine.Scene.Transforms;
using static XREngine.Data.Core.XRMath;

namespace XREngine.Components.Lights
{
    public class SpotLightComponent(float distance, float outerCutoffDeg, float innerCutoffDeg, float brightness, float exponent) : LightComponent()
    {
        private float 
            _outerCutoff = (float)Math.Cos(DegToRad(outerCutoffDeg)),
            _innerCutoff = (float)Math.Cos(DegToRad(innerCutoffDeg)),
            _distance = distance;

        private Cone _outerCone = new(
            Vector3.Zero,
            Globals.Backward,
            MathF.Tan(DegToRad(outerCutoffDeg)) * distance,
            distance);

        private Cone _innerCone = new(
            Vector3.Zero,
            Globals.Backward,
            MathF.Tan(DegToRad(innerCutoffDeg)) * distance,
            distance);

        private float _exponent = exponent;
        private float _brightness = brightness;

        public float Distance
        {
            get => _distance;
            set
            {
                SetField(ref _distance, value);
                UpdateCones();
            }
        }

        public float Exponent
        {
            get => _exponent;
            set => SetField(ref _exponent, value);
        }

        public float Brightness
        {
            get => _brightness;
            set => SetField(ref _brightness, value);
        }

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

            SetField(ref _outerCutoff, MathF.Cos(DegToRad(outerDegrees)));
            SetField(ref _innerCutoff, MathF.Cos(DegToRad(innerDegrees)));

            if (ShadowCamera != null && ShadowCamera.Parameters is XRPerspectiveCameraParameters p)
                p.VerticalFieldOfView = Math.Max(outerDegrees, innerDegrees) * 2.0f;

            UpdateCones();
            RecalcLightMatrix();
        }
        private void UpdateCones()
        {
            Vector3 dir = Transform.WorldForward;
            Vector3 coneOrigin = Transform.WorldTranslation + dir * (_distance * 0.5f);

            SetField(ref _outerCone, new(coneOrigin, -dir, _distance, MathF.Tan(DegToRad(OuterCutoffAngleDegrees)) * _distance));
            SetField(ref _innerCone, new(coneOrigin, -dir, _distance, MathF.Tan(DegToRad(InnerCutoffAngleDegrees)) * _distance));

            if (ShadowCamera != null)
                ShadowCamera.FarZ = _distance;

            Vector3 lightMeshOrigin = dir * (_distance * 0.5f);
            Matrix4x4 t = Matrix4x4.CreateTranslation(lightMeshOrigin);
            Matrix4x4 s = Matrix4x4.CreateScale(OuterCone.Radius, OuterCone.Radius, OuterCone.Height);
            LightMatrix = t * Transform.WorldMatrix * s;
        }

        public Cone OuterCone => _outerCone;
        public Cone InnerCone => _innerCone;

        public SpotLightComponent()
            : this(100.0f, 60.0f, 30.0f, 1.0f, 1.0f) { }

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            base.OnTransformWorldMatrixChanged(transform);
            UpdateCones();
        }

        protected internal override void OnComponentActivated()
        {
            if (World is null)
                return;
            
            if (World.VisualScene is VisualScene3D scene)
                scene.Lights.SpotLights.Add(this);

            if (ShadowMap is null)
                SetShadowMapResolution((uint)_shadowMapRenderRegion.Width, (uint)_shadowMapRenderRegion.Height);
        }

        protected internal override void OnComponentDeactivated()
        {
            if (World?.VisualScene is VisualScene3D scene)
                scene.Lights.SpotLights.Remove(this);

            base.OnComponentDeactivated();
        }

        public override void SetUniforms(XRRenderProgram program, string? targetStructName = null)
        {
            base.SetUniforms(program, targetStructName);

            targetStructName = $"{targetStructName ?? Engine.Rendering.Constants.LightsStructName}.";

            program.Uniform($"{targetStructName}Direction", Transform.WorldForward);
            program.Uniform($"{targetStructName}OuterCutoff", _outerCutoff);
            program.Uniform($"{targetStructName}InnerCutoff", _innerCutoff);
            program.Uniform($"{targetStructName}Position", Transform.WorldTranslation);
            program.Uniform($"{targetStructName}Radius", _distance);
            program.Uniform($"{targetStructName}Brightness", Brightness);
            program.Uniform($"{targetStructName}Exponent", Exponent);
            program.Uniform($"{targetStructName}Color", _color);
            program.Uniform($"{targetStructName}DiffuseIntensity", _diffuseIntensity);
            program.Uniform($"{targetStructName}WorldToLightProjMatrix", ShadowCamera?.ProjectionMatrix ?? Matrix4x4.Identity);
            program.Uniform($"{targetStructName}WorldToLightInvViewMatrix", ShadowCamera?.Transform.WorldMatrix ?? Matrix4x4.Identity);

            if (ShadowMap is null)
                return;
            
            var tex = ShadowMap.Material.Textures[1];
            if (tex is not null)
                program.Sampler("ShadowMap", tex, 4);
        }

        protected override XRCamera GetShadowCamera()
        {
            return new XRCamera(
                Transform,
                new XRPerspectiveCameraParameters(
                    Math.Max(OuterCutoffAngleDegrees, InnerCutoffAngleDegrees) * 2.0f,
                    1.0f,
                    1.0f,
                    _distance));
        }

        public override void SetShadowMapResolution(uint width, uint height)
        {
            base.SetShadowMapResolution(width, height);

            if (ShadowCamera?.Parameters is XRPerspectiveCameraParameters p)
                p.AspectRatio = width / height;
        }

        public override XRMaterial GetShadowMapMaterial(uint width, uint height, EDepthPrecision precision = EDepthPrecision.Flt32)
        {
            XRTexture2D[] textures =
            [
                new XRTexture2D(width, height, GetShadowDepthMapFormat(precision), EPixelFormat.Red, EPixelType.UnsignedByte)
                {
                    MinFilter = ETexMinFilter.Nearest,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    FrameBufferAttachment = EFrameBufferAttachment.DepthAttachment,
                },
                new XRTexture2D(width, height, EPixelInternalFormat.R32f, EPixelFormat.Red, EPixelType.Float)
                {
                    MinFilter = ETexMinFilter.Nearest,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    FrameBufferAttachment = EFrameBufferAttachment.ColorAttachment0,
                    SamplerName = "ShadowMap"
                },
            ];

            //This material is used for rendering to the framebuffer.
            XRMaterial mat = new([], textures, new XRShader(EShaderType.Fragment, ShaderHelper.Frag_DepthOutput));

            //No culling so if a light exists inside of a mesh it will shadow everything.
            mat.RenderOptions.CullMode = ECulling.None;

            return mat;
        }

        protected override void RecalcLightMatrix()
        {
            Vector3 lightMeshOrigin = Transform.WorldForward * (_distance * 0.5f);
            Matrix4x4 t = Matrix4x4.CreateTranslation(lightMeshOrigin);
            Matrix4x4 s = Matrix4x4.CreateScale(OuterCone.Radius, OuterCone.Radius, OuterCone.Height);
            LightMatrix = t * Transform.WorldMatrix * s;
        }
    }
}
