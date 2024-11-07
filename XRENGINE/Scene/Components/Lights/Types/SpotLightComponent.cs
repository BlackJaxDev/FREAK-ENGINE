﻿using Extensions;
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
            set => SetField(ref _distance, value);
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

        public static XRMesh GetVolumeMeshStatic()
            => XRMesh.Shapes.SolidCone(Vector3.Zero, Globals.Backward, 1.0f, 1.0f, 32, true);
        protected override XRMesh GetWireframeMesh()
            => XRMesh.Shapes.WireframeCone(Vector3.Zero, Globals.Backward, 1.0f, 1.0f, 32);

        public Cone OuterCone => _outerCone;
        public Cone InnerCone => _innerCone;

        public SpotLightComponent()
            : this(100.0f, 60.0f, 30.0f, 1.0f, 1.0f) { }

        protected override void OnTransformWorldMatrixChanged(TransformBase transform)
        {
            UpdateCones();
            base.OnTransformWorldMatrixChanged(transform);
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            if (Type != ELightType.Dynamic || World?.VisualScene is not VisualScene3D scene)
                return;
            
            scene.Lights.SpotLights.Add(this);
            if (CastsShadows && ShadowMap is null)
                SetShadowMapResolution(1024u, 1024u);
        }

        protected internal override void OnComponentDeactivated()
        {
            ShadowMap?.Destroy();

            if (Type == ELightType.Dynamic && World?.VisualScene is VisualScene3D scene)
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

            var mat = ShadowMap?.Material;
            if (mat is null || mat.Textures.Count < 2)
                return;
            
            var tex = mat.Textures[1];
            if (tex is not null)
                program.Sampler("ShadowMap", tex, 4);
        }

        protected XRCamera GetShadowCamera()
            => new(Transform, new XRPerspectiveCameraParameters(Math.Max(OuterCutoffAngleDegrees, InnerCutoffAngleDegrees) * 2.0f, 1.0f, 1.0f, _distance));

        private XRCamera? _shadowCamera;
        private XRCamera ShadowCamera => _shadowCamera ??= GetShadowCamera();

        public override void SetShadowMapResolution(uint width, uint height)
        {
            base.SetShadowMapResolution(width, height);

            if (ShadowCamera?.Parameters is XRPerspectiveCameraParameters p)
                p.AspectRatio = width / height;
        }

        public override XRMaterial GetShadowMapMaterial(uint width, uint height, EDepthPrecision precision = EDepthPrecision.Int24)
        {
            XRTexture2D[] refs =
            [
                new XRTexture2D(width, height, GetShadowDepthMapFormat(precision), EPixelFormat.DepthComponent, EPixelType.UnsignedByte)
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
                case nameof(Distance):
                    UpdateCones();
                    break;
            }
        }

        public void SetCutoffs(float innerDegrees, float outerDegrees, bool constrainInnerToOuter = true)
        {
            innerDegrees = innerDegrees.Clamp(0.0f, 90.0f);
            outerDegrees = outerDegrees.Clamp(0.0f, 90.0f);

            if (outerDegrees < innerDegrees)
            {
                float bias = 0.0001f;
                if (constrainInnerToOuter)
                    innerDegrees = outerDegrees - bias;
                else
                    outerDegrees = innerDegrees + bias;
            }

            SetField(ref _outerCutoff, MathF.Cos(DegToRad(outerDegrees)));
            SetField(ref _innerCutoff, MathF.Cos(DegToRad(innerDegrees)));

            if (ShadowCamera != null && ShadowCamera.Parameters is XRPerspectiveCameraParameters p)
                p.VerticalFieldOfView = Math.Max(outerDegrees, innerDegrees) * 2.0f;

            UpdateCones();
        }

        private void UpdateCones()
        {
            float d = Distance;
            Vector3 dir = Transform.WorldForward;
            Vector3 coneOrigin = Transform.WorldTranslation + dir * (d * 0.5f);

            SetField(ref _outerCone, new(coneOrigin, -dir, d, MathF.Tan(DegToRad(OuterCutoffAngleDegrees)) * d));
            SetField(ref _innerCone, new(coneOrigin, -dir, d, MathF.Tan(DegToRad(InnerCutoffAngleDegrees)) * d));

            if (ShadowCamera != null)
                ShadowCamera.FarZ = d;

            MeshCenterAdjustMatrix = Matrix4x4.CreateScale(OuterCone.Radius, OuterCone.Radius, OuterCone.Height) * Matrix4x4.CreateTranslation(Globals.Forward * (Distance * 0.5f));
        }
    }
}
