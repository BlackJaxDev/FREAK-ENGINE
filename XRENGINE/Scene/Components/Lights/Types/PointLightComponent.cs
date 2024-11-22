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
                MeshCenterAdjustMatrix = Matrix4x4.CreateScale(Radius);
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

        public static XRMesh GetVolumeMesh()
            => XRMesh.Shapes.SolidSphere(Vector3.Zero, 1.0f, 32);
        protected override XRMesh GetWireframeMesh()
            => XRMesh.Shapes.WireframeSphere(Vector3.Zero, Radius, 32);

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

            PositionOnlyTransform positionTransform = new(Transform);
            ShadowCameras.Fill(i => new XRCamera(new Transform(rotations[i], positionTransform), new XRPerspectiveCameraParameters(90.0f, 1.0f, 0.01f, radius)));
            ShadowExponentBase = 1.0f;
            ShadowExponent = 2.5f;
            ShadowMinBias = 0.05f;
            ShadowMaxBias = 10.0f;
        }

        protected override void OnTransformChanged()
        {
            PositionOnlyTransform positionTransform = new(Transform);
            foreach (var cam in ShadowCameras)
                cam.Transform.Parent = positionTransform;
            base.OnTransformChanged();
        }

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            _influenceVolume.Center = Transform.WorldTranslation;
            base.OnTransformWorldMatrixChanged(transform);
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            if (Type != ELightType.Dynamic)
                return;

            World?.Lights.PointLights.Add(this);

            if (CastsShadows && ShadowMap is null)
                SetShadowMapResolution(1024u, 1024u);
        }
        protected internal override void OnComponentDeactivated()
        {
            ShadowMap?.Destroy();

            if (Type == ELightType.Dynamic)
                World?.Lights.PointLights.Remove(this);

            base.OnComponentDeactivated();
        }

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

            var mat = ShadowMap?.Material;
            if (mat is null || mat.Textures.Count < 2)
                return;
            
            var tex = mat.Textures[1];
            if (tex is not null)
                program.Sampler("ShadowMap", tex, 4);
        }
        public override void SetShadowMapResolution(uint width, uint height)
        {
            uint res = Math.Max(width, height);
            base.SetShadowMapResolution(res, res);
        }
        /// <summary>
        /// This is to set special uniforms each time any mesh is rendered 
        /// with the shadow depth shader during the shadow pass.
        /// </summary>
        private void SetShadowDepthUniforms(XRMaterialBase material, XRRenderProgram program)
        {
            program.Uniform("FarPlaneDist", Radius);
            program.Uniform("LightPos", _influenceVolume.Center);
            for (int i = 0; i < ShadowCameras.Length; ++i)
            {
                var cam = ShadowCameras[i];
                program.Uniform($"InverseViewMatrices[{i}]", cam.Transform.WorldMatrix);
                program.Uniform($"ProjectionMatrices[{i}]", cam.ProjectionMatrix);
            }
        }
        public override XRMaterial GetShadowMapMaterial(uint width, uint height, EDepthPrecision precision = EDepthPrecision.Int24)
        {
            uint cubeExtent = Math.Max(width, height);
            XRTexture[] refs =
            [
                new XRTextureCube(cubeExtent, GetShadowDepthMapFormat(precision), EPixelFormat.DepthComponent, EPixelType.UnsignedByte, false)
                {
                    MinFilter = ETexMinFilter.Nearest,
                    MagFilter = ETexMagFilter.Nearest,
                    UWrap = ETexWrapMode.ClampToEdge,
                    VWrap = ETexWrapMode.ClampToEdge,
                    WWrap = ETexWrapMode.ClampToEdge,
                    FrameBufferAttachment = EFrameBufferAttachment.DepthAttachment,
                },
                new XRTextureCube(cubeExtent, EPixelInternalFormat.R16f, EPixelFormat.Red, EPixelType.HalfFloat, false)
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
            XRMaterial mat = new(refs, fragShader, geomShader);

            //No culling so if a light exists inside of a mesh it will shadow everything.
            mat.RenderOptions.CullMode = ECullMode.None;

            return mat;
        }

        public override void CollectVisibleItems(XRWorldInstance world)
        {
            if (!CastsShadows)
                return;

            world.VisualScene.CollectRenderedItems(_shadowRenderPipeline.MeshRenderCommands, _influenceVolume, null, true);
        }

        public override void RenderShadowMap(XRWorldInstance world, bool collectVisibleNow = false)
        {
            if (!CastsShadows || ShadowMap?.Material is null)
                return;

            if (collectVisibleNow)
            {
                world.VisualScene.CollectRenderedItems(_shadowRenderPipeline.MeshRenderCommands, _influenceVolume, null, true);
                _shadowRenderPipeline.MeshRenderCommands.SwapBuffers(true);
            }

            _shadowRenderPipeline.Render(world.VisualScene, null, null, ShadowMap, null, true, ShadowMap.Material);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(ShadowMap):
                        if (ShadowMap?.Material is not null)
                            ShadowMap.Material.SettingUniforms -= SetShadowDepthUniforms;
                        break;
                }
            }
            return change;
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(CastsShadows):
                    if (CastsShadows)
                        SetShadowMapResolution(1024u, 1024u);
                    else
                    {
                        ShadowMap?.Destroy();
                        ShadowMap = null;
                    }
                    break;
                case nameof(ShadowMap):
                    if (ShadowMap?.Material is not null)
                        ShadowMap.Material.SettingUniforms += SetShadowDepthUniforms;
                    break;
                case nameof(Radius):
                    MeshCenterAdjustMatrix = Matrix4x4.CreateScale(Radius);
                    break;
            }
        }
    }
}
