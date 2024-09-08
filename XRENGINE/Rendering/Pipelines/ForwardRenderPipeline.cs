using System.Numerics;
using XREngine.Components.Lights;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;

namespace XREngine.Rendering
{
    public class ForwardRenderPipeline : XRRenderPipeline
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

        public override void DestroyFBOs()
        {
            base.DestroyFBOs();

            BloomBlurFBO1?.Destroy();
            BloomBlurFBO1 = null;
            BloomBlurFBO2?.Destroy();
            BloomBlurFBO2 = null;
            BloomBlurFBO4?.Destroy();
            BloomBlurFBO4 = null;
            BloomBlurFBO8?.Destroy();
            BloomBlurFBO8 = null;
            BloomBlurFBO16?.Destroy();
            BloomBlurFBO16 = null;
            ForwardPassFBO?.Destroy();
            ForwardPassFBO = null;
            //DirLightFBO?.Destroy();
            //DirLightFBO = null;
            GBufferFBO?.Destroy();
            GBufferFBO = null;
            LightCombineFBO?.Destroy();
            LightCombineFBO = null;
            PostProcessFBO?.Destroy();
            PostProcessFBO = null;
            SSAOBlurFBO?.Destroy();
            SSAOBlurFBO = null;
            SSAOFBO?.Destroy();
            SSAOFBO = null;
        }
        //internal void GenerateFBOs()
        //{
        //    DateTime start = DateTime.Now;

        //    BloomBlurFBO1?.Generate();
        //    BloomBlurFBO2?.Generate();
        //    BloomBlurFBO4?.Generate();
        //    BloomBlurFBO8?.Generate();
        //    BloomBlurFBO16?.Generate();
        //    ForwardPassFBO?.Generate();
        //    //DirLightFBO?.Generate();
        //    GBufferFBO?.Generate();
        //    HUDFBO?.Generate();
        //    LightCombineFBO?.Generate();
        //    PostProcessFBO?.Generate();
        //    SSAOBlurFBO?.Generate();
        //    SSAOFBO?.Generate();

        //    TimeSpan span = DateTime.Now - start;
        //    Debug.Out($"FBO regeneration took {span.Seconds} seconds.");
        //}

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

