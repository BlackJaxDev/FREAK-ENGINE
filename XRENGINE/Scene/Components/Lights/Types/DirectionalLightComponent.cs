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
        private const float NearZ = 0.01f;

        private Vector3 _scale = Vector3.One;
        public Vector3 Scale
        {
            get => _scale;
            set => SetField(ref _scale, value);
        }

        public static XRMesh GetVolumeMeshStatic()
            => XRMesh.Shapes.SolidBox(new Vector3(-0.5f), new Vector3(0.5f));
        protected override XRMesh GetWireframeMesh()
            => XRMesh.Shapes.WireframeBox(new Vector3(-0.5f), new Vector3(0.5f));

        protected XRCamera GetShadowCamera()
        {
            XROrthographicCameraParameters parameters = new(Scale.X, Scale.Y, NearZ, Scale.Z - NearZ);
            parameters.SetOriginPercentages(0.5f, 0.5f);
            return new(ShadowCameraTransform, parameters);
        }

        private XRCamera? _shadowCamera;
        private XRCamera ShadowCamera => _shadowCamera ??= GetShadowCamera();

        private Transform? _shadowCameraTransform;
        private Transform ShadowCameraTransform => _shadowCameraTransform ??= new Transform() 
        {
            Parent = Transform,
            Order = XREngine.Scene.Transforms.Transform.EOrder.TRS,
            Translation = Globals.Backward * Scale.Z * 0.5f,
        };

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            if (Type != ELightType.Dynamic || World?.VisualScene is not VisualScene3D scene3D)
                return;

            if (CastsShadows && ShadowMap is null)
                SetShadowMapResolution(1024u, 1024u);

            scene3D.Lights.DirectionalLights.Add(this);
        }

        protected internal override void OnComponentDeactivated()
        {
            ShadowMap?.Destroy();
            
            if (Type == ELightType.Dynamic && World?.VisualScene is VisualScene3D scene3D)
                scene3D.Lights.DirectionalLights.Remove(this);

            base.OnComponentDeactivated();
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
            mat.RenderOptions.CullMode = ECullMode.None;

            return mat;
        }

        public override void CollectVisibleItems(VisualScene scene)
        {
            if (!CastsShadows)
                return;

            scene.CollectRenderedItems(_shadowRenderPipeline.MeshRenderCommands, ShadowCamera.WorldFrustum(), ShadowCamera);
        }

        public override void RenderShadowMap(VisualScene scene, bool collectVisibleNow = false)
        {
            if (!CastsShadows || ShadowMap?.Material is null)
                return;

            if (collectVisibleNow)
            {
                scene.CollectRenderedItems(_shadowRenderPipeline.MeshRenderCommands, ShadowCamera.WorldFrustum(), ShadowCamera);
                _shadowRenderPipeline.MeshRenderCommands.SwapBuffers();
            }

            _shadowRenderPipeline.Render(scene, ShadowCamera, null, ShadowMap, null, true, ShadowMap.Material);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Transform):
                    if (_shadowCameraTransform is not null)
                        _shadowCameraTransform.Parent = Transform;
                    break;
                case nameof(Scale):
                    MeshCenterAdjustMatrix = Matrix4x4.CreateScale(Scale);
                    ShadowCameraTransform.Translation = Globals.Backward * Scale.Z * 0.5f;
                    if (ShadowCamera.Parameters is not XROrthographicCameraParameters p)
                    {
                        XROrthographicCameraParameters parameters = new(Scale.X, Scale.Y, NearZ, Scale.Z - NearZ);
                        parameters.SetOriginPercentages(0.5f, 0.5f);
                        ShadowCamera.Parameters = parameters;
                    }
                    else
                    {
                        p.Width = Scale.X;
                        p.Height = Scale.Y;
                        p.FarZ = Scale.Z - NearZ;
                        p.NearZ = NearZ;
                    }
                    break;
            }
        }
    }
}
