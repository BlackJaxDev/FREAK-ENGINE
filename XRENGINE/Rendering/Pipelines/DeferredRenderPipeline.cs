﻿using System.Numerics;
using XREngine.Components.Lights;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;
using static XREngine.Engine.Rendering.State;

namespace XREngine.Rendering
{
    public class DeferredRenderPipeline : XRRenderPipeline
    {
        public ScreenSpaceAmbientOcclusionOptions _ssaoInfo = new();

        public XRTexture2D? DepthStencilTexture { get; private set; }
        public XRTexture2D? HDRSceneTexture { get; private set; }
        public XRTexture2D? BloomBlurTexture { get; private set; }
        public XRTexture2D? AlbedoOpacityTexture { get; private set; }
        public XRTexture2D? NormalTexture { get; private set; }
        public XRTexture2D? RMSITexture { get; private set; }
        public XRTexture2D? SSAONoiseTexture { get; private set; }
        public XRTexture2D? SSAOTexture { get; private set; }
        public XRTexture2D? LightingTexture { get; private set; }
        public XRTextureView2D? DepthViewTexture { get; private set; }
        public XRTextureView2D? StencilViewTexture { get; private set; }

        public XRQuadFrameBuffer? SSAOFBO { get; private set; }
        public XRQuadFrameBuffer? SSAOBlurFBO { get; private set; }
        public XRFrameBuffer? GBufferFBO { get; private set; }
        public XRQuadFrameBuffer? BloomBlurFBO1 { get; private set; }
        public XRQuadFrameBuffer? BloomBlurFBO2 { get; private set; }
        public XRQuadFrameBuffer? BloomBlurFBO4 { get; private set; }
        public XRQuadFrameBuffer? BloomBlurFBO8 { get; private set; }
        public XRQuadFrameBuffer? BloomBlurFBO16 { get; private set; }
        public XRQuadFrameBuffer? LightCombineFBO { get; private set; }
        public XRQuadFrameBuffer? ForwardPassFBO { get; private set; }
        public XRQuadFrameBuffer? PostProcessFBO { get; private set; }

        public XRMeshRenderer? PointLightRenderer { get; private set; }
        public XRMeshRenderer? SpotLightRenderer { get; private set; }
        public XRMeshRenderer? DirectionalLightRenderer { get; private set; }

        //public PrimitiveManager DecalManager;
        //public QuadFrameBuffer DirLightFBO;

        public XRTexture2D? _brdfTex = null;
        public XRTexture2D? BrdfTex => _brdfTex ??= PrecomputeBRDF();

        public BoundingRectangle BloomRect16;
        public BoundingRectangle BloomRect8;
        public BoundingRectangle BloomRect4;
        public BoundingRectangle BloomRect2;
        //public BoundingRectangle BloomRect1;

        internal void RenderDirLight(DirectionalLightComponent c)
            => RenderLight(DirectionalLightRenderer!, c);
        internal void RenderPointLight(PointLightComponent c)
            => RenderLight(PointLightRenderer!, c);
        internal void RenderSpotLight(SpotLightComponent c)
            => RenderLight(SpotLightRenderer!, c);
        private void RenderLight(XRMeshRenderer renderer, LightComponent comp)
        {
            _currentLightComponent = comp;
            renderer.Render(comp.LightMatrix);
            _currentLightComponent = null;
        }

        //internal void RenderDecal(DecalComponent c)
        //{
        //    _decalComp = c;
        //    DecalManager.Render(c.DecalRenderMatrix);
        //}

