using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Lights
{
    [RequiresTransform(typeof(Transform))]
    public class DirectionalLightComponent : LightComponent
    {
        private Vector3 _scale = Vector3.One;
        public Vector3 Scale
        {
            get => _scale;
            set
            {
                SetField(ref _scale, value);
                UpdateShadowCameraScale();
                RecalcLightMatrix();
            }
        }

        protected override void RecalcLightMatrix()
            => LightMatrix = Matrix4x4.CreateScale(_scale) * Transform.WorldMatrix;

        protected XRCamera GetShadowCamera()
            => new(ShadowCameraTransform, new XROrthographicCameraParameters(Scale.X, Scale.Y, 0.01f, Scale.Z));

        private XRCamera? _shadowCamera;
        private XRCamera ShadowCamera => _shadowCamera ??= GetShadowCamera();

        private Transform? _shadowCameraTransform;
        private Transform ShadowCameraTransform => _shadowCameraTransform ??= new Transform() { Parent = Transform };

        private void UpdateShadowCameraScale()
        {
            if (ShadowCamera!.Parameters is not XROrthographicCameraParameters p)
                ShadowCamera.Parameters = p = new XROrthographicCameraParameters(Scale.X, Scale.Y, 0.01f, Scale.Z);
            else
            {
                p.Width = Scale.X;
                p.Height = Scale.Y;
                p.FarZ = Scale.Z;
                p.NearZ = 0.01f;
            }
            ShadowCameraTransform.Translation = new Vector3(0.0f, 0.0f, Scale.Z * 0.5f);
        }

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            RecalcLightMatrix();
            base.OnTransformWorldMatrixChanged(transform);
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            if (Type != ELightType.Dynamic || World?.VisualScene is not VisualScene3D scene3D)
                return;

            if (ShadowMap is null)
                SetShadowMapResolution((uint)_shadowMapRenderRegion.Width, (uint)_shadowMapRenderRegion.Height);

            scene3D.Lights.DirectionalLights.Add(this);
        }

        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();

            ShadowMap?.Destroy();
            
            if (Type == ELightType.Dynamic && World?.VisualScene is VisualScene3D scene3D)
                scene3D.Lights.DirectionalLights.Remove(this);
        }

        public override void SetUniforms(XRRenderProgram program, string? targetStructName = null)
        {
            base.SetUniforms(program, targetStructName);

            targetStructName = $"{targetStructName ?? Engine.Rendering.Constants.LightsStructName}.";

            program.Uniform($"{targetStructName}Direction", Transform.WorldForward);
            program.Uniform($"{targetStructName}Color", _color);
            program.Uniform($"{targetStructName}DiffuseIntensity", _diffuseIntensity);
            program.Uniform($"{targetStructName}WorldToLightProjMatrix", ShadowCamera?.ProjectionMatrix ?? Matrix4x4.Identity);
            program.Uniform($"{targetStructName}WorldToLightInvViewMatrix", ShadowCamera?.Transform.WorldMatrix ?? Matrix4x4.Identity);

            if (ShadowMap?.Material is null)
                return;

            var shadowMap = ShadowMap.Material.Textures[1];
            if (shadowMap != null)
                program.Sampler("ShadowMap", shadowMap, 4);
        }

        public override void SetShadowMapResolution(uint width, uint height)
        {
            base.SetShadowMapResolution(width, height);
            UpdateShadowCameraScale();
        }

        public override XRMaterial GetShadowMapMaterial(uint width, uint height, EDepthPrecision precision = EDepthPrecision.Int24)
        {
            XRTexture[] refs =
            [
                 new XRTexture2D(width, height, GetShadowDepthMapFormat(precision), EPixelFormat.DepthComponent, EPixelType.Float)
                 {
                     MinFilter = ETexMinFilter.Nearest,
                     MagFilter = ETexMagFilter.Nearest,
                     UWrap = ETexWrapMode.ClampToEdge,
                     VWrap = ETexWrapMode.ClampToEdge,
                     FrameBufferAttachment = EFrameBufferAttachment.DepthAttachment,
                 },
                 new XRTexture2D(width, height, EPixelInternalFormat.R16f, EPixelFormat.Red, EPixelType.HalfFloat)
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
            XRMaterial mat = new(refs, new XRShader(EShaderType.Fragment, ShaderHelper.Frag_DepthOutput));

            //No culling so if a light exists inside of a mesh it will shadow everything.
            mat.RenderOptions.CullMode = ECulling.None;

            return mat;
        }

        protected override IVolume GetShadowVolume()
            => ShadowCamera.WorldFrustum();

        public override void CollectVisibleItems(VisualScene scene)
        {
            if (!CastsShadows)
                return;

            scene.CollectRenderedItems(_shadowRenderPipeline.MeshRenderCommands, GetShadowVolume(), ShadowCamera);
        }

        public override void RenderShadowMap(VisualScene scene, bool collectVisibleNow = false)
        {
            if (!CastsShadows || ShadowMap?.Material is null)
                return;

            if (collectVisibleNow)
                scene.CollectRenderedItems(_shadowRenderPipeline.MeshRenderCommands, GetShadowVolume(), ShadowCamera);

            scene.SwapBuffers();
            _shadowRenderPipeline.Render(scene, ShadowCamera, null, ShadowMap, null, true, ShadowMap.Material);
        }
    }
}