            BloomRect16.Width = (int)(width * 0.0625f);
            BloomRect16.Height = (int)(height * 0.0625f);
            BloomRect8.Width = (int)(width * 0.125f);
            BloomRect8.Height = (int)(height * 0.125f);
            BloomRect4.Width = (int)(width * 0.25f);
            BloomRect4.Height = (int)(height * 0.25f);
            BloomRect2.Width = (int)(width * 0.5f);
            BloomRect2.Height = (int)(height * 0.5f);
            //BloomRect1.Width = width;
            //BloomRect1.Height = height;

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
            XRShader bloomBlurShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "BloomBlur.fs"), EShaderType.Fragment);
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
            XRTexture2D[] blurRefs = { BloomBlurTexture };
            XRTexture2D[] hudRefs = { hudTexture };
            XRTexture2D[] postProcessRefs =
            [
                HDRSceneTexture,
                BloomBlurTexture,
                DepthViewTexture,
                StencilViewTexture,
                hudTexture,
            ];
            ShaderVar[] blurVars =
            [
                new ShaderFloat(0.0f, "Ping"),
                new ShaderInt(0, "LOD"),
            ];
            XRMaterial brightMat = new(brightRefs, brightShader) { RenderOptions = renderParams };
            XRMaterial bloomBlurMat = new(blurVars, blurRefs, bloomBlurShader) { RenderOptions = renderParams };
            XRMaterial postProcessMat = new(postProcessRefs, postProcessShader) { RenderOptions = renderParams };
            XRMaterial hudMat = new(hudRefs, hudShader) { RenderOptions = renderParams };

            ForwardPassFBO = new XRQuadFrameBuffer(brightMat);
            ForwardPassFBO.SettingUniforms += BrightPassFBO_SettingUniforms;
            ForwardPassFBO.SetRenderTargets(
                (HDRSceneTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1),
                (DepthStencilTexture, EFrameBufferAttachment.DepthStencilAttachment, 0, -1));

            BloomBlurFBO1 = new XRQuadFrameBuffer(bloomBlurMat);
            BloomBlurFBO1.SetRenderTargets((BloomBlurTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));
            BloomBlurFBO2 = new XRQuadFrameBuffer(bloomBlurMat);
            BloomBlurFBO2.SetRenderTargets((BloomBlurTexture, EFrameBufferAttachment.ColorAttachment0, 1, -1));
            BloomBlurFBO4 = new XRQuadFrameBuffer(bloomBlurMat);
            BloomBlurFBO4.SetRenderTargets((BloomBlurTexture, EFrameBufferAttachment.ColorAttachment0, 2, -1));
            BloomBlurFBO8 = new XRQuadFrameBuffer(bloomBlurMat);
            BloomBlurFBO8.SetRenderTargets((BloomBlurTexture, EFrameBufferAttachment.ColorAttachment0, 3, -1));
            BloomBlurFBO16 = new XRQuadFrameBuffer(bloomBlurMat);
            BloomBlurFBO16.SetRenderTargets((BloomBlurTexture, EFrameBufferAttachment.ColorAttachment0, 4, -1));

            PostProcessFBO = new XRQuadFrameBuffer(postProcessMat);
            PostProcessFBO.SettingUniforms += _postProcess_SettingUniforms;

            //UserInterfaceFBO = new XRQuadFrameBuffer(hudMat);
            //UserInterfaceFBO.SetRenderTargets((hudTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));

            OnInitializeFBOs();

            ModifyingFBOs = false;
            FBOsInitialized = true;
        }

        private unsafe void CreateSSAONoiseTexture()
        {
            SSAONoiseTexture = new XRTexture2D(_ssaoInfo.NoiseWidth, _ssaoInfo.NoiseHeight, EPixelInternalFormat.Rg32f, EPixelFormat.Rg, EPixelType.Float)
            {
                MinFilter = ETexMinFilter.Nearest,
                MagFilter = ETexMagFilter.Nearest,
                UWrap = ETexWrapMode.Repeat,
                VWrap = ETexWrapMode.Repeat,
                Resizable = false,
            };
            SSAONoiseTexture.Mipmaps[0].GetPixels().SetPixels(_ssaoInfo.Noise.SelectMany(v => new float[] { v.X, v.Y }).ToArray());
        }

        protected virtual void OnInitializeFBOs() { }

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

            if (Engine.Rendering.State.CurrentlyRenderingWorld?.VisualScene is not VisualScene3D scene)
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

        private void BrightPassFBO_SettingUniforms(XRRenderProgram program)
            => RenderingCamera?.SetBloomUniforms(program);

        private void SSAO_SetUniforms(XRRenderProgram program)
        {
            if (RenderingCamera is null)
                return;

            program.Uniform("NoiseScale", Viewport!.InternalResolutionRegion.Extents / 4.0f);
            program.Uniform("Samples", _ssaoInfo.Kernel);

            RenderingCamera.SetUniforms(program);
            RenderingCamera.SetAmbientOcclusionUniforms(program);
        }

        private void _postProcess_SettingUniforms(XRRenderProgram program)
        {
            if (RenderingCamera is null)
                return;

            RenderingCamera.SetUniforms(program);
            RenderingCamera.SetPostProcessUniforms(program);
        }

        private void RenderToFBO(XRFrameBuffer? target)
        {
            target?.BindForWriting();

            Engine.Rendering.State.ClearDepth(1.0f);
            Engine.Rendering.State.EnableDepthTest(true);
            Engine.Rendering.State.AllowDepthWrite(true);
            Engine.Rendering.State.StencilMask(~0);
            Engine.Rendering.State.ClearStencil(0);
            Engine.Rendering.State.Clear(target?.TextureTypes ?? (EFrameBufferTextureType.Color | EFrameBufferTextureType.Depth | EFrameBufferTextureType.Stencil));

            Engine.Rendering.State.AllowDepthWrite(false);
            MeshRenderCommands.Render((int)ERenderPass.Background);

            Engine.Rendering.State.AllowDepthWrite(true);
            MeshRenderCommands.Render((int)ERenderPass.OpaqueDeferredLit);
            MeshRenderCommands.Render((int)ERenderPass.OpaqueForward);
            MeshRenderCommands.Render((int)ERenderPass.TransparentForward);

            //Render forward on-top objects last
            //Disable depth fail for objects on top
            Engine.Rendering.State.DepthFunc(EComparison.Always);
            MeshRenderCommands.Render((int)ERenderPass.OnTopForward);

            target?.UnbindFromWriting();
        }

        private void RenderToViewport(VisualScene visualScene, XRCamera camera, XRViewport? viewport, XRFrameBuffer? target)
        {
            PushRenderingCamera(camera);
            {
                //Enable internal resolution
                using (Engine.Rendering.State.PushRenderArea(viewport.InternalResolutionRegion))
                {
                    RenderDeferredPass();

                    SSAOFBO!.RenderTo(SSAOBlurFBO!);
                    SSAOBlurFBO!.RenderTo(GBufferFBO!);

                    if (visualScene is VisualScene3D scene3D)
                        RenderLightPass(scene3D.Lights);

                    RenderForwardPass();
                    RenderBloomPass();

                    ColorGradingSettings? cgs = camera.PostProcessing?.ColorGrading;
                    if (cgs != null && cgs.AutoExposure)
                        cgs.Exposure = AbstractRenderer.Current?.CalculateDotLuminance(HDRSceneTexture!, true) ?? 1.0f;

                    XRMaterial postMat = camera.PostProcessMaterial;
                    if (postMat != null)
                        RenderPostProcessPass(viewport, postMat);
                }

                //Full viewport resolution now
                using (Engine.Rendering.State.PushRenderArea(viewport.Region))
                {
                    //Render the last pass to the actual screen resolution, 
                    //or the provided target FBO
                    target?.BindForWriting();
                    PostProcessFBO!.Render();
                    target?.UnbindFromWriting();
                }
            }
            PopRenderingCamera();
        }

        private void RenderPostProcessPass(XRViewport viewport, XRMaterial post)
        {
            //TODO: Apply camera post process material pass here

        }
        private void RenderDeferredPass()
        {
            using (SSAOFBO!.BindForWriting())
            {
                Engine.Rendering.State.StencilMask(~0);
                Engine.Rendering.State.ClearStencil(0);
                Engine.Rendering.State.Clear(EFrameBufferTextureType.Color | EFrameBufferTextureType.Depth | EFrameBufferTextureType.Stencil);
                Engine.Rendering.State.EnableDepthTest(true);
                Engine.Rendering.State.ClearDepth(1.0f);
                MeshRenderCommands.Render((int)ERenderPass.OpaqueDeferredLit);
                MeshRenderCommands.Render((int)ERenderPass.DeferredDecals);
                Engine.Rendering.State.EnableDepthTest(false);
            }
        }
        private void RenderForwardPass()
        {
            using (ForwardPassFBO!.BindForWriting())
            {
                //Render the deferred pass lighting result, no depth testing
                Engine.Rendering.State.EnableDepthTest(false);
                LightCombineFBO!.Render();

                //Normal depth test for opaque forward
                Engine.Rendering.State.EnableDepthTest(true);
                MeshRenderCommands.Render((int)ERenderPass.OpaqueForward);

                //No depth writing for backgrounds (skybox)
                Engine.Rendering.State.AllowDepthWrite(false);
                MeshRenderCommands.Render((int)ERenderPass.Background);

                //Render forward transparent objects next, normal depth testing
                Engine.Rendering.State.EnableDepthTest(true);
                MeshRenderCommands.Render((int)ERenderPass.TransparentForward);

                //Render forward on-top objects last
                MeshRenderCommands.Render((int)ERenderPass.OnTopForward);

                Engine.Rendering.State.EnableDepthTest(false);
            }
        }
        private void RenderLightPass(Lights3DCollection lights)
        {
            LightCombineFBO!.BindForWriting();
            {
                //Start with blank slate so additive blending doesn't ghost old frames
                Engine.Rendering.State.Clear(EFrameBufferTextureType.Color);

                foreach (PointLightComponent c in lights.PointLights)
                    RenderPointLight(c);

                foreach (SpotLightComponent c in lights.SpotLights)
                    RenderSpotLight(c);

                foreach (DirectionalLightComponent c in lights.DirectionalLights)
                    RenderDirLight(c);
            }
            LightCombineFBO.UnbindFromWriting();
        }

        private static void BloomBlur(XRQuadFrameBuffer fbo, int mipmap, float dir)
        {
            using (fbo.BindForWriting())
            {
                fbo.Material.Parameter<ShaderFloat>(0)!.Value = dir;
                fbo.Material.Parameter<ShaderInt>(1)!.Value = mipmap;
                fbo.Render();
            }
        }

        private void BloomScaledPass(XRQuadFrameBuffer fbo, BoundingRectangle rect, int mipmap)
        {
            using (Engine.Rendering.State.PushRenderArea(rect))
            {
                BloomBlur(fbo, mipmap, 0.0f);
                BloomBlur(fbo, mipmap, 1.0f);
            }
        }
        private void RenderBloomPass()
        {
            using (BloomBlurFBO1!.BindForWriting())
                ForwardPassFBO!.Render();

            BloomBlurTexture!.Bind();
            BloomBlurTexture.GenerateMipmapsGPU();

            BloomScaledPass(BloomBlurFBO16!, BloomRect16, 4);
            BloomScaledPass(BloomBlurFBO8!, BloomRect8, 3);
            BloomScaledPass(BloomBlurFBO4!, BloomRect4, 2);
            BloomScaledPass(BloomBlurFBO2!, BloomRect2, 1);
            //Don't blur original image, barely makes a difference to result
        }
    }
}