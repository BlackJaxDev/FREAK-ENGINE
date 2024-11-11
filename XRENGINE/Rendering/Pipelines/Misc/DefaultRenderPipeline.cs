using XREngine.Components.Lights;
using XREngine.Data.Colors;
using XREngine.Data.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Pipelines.Commands;
using XREngine.Scene;
using static XREngine.Engine.Rendering.State;

namespace XREngine.Rendering;

public class DefaultRenderPipeline : RenderPipeline
{
    public const string SceneShaderPath = "Scene3D";

    private readonly NearToFarRenderCommandSorter _nearToFarSorter = new();
    private readonly FarToNearRenderCommandSorter _farToNearSorter = new();

    protected override Dictionary<int, IComparer<RenderCommand>?> GetPassIndicesAndSorters()
        => new()
        {
            { (int)EDefaultRenderPass.PreRender, _nearToFarSorter },
            { (int)EDefaultRenderPass.Background, null },
            { (int)EDefaultRenderPass.OpaqueDeferredLit, _nearToFarSorter },
            { (int)EDefaultRenderPass.DeferredDecals, _farToNearSorter },
            { (int)EDefaultRenderPass.OpaqueForward, _nearToFarSorter },
            { (int)EDefaultRenderPass.TransparentForward, _farToNearSorter },
            { (int)EDefaultRenderPass.OnTopForward, null },
            { (int)EDefaultRenderPass.PostRender, _nearToFarSorter }
        };

    protected override Lazy<XRMaterial> InvalidMaterialFactory => new(MakeInvalidMaterial, LazyThreadSafetyMode.PublicationOnly);

    private XRMaterial MakeInvalidMaterial()
    {
        Debug.Out("Generating invalid material");
        return XRMaterial.CreateColorMaterialDeferred();
    }

    //FBOs
    const string SSAOFBOName = "SSAOFBO";
    const string SSAOBlurFBOName = "SSAOBlurFBO";
    const string GBufferFBOName = "GBufferFBO";
    const string LightCombineFBOName = "LightCombineFBO";
    const string ForwardPassFBOName = "ForwardPassFBO";
    const string PostProcessFBOName = "PostProcessFBO";
    const string UserInterfaceFBOName = "UserInterfaceFBO";

    //Textures
    const string SSAONoiseTextureName = "SSAONoiseTexture";
    const string SSAOIntensityTextureName = "SSAOIntensityTexture";
    const string NormalTextureName = "Normal";
    const string DepthViewTextureName = "DepthView";
    const string StencilViewTextureName = "StencilView";
    const string AlbedoOpacityTextureName = "AlbedoOpacity";
    const string RMSITextureName = "RMSI";
    const string DepthStencilTextureName = "DepthStencil";
    const string DiffuseTextureName = "LightingTexture";
    const string HDRSceneTextureName = "HDRSceneTex";
    const string BloomBlurTextureName = "BloomBlurTexture";
    const string HUDTextureName = "HUDTex";
    const string BRDFTextureName = "BRDF";

    protected override ViewportRenderCommandContainer GenerateCommandChain()
    {
        ViewportRenderCommandContainer c = [];
        var ifElse = c.Add<VPRC_IfElse>();
        ifElse.ConditionEvaluator = () => State.WindowViewport is not null;
        ifElse.TrueCommands = CreateViewportTargetCommands();
        ifElse.FalseCommands = CreateFBOTargetCommands();
        return c;
    }

    public static ViewportRenderCommandContainer CreateFBOTargetCommands()
    {
        ViewportRenderCommandContainer c = [];

        c.Add<VPRC_SetClears>().Set(ColorF4.Black, 1.0f, 0);
        c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.PreRender;

        using (c.AddUsing<VPRC_PushOutputFBORenderArea>())
        {
            using (c.AddUsing<VPRC_BindOutputFBO>())
            {
                c.Add<VPRC_StencilMask>().Set(~0u);
                c.Add<VPRC_ClearByBoundFBO>();
                c.Add<VPRC_DepthTest>().Enable = true;
                c.Add<VPRC_DepthWrite>().Allow = false;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.Background;
                c.Add<VPRC_DepthWrite>().Allow = true;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.OpaqueDeferredLit;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.OpaqueForward;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.TransparentForward;
                c.Add<VPRC_DepthFunc>().Comp = EComparison.Always;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.OnTopForward;
            }
        }
        c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.PostRender;
        return c;
    }