        /// <summary>
        /// This method is called to generate all framebuffers necessary to render the final image for the viewport.
        /// </summary>
        public override unsafe void InitializeFBOs()
        {
            base.InitializeFBOs();

            if (Viewport is null)
                return;

            uint width = (uint)Viewport.InternalWidth;
            uint height = (uint)Viewport.InternalHeight;

            RenderingParameters renderParams = new();
            renderParams.DepthTest.Enabled = ERenderParamUsage.Unchanged;
            renderParams.DepthTest.UpdateDepth = false;
            renderParams.DepthTest.Function = EComparison.Always;

            DepthStencilTexture = XRTexture2D.CreateFrameBufferTexture(width, height,
                EPixelInternalFormat.Depth24Stencil8, EPixelFormat.DepthStencil, EPixelType.UnsignedInt248,
                EFrameBufferAttachment.DepthStencilAttachment);
            DepthStencilTexture.MinFilter = ETexMinFilter.Nearest;
            DepthStencilTexture.MagFilter = ETexMagFilter.Nearest;
            DepthStencilTexture.Resizable = false;

            DepthViewTexture = new XRTextureView2D(DepthStencilTexture, 0, 1, 0, 1,
                EPixelType.UnsignedInt248, EPixelFormat.DepthStencil, EPixelInternalFormat.Depth24Stencil8)
            {
                Resizable = false,
                DepthStencilFormat = EDepthStencilFmt.Depth,
                MinFilter = ETexMinFilter.Nearest,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
            };

            StencilViewTexture = new XRTextureView2D(DepthStencilTexture, 0, 1, 0, 1,
                EPixelType.UnsignedInt248, EPixelFormat.DepthStencil, EPixelInternalFormat.Depth24Stencil8)
            {
                Resizable = false,
                DepthStencilFormat = EDepthStencilFmt.Stencil,
                MinFilter = ETexMinFilter.Nearest,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.ClampToEdge,
                VWrap = ETexWrapMode.ClampToEdge,
            };

            //If forward, we can render directly to the post process FBO.
            //If deferred, we have to render to a quad first, then render that to post process
            //if (Engine.Settings.EnableDeferredPass)
            {
                CreateSSAONoiseTexture();

                AlbedoOpacityTexture = XRTexture2D.CreateFrameBufferTexture(width, height,
                    EPixelInternalFormat.Rgba16f, EPixelFormat.Rgba, EPixelType.HalfFloat);
                NormalTexture = XRTexture2D.CreateFrameBufferTexture(width, height,
                    EPixelInternalFormat.Rgb16f, EPixelFormat.Rgb, EPixelType.HalfFloat);
                RMSITexture = XRTexture2D.CreateFrameBufferTexture(width, height,
                    EPixelInternalFormat.Rgba8, EPixelFormat.Rgba, EPixelType.UnsignedByte);
                SSAOTexture = XRTexture2D.CreateFrameBufferTexture(width, height,
                    EPixelInternalFormat.R16f, EPixelFormat.Red, EPixelType.HalfFloat,
                    EFrameBufferAttachment.ColorAttachment0);
                SSAOTexture.MinFilter = ETexMinFilter.Nearest;
                SSAOTexture.MagFilter = ETexMagFilter.Nearest;

                XRShader ssaoShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "SSAOGen.fs"), EShaderType.Fragment);
                XRShader ssaoBlurShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "SSAOBlur.fs"), EShaderType.Fragment);

                XRTexture2D[] ssaoRefs =
                [
                    NormalTexture,
                    SSAONoiseTexture!,
                    DepthViewTexture,
                ];
                XRTexture2D[] ssaoBlurRefs =
                [
                    SSAOTexture
                ];
                //XRTexture2D[] deferredLightingRefs = new XRTexture2D[]
                //{
                //    albedoOpacityTexture,
                //    normalTexture,
                //    rmsiTexture,
                //    ssaoTexture,
                //    depthViewTexture,
                //};

                XRMaterial ssaoMat = new(ssaoRefs, ssaoShader) { RenderOptions = renderParams };
                XRMaterial ssaoBlurMat = new(ssaoBlurRefs, ssaoBlurShader) { RenderOptions = renderParams };

                SSAOFBO = new XRQuadFrameBuffer(ssaoMat);
                SSAOFBO.SettingUniforms += SSAO_SetUniforms;
                SSAOFBO.SetRenderTargets(
                    (AlbedoOpacityTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1),
                    (NormalTexture, EFrameBufferAttachment.ColorAttachment1, 0, -1),
                    (RMSITexture, EFrameBufferAttachment.ColorAttachment2, 0, -1),
                    (DepthStencilTexture, EFrameBufferAttachment.DepthStencilAttachment, 0, -1));

                SSAOBlurFBO = new XRQuadFrameBuffer(ssaoBlurMat);
                GBufferFBO = new XRFrameBuffer();
                GBufferFBO.SetRenderTargets((SSAOTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));

                #region Light Meshes

               BlendMode additiveBlend = new()
                {
                    //Add the previous and current light colors together using FuncAdd with each mesh render
                    Enabled = ERenderParamUsage.Enabled,
                    RgbDstFactor = EBlendingFactor.One,
                    AlphaDstFactor = EBlendingFactor.One,
                    RgbSrcFactor = EBlendingFactor.One,
                    AlphaSrcFactor = EBlendingFactor.One,
                    RgbEquation = EBlendEquationMode.FuncAdd,
                    AlphaEquation = EBlendEquationMode.FuncAdd,
                };
                RenderingParameters additiveRenderParams = new()
                {
                    //Render only the backside so that the light still shows if the camera is inside of the volume
                    //and the light does not add itself twice for the front and back faces.
                    CullMode = ECulling.Front,
                    UniformRequirements = EUniformRequirements.Camera,
                    BlendMode = additiveBlend,
                };
                additiveRenderParams.DepthTest.Enabled = ERenderParamUsage.Disabled;

                LightingTexture = XRTexture2D.CreateFrameBufferTexture(width, height,
                    EPixelInternalFormat.Rgb16f, EPixelFormat.Rgb, EPixelType.HalfFloat);
                XRShader lightCombineShader = XRShader.EngineShader(
                    Path.Combine(SceneShaderPath, "DeferredLightCombine.fs"), EShaderType.Fragment);
                XRTexture[] lightCombineTextures =
                [
                    AlbedoOpacityTexture,
                    NormalTexture,
                    RMSITexture,
                    SSAOTexture,
                    DepthViewTexture,
                    LightingTexture,
                    BrdfTex,
                    //irradiance
                    //prefilter
                ];

                XRMaterial lightCombineMat = new(lightCombineTextures, lightCombineShader) { RenderOptions = renderParams };
                LightCombineFBO = new XRQuadFrameBuffer(lightCombineMat);
                LightCombineFBO.SetRenderTargets((LightingTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));
                LightCombineFBO.SettingUniforms += LightCombineFBO_SettingUniforms;

                XRTexture2D[] lightRefs =
                [
                    AlbedoOpacityTexture,
                    NormalTexture,
                    RMSITexture,
                    DepthViewTexture,
                    //shadow map texture
                ];

                XRShader pointLightShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "DeferredLightingPoint.fs"), EShaderType.Fragment);
                XRShader spotLightShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "DeferredLightingSpot.fs"), EShaderType.Fragment);
                XRShader dirLightShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "DeferredLightingDir.fs"), EShaderType.Fragment);

                XRMaterial pointLightMat = new(lightRefs, pointLightShader) { RenderOptions = additiveRenderParams };
                XRMaterial spotLightMat = new(lightRefs, spotLightShader) { RenderOptions = additiveRenderParams };
                XRMaterial dirLightMat = new(lightRefs, dirLightShader) { RenderOptions = additiveRenderParams };

                XRMesh pointLightMesh = XRMesh.Shapes.SolidSphere(Vector3.Zero, 1.0f, 20u);
                XRMesh spotLightMesh = XRMesh.Shapes.SolidCone(Vector3.Zero, Vector3.UnitZ, 1.0f, 1.0f, 32, true);
                XRMesh dirLightMesh = XRMesh.Shapes.SolidBox(new Vector3(-0.5f), new Vector3(0.5f));

                PointLightRenderer = new XRMeshRenderer(pointLightMesh, pointLightMat);
                PointLightRenderer.SettingUniforms += LightManager_SettingUniforms;

                SpotLightRenderer = new XRMeshRenderer(spotLightMesh, spotLightMat);
                SpotLightRenderer.SettingUniforms += LightManager_SettingUniforms;

                DirectionalLightRenderer = new XRMeshRenderer(dirLightMesh, dirLightMat);
                DirectionalLightRenderer.SettingUniforms += LightManager_SettingUniforms;

                #endregion
            }

            //Begin forward pass

            BloomBlurTexture = XRTexture2D.CreateFrameBufferTexture(width, height,
                EPixelInternalFormat.Rgb8, EPixelFormat.Rgb, EPixelType.UnsignedByte);

            BloomBlurTexture.MagFilter = ETexMagFilter.Linear;
            BloomBlurTexture.MinFilter = ETexMinFilter.LinearMipmapLinear;
            BloomBlurTexture.UWrap = ETexWrapMode.ClampToEdge;
            BloomBlurTexture.VWrap = ETexWrapMode.ClampToEdge;

            HDRSceneTexture = XRTexture2D.CreateFrameBufferTexture(width, height,
                EPixelInternalFormat.Rgba16f, EPixelFormat.Rgba, EPixelType.HalfFloat,
                EFrameBufferAttachment.ColorAttachment0);

            //_hdrSceneTexture.Resizeable = false;
            HDRSceneTexture.MinFilter = ETexMinFilter.Nearest;
            HDRSceneTexture.MagFilter = ETexMagFilter.Nearest;
            HDRSceneTexture.UWrap = ETexWrapMode.ClampToEdge;
            HDRSceneTexture.VWrap = ETexWrapMode.ClampToEdge;
            HDRSceneTexture.SamplerName = "HDRSceneTex";

            XRShader brightShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "BrightPass.fs"), EShaderType.Fragment);
            XRShader postProcessShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "PostProcess.fs"), EShaderType.Fragment);
            XRShader hudShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "HudFBO.fs"), EShaderType.Fragment);

            XRTexture2D hudTexture = XRTexture2D.CreateFrameBufferTexture(width, height,
                EPixelInternalFormat.Rgba16f, EPixelFormat.Rgba, EPixelType.HalfFloat);
            hudTexture.MinFilter = ETexMinFilter.Nearest;
            hudTexture.MagFilter = ETexMagFilter.Nearest;
            hudTexture.UWrap = ETexWrapMode.ClampToEdge;
            hudTexture.VWrap = ETexWrapMode.ClampToEdge;
            hudTexture.SamplerName = "HUDTex";

            XRTexture2D[] brightRefs = { HDRSceneTexture };
            XRTexture2D[] hudRefs = { hudTexture };
            XRTexture2D[] postProcessRefs =
            [
                HDRSceneTexture,
                BloomBlurTexture,
                DepthViewTexture,
                StencilViewTexture,
                hudTexture,
            ];
            XRMaterial brightMat = new(brightRefs, brightShader) { RenderOptions = renderParams };
            XRMaterial postProcessMat = new(postProcessRefs, postProcessShader) { RenderOptions = renderParams };
            XRMaterial hudMat = new(hudRefs, hudShader) { RenderOptions = renderParams };

            ForwardPassFBO = new XRQuadFrameBuffer(brightMat);
            ForwardPassFBO.SettingUniforms += BrightPassFBO_SettingUniforms;
            ForwardPassFBO.SetRenderTargets(
                (HDRSceneTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1),
                (DepthStencilTexture, EFrameBufferAttachment.DepthStencilAttachment, 0, -1));

            PostProcessFBO = new XRQuadFrameBuffer(postProcessMat);
            PostProcessFBO.SettingUniforms += _postProcess_SettingUniforms;

            //UserInterfaceFBO = new XRQuadFrameBuffer(hudMat);
            //UserInterfaceFBO.SetRenderTargets((hudTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));
        }

        private LightComponent? _currentLightComponent;
        //private DecalComponent _decalComp;

        private void LightManager_SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            if (_currentLightComponent is null)
                return;

            RenderingCamera?.PostProcessing?.Shadows.SetUniforms(materialProgram);
            _currentLightComponent.SetShadowUniforms(materialProgram);
            _currentLightComponent.SetUniforms(materialProgram);
        }
        private void LightCombineFBO_SettingUniforms(XRRenderProgram program)
        {
            if (RenderingCamera is null)
                return;

            RenderingCamera.SetUniforms(program);

            if (RenderingWorld?.VisualScene is not VisualScene3D scene)
                return;

            var lightProbes = scene.Lights.GetNearestProbes(program.LightProbeTransform?.WorldTranslation ?? Vector3.Zero);
            if (lightProbes.Length == 0)
                return;

            LightProbeComponent probe = lightProbes[0];
            int baseCount = LightCombineFBO?.Material?.Textures?.Count ?? 0;

            if (probe.IrradianceTexture != null)
            {
                var tex = probe.IrradianceTexture;
                if (tex != null)
                    program.Sampler("Irradiance", tex, baseCount);
            }

            ++baseCount;

            if (probe.PrefilterTex != null)
            {
                var tex = probe.PrefilterTex;
                if (tex != null)
                    program.Sampler("Prefilter", tex, baseCount);
            }
        }

        //private void DecalManager_SettingUniforms(RenderProgram vertexProgram, RenderProgram materialProgram)
        //{
        //    materialProgram.Uniform("BoxWorldMatrix", _decalComp.WorldMatrix);
        //    materialProgram.Uniform("InvBoxWorldMatrix", _decalComp.InverseWorldMatrix);
        //    materialProgram.Uniform("BoxHalfScale", _decalComp.Box.HalfExtents.Raw);
        //    materialProgram.Sampler("Texture4", _decalComp.AlbedoOpacity.RenderTextureGeneric, 4);
        //    materialProgram.Sampler("Texture5", _decalComp.Normal.RenderTextureGeneric, 5);
        //    materialProgram.Sampler("Texture6", _decalComp.RMSI.RenderTextureGeneric, 6);
        //}

        //private XRCamera? RenderingCamera => Engine.Rendering.State.RenderingCamera;

        private void BrightPassFBO_SettingUniforms(XRRenderProgram program)
            => RenderingCamera?.SetBloomUniforms(program);

        private void RenderPostProcessPass(XRViewport viewport, XRMaterial post)
        {
            //TODO: Apply camera post process material pass here
        }
        private void RenderDeferredPass()
        {
            using (SSAOFBO!.BindForWriting())
            {
                StencilMask(~0);
                ClearStencil(0);
                Clear(EFrameBufferTextureType.Color | EFrameBufferTextureType.Depth | EFrameBufferTextureType.Stencil);
                EnableDepthTest(true);
                ClearDepth(1.0f);
                MeshRenderCommands.Render((int)ERenderPass.OpaqueDeferredLit);
                MeshRenderCommands.Render((int)ERenderPass.DeferredDecals);
                EnableDepthTest(false);
            }
        }
        private void RenderForwardPass()
        {
            using (ForwardPassFBO!.BindForWriting())
            {
                //Render the deferred pass lighting result, no depth testing
                EnableDepthTest(false);
                LightCombineFBO!.Render();

                //Normal depth test for opaque forward
                EnableDepthTest(true);
                MeshRenderCommands.Render((int)ERenderPass.OpaqueForward);

                //No depth writing for backgrounds (skybox)
                AllowDepthWrite(false);
                MeshRenderCommands.Render((int)ERenderPass.Background);

                //Render forward transparent objects next, normal depth testing
                EnableDepthTest(true);
                MeshRenderCommands.Render((int)ERenderPass.TransparentForward);

                //Render forward on-top objects last
                MeshRenderCommands.Render((int)ERenderPass.OnTopForward);

                EnableDepthTest(false);
            }
        }
        private void RenderLightPass(Lights3DCollection lights)
        {
            LightCombineFBO!.BindForWriting();
            {
                //Start with blank slate so additive blending doesn't ghost old frames
                Clear(EFrameBufferTextureType.Color);

                foreach (PointLightComponent c in lights.PointLights)
                    RenderPointLight(c);

                foreach (SpotLightComponent c in lights.SpotLights)
                    RenderSpotLight(c);

                foreach (DirectionalLightComponent c in lights.DirectionalLights)
                    RenderDirLight(c);
            }
            LightCombineFBO.UnbindFromWriting();
        }
    }
}