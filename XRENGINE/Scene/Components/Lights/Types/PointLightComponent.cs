using Extensions;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Transforms.Rotations;
using XREngine.Rendering;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Lights
{
    public class PointLightComponent : LightComponent
    {
        public float Radius
        {
            get => _influenceVolume.Radius;
            set
            {
                _influenceVolume.Radius = value;
                foreach (var cam in ShadowCameras)
                    cam.FarZ = value;
                RecalcLightMatrix();
            }
        }

        public uint ShadowMapResolution
        {
            get => (uint)_shadowMapRenderRegion.Width;
            set => SetShadowMapResolution(value, value);
        }

        private float _brightness = 1.0f;
        public float Brightness
        {
            get => _brightness;
            set => SetField(ref _brightness, value);
        }

        public XRCamera[] ShadowCameras { get; }

        private Sphere _influenceVolume;

        public PointLightComponent()
            : this(100.0f, 1.0f) { }
        public PointLightComponent(float radius, float brightness)
            : base()
        {
            _influenceVolume = new Sphere(Vector3.Zero, radius);
            Brightness = brightness;

            ShadowCameras = new XRCamera[6];
            Rotator[] rotations =
            [
                new(  0.0f, -90.0f, 180.0f), //+X
                new(  0.0f,  90.0f, 180.0f), //-X
                new( 90.0f,   0.0f,   0.0f), //+Y
                new(-90.0f,   0.0f,   0.0f), //-Y
                new(  0.0f, 180.0f, 180.0f), //+Z
                new(  0.0f,   0.0f, 180.0f), //-Z
            ];
            ShadowCameras.Fill(i => new XRCamera(new Transform(rotations[i]) { Parent = Transform }, new XRPerspectiveCameraParameters(90.0f, 1.0f, 0.01f, radius)));
            ShadowExponentBase = 1.0f;
            ShadowExponent = 2.5f;
            ShadowMinBias = 0.05f;
            ShadowMaxBias = 10.0f;
        }
        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            _influenceVolume.Center = Transform.WorldTranslation;
            RecalcLightMatrix();
            base.OnTransformWorldMatrixChanged(transform);
        }

        protected override void RecalcLightMatrix()
        {
            LightMatrix = Transform.WorldMatrix * Matrix4x4.CreateScale(Radius);
        }

        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();
            foreach (var cam in ShadowCameras)
                cam.Transform.Parent = Transform;
            RecalcLightMatrix();
        }

        protected internal override void OnComponentActivated()
        {
            if (World?.VisualScene is VisualScene3D scene && Type == ELightType.Dynamic)
            {
                scene.Lights.PointLights.Add(this);

                if (ShadowMap is null)
                    SetShadowMapResolution(1024, 1024);
            }
            base.OnComponentActivated();
        }
        protected internal override void OnComponentDeactivated()
        {
            if (World?.VisualScene is VisualScene3D scene && Type == ELightType.Dynamic)
                scene.Lights.PointLights.Remove(this);

            base.OnComponentDeactivated();
        }

        protected override IVolume GetShadowVolume() => _influenceVolume;

        /// <summary>
        /// This is to set uniforms in the GBuffer lighting shader 
        /// or in a forward shader that requests lighting uniforms.
        /// </summary>
        public override void SetUniforms(XRRenderProgram program, string? targetStructName = null)
        {
            base.SetUniforms(program, targetStructName);

            targetStructName = $"{(targetStructName ?? Engine.Rendering.Constants.LightsStructName)}.";

            program.Uniform($"{targetStructName}Color", _color);
            program.Uniform($"{targetStructName}DiffuseIntensity", _diffuseIntensity);
            program.Uniform($"{targetStructName}Position", _influenceVolume.Center);
            program.Uniform($"{targetStructName}Radius", Radius);
            program.Uniform($"{targetStructName}Brightness", Brightness);

            if (ShadowMap is null)
                return;
            
            var tex = ShadowMap.Material.Textures[1];
            if (tex is not null)
                program.Sampler("ShadowMap", tex, 4);
        }
        public override void SetShadowMapResolution(uint width, uint height)
        {
            bool wasNull = ShadowMap is null;
            uint res = Math.Max(width, height);

            base.SetShadowMapResolution(res, res);

            if (wasNull && ShadowMap is not null)
                ShadowMap.Material.SettingUniforms += SetShadowDepthUniforms;
        }
        /// <summary>
        /// This is to set special uniforms each time any mesh is rendered 
        /// with the shadow depth shader during the shadow pass.
        /// </summary>
        private void SetShadowDepthUniforms(XRMaterialBase material)
        {
            var p = material.ShaderPipelineProgram;
            if (p is null)
                return;

            p.Uniform("FarPlaneDist", Radius);
            p.Uniform("LightPos", _influenceVolume.Center);
            for (int i = 0; i < ShadowCameras.Length; ++i)
            {
                var cam = ShadowCameras[i];
                p.Uniform($"InverseViewMatrices[{i}]", cam.Transform.WorldMatrix);
                p.Uniform($"ProjectionMatrices[{i}]", cam.ProjectionMatrix);
            }
        }
        public override XRMaterial GetShadowMapMaterial(uint width, uint height, EDepthPrecision precision = EDepthPrecision.Flt32)
        {
            uint cubeExtent = Math.Max(width, height);
            XRTexture[] refs =
            [
                new XRTextureCube(cubeExtent, GetShadowDepthMapFormat(precision), EPixelFormat.Red, EPixelType.UnsignedByte)
                {
                    MinFilter = ETexMinFilter.Nearest,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    WWrap = ETexWrapMode.ClampToEdge,
                    FrameBufferAttachment = EFrameBufferAttachment.DepthAttachment,
                },
                 new XRTextureCube(cubeExtent, EPixelInternalFormat.R32f, EPixelFormat.Red, EPixelType.Float)
                {
                    MinFilter = ETexMinFilter.Nearest,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    WWrap = ETexWrapMode.ClampToEdge,
                    FrameBufferAttachment = EFrameBufferAttachment.ColorAttachment0,
                    SamplerName = "ShadowMap"
                },
            ];

            //This material is used for rendering to the framebuffer.
            XRShader fragShader = XRShader.EngineShader("PointLightShadowDepth.fs", EShaderType.Fragment);
            XRShader geomShader = XRShader.EngineShader("PointLightShadowDepth.gs", EShaderType.Geometry);
            XRMaterial mat = new([], refs, fragShader, geomShader);

            //No culling so if a light exists inside of a mesh it will shadow everything.
            mat.RenderOptions.CullMode = ECulling.None;

            return mat;
        }

        protected override XRCamera? GetShadowCamera()
        {
            return null;
        }
    }
}