    private ViewportRenderCommandContainer CreateViewportTargetCommands()
    {
        ViewportRenderCommandContainer c = [];

        CacheTextures(c);

        //Create FBOs only after all their texture dependencies have been cached.

        c.Add<VPRC_CacheOrCreateFBO>().SetOptions(
            UserInterfaceFBOName,
            CreateUserInterfaceFBO,
            GetDesiredFBOSizeFull);

        c.Add<VPRC_SetClears>().Set(ColorF4.Black, 1.0f, 0);
        c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.PreRender;

        using (c.AddUsing<VPRC_PushViewportRenderArea>(t => t.UseInternalResolution = true))
        {
            //Render to the SSAO FBO
            var ssaoCommand = c.Add<VPRC_SSAO>();
            ssaoCommand.SetGBufferInputTextureNames(NormalTextureName, DepthViewTextureName, AlbedoOpacityTextureName, RMSITextureName, DepthStencilTextureName);
            ssaoCommand.SetOutputNames(SSAONoiseTextureName, SSAOIntensityTextureName, SSAOFBOName, SSAOBlurFBOName, GBufferFBOName);

            using (c.AddUsing<VPRC_BindFBOByName>(x => x.FrameBufferName = SSAOFBOName))
            {
                c.Add<VPRC_StencilMask>().Set(~0u);
                c.Add<VPRC_ClearByBoundFBO>();

                c.Add<VPRC_DepthTest>().Enable = true;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.OpaqueDeferredLit;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.DeferredDecals;
            }

            c.Add<VPRC_DepthTest>().Enable = false;
            c.Add<VPRC_BlitFBO>().SetTargets(SSAOFBOName, SSAOBlurFBOName);
            c.Add<VPRC_BlitFBO>().SetTargets(SSAOBlurFBOName, GBufferFBOName);

            //LightCombine FBO
            c.Add<VPRC_CacheOrCreateFBO>().SetOptions(
                LightCombineFBOName,
                CreateLightCombineFBO,
                GetDesiredFBOSizeInternal);

            //Render the GBuffer fbo to the LightCombine fbo
            using (c.AddUsing<VPRC_BindFBOByName>(x => x.FrameBufferName = LightCombineFBOName))
            {
                c.Add<VPRC_StencilMask>().Set(~0u);
                c.Add<VPRC_ClearByBoundFBO>();

                c.Add<VPRC_LightCombinePass>().SetOptions(AlbedoOpacityTextureName, NormalTextureName, RMSITextureName, DepthViewTextureName);
            }

            //ForwardPass FBO
            c.Add<VPRC_CacheOrCreateFBO>().SetOptions(
                ForwardPassFBOName,
                CreateForwardPassFBO,
                GetDesiredFBOSizeInternal);

            //Render the LightCombine fbo to the ForwardPass fbo
            using (c.AddUsing<VPRC_BindFBOByName>(x => x.FrameBufferName = ForwardPassFBOName))
            {
                //Render the deferred pass lighting result, no depth testing
                c.Add<VPRC_DepthTest>().Enable = false;
                c.Add<VPRC_BlitFBO>().SourceQuadFBOName = LightCombineFBOName;

                //No depth writing for backgrounds (skybox)
                c.Add<VPRC_DepthTest>().Enable = false;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.Background;

                //Normal depth test for opaque forward
                c.Add<VPRC_DepthTest>().Enable = true;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.OpaqueForward;

                //Render forward transparent objects next, normal depth testing
                //c.Add<VPRC_DepthTest>().Enable = true;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.TransparentForward;

                //Render forward on-top objects last
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.OnTopForward;
            }

            c.Add<VPRC_DepthTest>().Enable = false;

            c.Add<VPRC_BloomPass>().SetTargetFBONames(
                ForwardPassFBOName,
                BloomBlurTextureName);

            //PostProcess FBO
            //This FBO is created here because it relies on BloomBlurTextureName, which is created in the BloomPass.
            c.Add<VPRC_CacheOrCreateFBO>().SetOptions(
                PostProcessFBOName,
                CreatePostProcessFBO,
                GetDesiredFBOSizeInternal);

            c.Add<VPRC_ExposureUpdate>().SetOptions(HDRSceneTextureName, true);
        }
        using (c.AddUsing<VPRC_PushViewportRenderArea>(t => t.UseInternalResolution = false))
        {
            using (c.AddUsing<VPRC_BindOutputFBO>())
            {
                c.Add<VPRC_StencilMask>().Set(~0u);
                c.Add<VPRC_ClearByBoundFBO>();

                c.Add<VPRC_DepthFunc>().Comp = EComparison.Always;
                c.Add<VPRC_DepthWrite>().Allow = false;

                c.Add<VPRC_RenderQuadFBO>().FrameBufferName = PostProcessFBOName;
                c.Add<VPRC_RenderUI>().UserInterfaceFBOName = UserInterfaceFBOName;
            }
        }
        c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)EDefaultRenderPass.PostRender;
        return c;
    }

    private XRFrameBuffer CreateUserInterfaceFBO()
    {
        var hudTexture = GetTexture<XRTexture2D>(HUDTextureName)!;
        XRShader hudShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "HudFBO.fs"), EShaderType.Fragment);
        XRTexture2D[] hudRefs = { hudTexture };
        XRMaterial hudMat = new(hudRefs, hudShader)
        {
            RenderOptions = new RenderingParameters()
            {
                DepthTest = new()
                {
                    Enabled = ERenderParamUsage.Unchanged,
                    Function = EComparison.Always,
                    UpdateDepth = false,
                },
            }
        };
        var uiFBO = new XRQuadFrameBuffer(hudMat);
        uiFBO.SetRenderTargets((hudTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));
        return uiFBO;
    }

    XRTexture CreateBRDFTexture()
        => PrecomputeBRDF();

    XRTexture CreateDepthStencilTexture()
    {
        var dsTex = XRTexture2D.CreateFrameBufferTexture(InternalWidth, InternalHeight,
            EPixelInternalFormat.Depth24Stencil8,
            EPixelFormat.DepthStencil,
            EPixelType.UnsignedInt248,
            EFrameBufferAttachment.DepthStencilAttachment);
        dsTex.MinFilter = ETexMinFilter.Nearest;
        dsTex.MagFilter = ETexMagFilter.Nearest;
        dsTex.Resizable = false;
        dsTex.Name = DepthStencilTextureName;
        dsTex.SizedInternalFormat = ESizedInternalFormat.Depth24Stencil8;
        return dsTex;
    }

    XRTexture CreateDepthViewTexture()
        => new XRTexture2DView(
            GetTexture<XRTexture2D>(DepthStencilTextureName)!,
            0, 1,
            EPixelInternalFormat.Depth24Stencil8,
            false, false)
        {
            DepthStencilViewFormat = EDepthStencilFmt.Depth,
            Name = DepthViewTextureName,
        };

    XRTexture CreateStencilViewTexture()
        => new XRTexture2DView(
            GetTexture<XRTexture2D>(DepthStencilTextureName)!,
            0, 1,
            EPixelInternalFormat.Depth24Stencil8,
            false, false)
        {
            DepthStencilViewFormat = EDepthStencilFmt.Stencil,
            Name = StencilViewTextureName,
        };

    XRTexture CreateAlbedoOpacityTexture()
    {
        var tex = XRTexture2D.CreateFrameBufferTexture(
            InternalWidth, InternalHeight,
            EPixelInternalFormat.Rgba16f,
            EPixelFormat.Rgba,
            EPixelType.HalfFloat);
        tex.MinFilter = ETexMinFilter.Nearest;
        tex.MagFilter = ETexMagFilter.Nearest;
        tex.Name = AlbedoOpacityTextureName;
        return tex;
    }

    XRTexture CreateNormalTexture()
    {
        var tex = XRTexture2D.CreateFrameBufferTexture(
            InternalWidth, InternalHeight,
            EPixelInternalFormat.Rgb16f,
            EPixelFormat.Rgb,
            EPixelType.HalfFloat);
        tex.MinFilter = ETexMinFilter.Nearest;
        tex.MagFilter = ETexMagFilter.Nearest;
        tex.Name = NormalTextureName;
        return tex;
    }

    XRTexture CreateRMSITexture() =>
        XRTexture2D.CreateFrameBufferTexture(
            InternalWidth, InternalHeight,
            EPixelInternalFormat.Rgba8,
            EPixelFormat.Rgba,
            EPixelType.UnsignedByte);

    XRTexture CreateSSAOTexture()
    {
        var ssao = XRTexture2D.CreateFrameBufferTexture(
            InternalWidth, InternalHeight,
            EPixelInternalFormat.R16f,
            EPixelFormat.Red,
            EPixelType.HalfFloat,
            EFrameBufferAttachment.ColorAttachment0);
        ssao.MinFilter = ETexMinFilter.Nearest;
        ssao.MagFilter = ETexMagFilter.Nearest;
        return ssao;
    }

    XRTexture CreateLightingTexture() =>
        XRTexture2D.CreateFrameBufferTexture(
            InternalWidth, InternalHeight,
            EPixelInternalFormat.Rgb16f,
            EPixelFormat.Rgb,
            EPixelType.HalfFloat);

    XRTexture CreateHDRSceneTexture()
    {
        var tex = XRTexture2D.CreateFrameBufferTexture(InternalWidth, InternalHeight,
            EPixelInternalFormat.Rgba16f,
            EPixelFormat.Rgba,
            EPixelType.HalfFloat,
            EFrameBufferAttachment.ColorAttachment0);
        tex.MinFilter = ETexMinFilter.Nearest;
        tex.MagFilter = ETexMagFilter.Nearest;
        tex.UWrap = ETexWrapMode.ClampToEdge;
        tex.VWrap = ETexWrapMode.ClampToEdge;
        tex.SamplerName = HDRSceneTextureName;
        tex.Name = HDRSceneTextureName;
        return tex;
    }

    private void CacheTextures(ViewportRenderCommandContainer c)
    {
        //BRDF, for PBR lighting
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            BRDFTextureName,
            CreateBRDFTexture,
            null,
            null);

        //Depth + Stencil GBuffer texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            DepthStencilTextureName,
            CreateDepthStencilTexture,
            NeedsRecreateTextureInternalSize,
            ResizeTextureInternalSize);

        //Depth view texture
        //This is a view of the depth/stencil texture that only shows the depth values.
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            DepthViewTextureName,
            CreateDepthViewTexture,
            NeedsRecreateTextureInternalSize,
            ResizeTextureInternalSize);

        //Stencil view texture
        //This is a view of the depth/stencil texture that only shows the stencil values.
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            StencilViewTextureName,
            CreateStencilViewTexture,
            NeedsRecreateTextureInternalSize,
            ResizeTextureInternalSize);

        //Albedo/Opacity GBuffer texture
        //RGB = Albedo, A = Opacity
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            AlbedoOpacityTextureName,
            CreateAlbedoOpacityTexture,
            NeedsRecreateTextureInternalSize,
            ResizeTextureInternalSize);

        //Normal GBuffer texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            NormalTextureName,
            CreateNormalTexture,
            NeedsRecreateTextureInternalSize,
            ResizeTextureInternalSize);

        //RMSI GBuffer texture
        //R = Roughness, G = Metallic, B = Specular, A = IOR
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            RMSITextureName,
            CreateRMSITexture,
            NeedsRecreateTextureInternalSize,
            ResizeTextureInternalSize);

        //SSAO FBO texture, this is created later by the SSAO command
        //c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
        //    SSAOIntensityTextureName,
        //    CreateSSAOTexture,
        //    NeedsRecreateTextureInternalSize,
        //    ResizeTextureInternalSize);

        //Lighting texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            DiffuseTextureName,
            CreateLightingTexture,
            NeedsRecreateTextureInternalSize,
            ResizeTextureInternalSize);

        //HDR Scene texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            HDRSceneTextureName,
            CreateHDRSceneTexture,
            NeedsRecreateTextureInternalSize,
            ResizeTextureInternalSize);

        //HUD texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            HUDTextureName,
            CreateHUDTexture,
            NeedsRecreateTextureFullSize,
            ResizeTextureFullSize);
    }

    private XRTexture CreateHUDTexture()
    {
        var hudTexture = XRTexture2D.CreateFrameBufferTexture(
            (uint)State.WindowViewport!.Width,
            (uint)State.WindowViewport!.Height,
            EPixelInternalFormat.Rgba16f,
            EPixelFormat.Rgba,
            EPixelType.HalfFloat);
        hudTexture.MinFilter = ETexMinFilter.Nearest;
        hudTexture.MagFilter = ETexMagFilter.Nearest;
        hudTexture.UWrap = ETexWrapMode.ClampToEdge;
        hudTexture.VWrap = ETexWrapMode.ClampToEdge;
        hudTexture.SamplerName = HUDTextureName;
        return hudTexture;
    }

    private XRFrameBuffer CreatePostProcessFBO()
    {
        XRTexture[] postProcessRefs =
        [
            GetTexture<XRTexture2D>(HDRSceneTextureName)!,
            GetTexture<XRTexture2D>(BloomBlurTextureName)!,
            GetTexture<XRTexture2DView>(DepthViewTextureName)!,
            GetTexture<XRTexture2DView>(StencilViewTextureName)!,
            GetTexture<XRTexture2D>(HUDTextureName)!,
        ];
        XRShader postProcessShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "PostProcess.fs"), EShaderType.Fragment);
        XRMaterial postProcessMat = new(postProcessRefs, postProcessShader)
        {
            RenderOptions = new RenderingParameters()
            {
                DepthTest = new DepthTest()
                {
                    Enabled = ERenderParamUsage.Unchanged,
                    Function = EComparison.Always,
                    UpdateDepth = false,
                },
            }
        };
        var PostProcessFBO = new XRQuadFrameBuffer(postProcessMat);
        PostProcessFBO.SettingUniforms += PostProcess_SettingUniforms;
        return PostProcessFBO;
    }
    private void PostProcess_SettingUniforms(XRRenderProgram program)
    {
        var sceneCam = PipelineState?.SceneCamera;
        if (sceneCam is null)
            return;

        sceneCam.SetUniforms(program);
        sceneCam.SetPostProcessUniforms(program);
    }

    private XRFrameBuffer CreateForwardPassFBO()
    {
        XRTexture2D[] brightRefs = { GetTexture<XRTexture2D>(HDRSceneTextureName)! };
        XRMaterial brightMat = new(
            [
                new ShaderFloat(1.0f, "BloomIntensity"),
                new ShaderFloat(1.0f, "BloomThreshold"),
                new ShaderFloat(0.5f, "SoftKnee"),
                new ShaderVector3(Engine.Rendering.Settings.DefaultLuminance, "Luminance") 
            ],
            brightRefs,
            XRShader.EngineShader(Path.Combine(SceneShaderPath, "BrightPass.fs"), EShaderType.Fragment))
        {
            RenderOptions = new RenderingParameters()
            {
                DepthTest = new()
                {
                    Enabled = ERenderParamUsage.Unchanged,
                    Function = EComparison.Always,
                    UpdateDepth = false,
                },
            }
        };

        var fbo = new XRQuadFrameBuffer(brightMat);
        fbo.SettingUniforms += BrightPassFBO_SettingUniforms;
        fbo.SetRenderTargets(
            (GetTexture<XRTexture2D>(HDRSceneTextureName)!, EFrameBufferAttachment.ColorAttachment0, 0, -1),
            (GetTexture<XRTexture2D>(DepthStencilTextureName)!, EFrameBufferAttachment.DepthStencilAttachment, 0, -1));
        
        return fbo;
    }


    private void BrightPassFBO_SettingUniforms(XRRenderProgram program)
    {
        var sceneCam = PipelineState?.SceneCamera;
        if (sceneCam is null)
            return;

        sceneCam.SetBloomBrightPassUniforms(program);
    }

    private XRFrameBuffer CreateLightCombineFBO()
    {
        var diffuseTexture = GetTexture<XRTexture2D>(DiffuseTextureName)!;

        XRTexture[] lightCombineTextures = [
            GetTexture<XRTexture2D>(AlbedoOpacityTextureName)!,
            GetTexture<XRTexture2D>(NormalTextureName)!,
            GetTexture<XRTexture2D>(RMSITextureName)!,
            GetTexture<XRTexture2D>(SSAOIntensityTextureName)!,
            GetTexture<XRTexture2DView>(DepthViewTextureName)!,
            diffuseTexture,
            GetTexture<XRTexture2D>(BRDFTextureName)!,
            //irradiance
            //prefilter
        ];
        XRShader lightCombineShader = XRShader.EngineShader(Path.Combine(SceneShaderPath, "DeferredLightCombine.fs"), EShaderType.Fragment);
        XRMaterial lightCombineMat = new(lightCombineTextures, lightCombineShader)
        {
            RenderOptions = new RenderingParameters()
            {
                DepthTest = new()
                {
                    Enabled = ERenderParamUsage.Unchanged,
                    Function = EComparison.Always,
                    UpdateDepth = false,
                },
            }
        };

        var lightCombineFBO = new XRQuadFrameBuffer(lightCombineMat) { Name = LightCombineFBOName };
        lightCombineFBO.SetRenderTargets((diffuseTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));
        lightCombineFBO.SettingUniforms += LightCombineFBO_SettingUniforms;
        return lightCombineFBO;
    }

    private void LightCombineFBO_SettingUniforms(XRRenderProgram program)
    {
        var sceneCam = PipelineState?.SceneCamera;
        if (sceneCam is null)
            return;
        
        sceneCam.SetUniforms(program);

        if (RenderingScene is not VisualScene3D scene)
            return;

        //var lightProbes = scene.Lights.GetNearestProbes(/*program.LightProbeTransform?.WorldTranslation ?? */Vector3.Zero);
        //if (lightProbes.Length == 0)
        //    return;

        //LightProbeComponent probe = lightProbes[0];
        if (scene.Lights.LightProbes.Count == 0)
            return;

        LightProbeComponent probe = scene.Lights.LightProbes[0];

        int baseCount = GetFBO<XRQuadFrameBuffer>(LightCombineFBOName)?.Material?.Textures?.Count ?? 0;

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

    //private void RenderToViewport(VisualScene visualScene, XRCamera camera, XRViewport viewport, XRFrameBuffer? target)
    //{
    //    ColorGradingSettings? cgs = RenderStatus.Camera?.PostProcessing?.ColorGrading;
    //    if (cgs != null && cgs.AutoExposure)
    //        cgs.Exposure = AbstractRenderer.Current?.CalculateDotLuminance(HDRSceneTexture!, true) ?? 1.0f;

    //    XRMaterial? postMat = RenderStatus.Camera?.PostProcessMaterial;
    //    if (postMat != null)
    //        RenderPostProcessPass(viewport, postMat);
    //}
}
