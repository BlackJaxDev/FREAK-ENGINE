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
                _scale = value;

                if (ShadowCamera != null)
                {
                    if (ShadowCamera.Parameters is not XROrthographicCameraParameters p)
                        ShadowCamera.Parameters = p = new XROrthographicCameraParameters(Scale.X, Scale.Y, 0.01f, Scale.Z);
                    else
                    {
                        p.Width = Scale.X;
                        p.Height = Scale.Y;
                        p.FarPlane = Scale.Z;
                        p.NearPlane = 0.01f;
                    }

                    //ShadowCamera.Translation.Value = Transform.WorldTranslation;
                    //ShadowCamera.TranslateRelative(0.0f, 0.0f, Scale.Z * 0.5f);
                }

                LightMatrix = Transform.WorldMatrix * Matrix4x4.CreateScale(_scale);
            }
        }

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            if (ShadowCamera != null)
            {
                //ShadowCamera.Translation.Value = WorldPoint;
                //ShadowCamera.TranslateRelative(0.0f, 0.0f, Scale.Z * 0.5f);
            }

            LightMatrix = Transform.WorldMatrix * Matrix4x4.CreateScale(_scale);
            base.OnTransformWorldMatrixChanged(transform);
        }
        //protected override void OnWorldTransformChanged(bool recalcChildWorldTransformsNow = true)
        //{
        //    if (ShadowCamera != null)
        //    {
        //        ShadowCamera.Translation.Value = WorldPoint;
        //        ShadowCamera.TranslateRelative(0.0f, 0.0f, Scale.Z * 0.5f);
        //    }

        //    LightMatrix = WorldMatrix.Value * Scale.AsScaleMatrix();

        //    base.OnWorldTransformChanged(recalcChildWorldTransformsNow);
        //}

        protected internal override void Start()
        {
            base.Start();

            if (Type != ELightType.Dynamic || World?.VisualScene is not VisualScene3D scene3D)
                return;

            if (ShadowMap is null)
                SetShadowMapResolution((uint)_shadowMapRenderRegion.Width, (uint)_shadowMapRenderRegion.Height);

            scene3D.Lights.DirectionalLights.Add(this);

            //ShadowCamera.Transform.LocalTranslation = WorldPoint;
            //ShadowCamera.TranslateRelative(0.0f, 0.0f, Scale.Z * 0.5f);
        }

        protected internal override void Stop()
        {
            base.Stop();

            ShadowMap?.Destroy();
            
            if (Type == ELightType.Dynamic && World?.VisualScene is VisualScene3D scene3D)
                scene3D.Lights.DirectionalLights.Remove(this);
        }

        public override void SetUniforms(XRRenderProgram program, string? targetStructName = null)
        {
            if (ShadowMap?.Material is null || ShadowCamera is null)
                return;

            targetStructName = $"{targetStructName ?? Engine.Rendering.Constants.LightsStructName}.";

            program.Uniform($"{targetStructName}Direction", Transform.WorldForward);
            program.Uniform($"{targetStructName}Color", _color);
            program.Uniform($"{targetStructName}DiffuseIntensity", _diffuseIntensity);
            program.Uniform($"{targetStructName}WorldToLightSpaceProjMatrix", ShadowCamera.WorldViewProjectionMatrix);

            program.Sampler("ShadowMap", ShadowMap.Material.Textures[1], 4);
        }

        public override void SetShadowMapResolution(uint width, uint height)
        {
            base.SetShadowMapResolution(width, height);
            //if (ShadowCamera is null)
            //{
            //    ShadowCamera = new OrthographicCamera(Vector3.One, Vector3.Zero, Rotator.GetZero(), new Vector2(0.5f), 0.0f, Scale.Z);
            //    ShadowCamera.Rotation.SyncFrom(Transform.Rotation);
            //    ShadowCamera.Resize(Scale.X, Scale.Y);
            //}
        }

        public override XRMaterial GetShadowMapMaterial(uint width, uint height, EDepthPrecision precision = EDepthPrecision.Flt32)
        {
            XRTexture[] refs =
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
            XRMaterial mat = new(refs, new XRShader(EShaderType.Fragment, ShaderHelper.Frag_DepthOutput));

            //No culling so if a light exists inside of a mesh it will shadow everything.
            mat.RenderOptions.CullMode = ECulling.None;

            return mat;
        }
    }
}
