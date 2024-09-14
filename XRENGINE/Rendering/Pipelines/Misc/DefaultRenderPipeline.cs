﻿using System.Numerics;
using XREngine.Components.Lights;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Pipelines.Commands;
using XREngine.Scene;
using static XREngine.Engine.Rendering.State;

namespace XREngine.Rendering;

public class DefaultRenderPipeline : XRRenderPipeline
{
    protected override Lazy<XRMaterial> LazyInvalidMaterial => new(XRMaterial.CreateColorMaterialDeferred);

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
    const string SSAOFBOTextureName = "SSAOFBOTexture";
    const string NormalTextureName = "Normal";
    const string DepthViewTextureName = "DepthView";
    const string StencilViewTextureName = "StencilView";
    const string AlbedoOpacityTextureName = "AlbedoOpacity";
    const string RMSITextureName = "RMSI";
    const string DepthStencilTextureName = "DepthStencil";
    const string LightingTextureName = "LightingTexture";
    const string HDRSceneTextureName = "HDRSceneTexture";
    const string BloomBlurTextureName = "BloomBlurTexture";
    const string HUDTextureName = "HUDTexture";
    const string BRDFTextureName = "BRDF";

    protected override ViewportRenderCommandContainer GenerateCommandChainInternal()
    {
        ViewportRenderCommandContainer c = new(this);
        var ifElse = c.Add<VPRC_IfElse>();
        ifElse.ConditionEvaluator = () => RenderStatus.Viewport is not null;
        ifElse.TrueCommands = CreateViewportTargetCommands();
        ifElse.FalseCommands = CreateFBOTargetCommands();
        return c;
    }

    private ViewportRenderCommandContainer CreateFBOTargetCommands()
    {
        ViewportRenderCommandContainer c = new(this);
        using (c.AddUsing<VPRC_PushOutputFBORenderArea>())
        {
            using (c.AddUsing<VPRC_BindOutputFBO>())
            {
                c.Add<VPRC_Manual>().ManualAction = () =>
                {
                    ClearDepth(1.0f);
                    EnableDepthTest(true);
                    AllowDepthWrite(true);
                    StencilMask(~0u);
                    ClearStencil(0);
                    //Clear(/*target?.TextureTypes ?? */EFrameBufferTextureTypeFlags.All);
                    Clear(true, true, true);
                    AllowDepthWrite(false);
                };
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.Background;

                c.Add<VPRC_Manual>().ManualAction = () => AllowDepthWrite(true);
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.OpaqueDeferredLit;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.OpaqueForward;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.TransparentForward;

                c.Add<VPRC_Manual>().ManualAction = () => DepthFunc(EComparison.Always);
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.OnTopForward;
            }
        }
        return c;
    }

    private ViewportRenderCommandContainer CreateViewportTargetCommands()
    {
        ViewportRenderCommandContainer c = new(this);
        CacheTextures(c);

        //Create FBOs only after all their texture dependencies have been cached.

        //LightCombine FBO
        c.Add<VPRC_CacheOrCreateFBO>().SetOptions(
            LightCombineFBOName,
            CreateLightCombineFBO,
            GetDesiredFBOSizeInternal);

        //ForwardPass FBO
        c.Add<VPRC_CacheOrCreateFBO>().SetOptions(
            ForwardPassFBOName,
            CreateForwardPassFBO,
            GetDesiredFBOSizeInternal);

        c.Add<VPRC_CacheOrCreateFBO>().SetOptions(
            UserInterfaceFBOName,
            CreateUserInterfaceFBO,
            GetDesiredFBOSizeFull);

        using (c.AddUsing<VPRC_PushViewportRenderArea>(t => t.UseInternalResolution = true))
        {
            //Render to the SSAO FBO
            var ssaoCommand = c.Add<VPRC_SSAO>();
            ssaoCommand.SetGBufferInputTextureNames(NormalTextureName, DepthViewTextureName, AlbedoOpacityTextureName, RMSITextureName, DepthStencilTextureName);
            ssaoCommand.SetOutputNames(SSAONoiseTextureName, SSAOFBOTextureName, SSAOFBOName, SSAOBlurFBOName, GBufferFBOName);

            using (c.AddUsing<VPRC_BindFBOByName>(x => x.FrameBufferName = SSAOFBOName))
            {
                c.Add<VPRC_Manual>().ManualAction = () =>
                {
                    StencilMask(~0u);
                    ClearStencil(0);
                    Clear(true, true, true);
                    EnableDepthTest(true);
                    ClearDepth(1.0f);
                };
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.OpaqueDeferredLit;
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.DeferredDecals;
                c.Add<VPRC_Manual>().ManualAction = () => EnableDepthTest(false);
            }

            //Render the SSAO fbo to the SSAO blur fbo
            c.Add<VPRC_BlitFBO>().SetTargets(SSAOFBOName, SSAOBlurFBOName);

            //Render the SSAO blur fbo to the GBuffer fbo
            c.Add<VPRC_BlitFBO>().SetTargets(SSAOBlurFBOName, GBufferFBOName);

            //Render the GBuffer fbo to the LightCombine fbo
            using (c.AddUsing<VPRC_BindFBOByName>(x => x.FrameBufferName = LightCombineFBOName))
            {
                c.Add<VPRC_Manual>().ManualAction = () => Clear(true, false, false);
                c.Add<VPRC_LightCombinePass>().SetOptions(AlbedoOpacityTextureName, NormalTextureName, RMSITextureName, DepthViewTextureName);
            }

            //Render the LightCombine fbo to the ForwardPass fbo
            using (c.AddUsing<VPRC_BindFBOByName>(x => x.FrameBufferName = ForwardPassFBOName))
            {
                //Render the deferred pass lighting result, no depth testing
                c.Add<VPRC_Manual>().ManualAction = () => EnableDepthTest(false);
                c.Add<VPRC_BlitFBO>().SourceQuadFBOName = LightCombineFBOName;

                //Normal depth test for opaque forward
                c.Add<VPRC_Manual>().ManualAction = () => EnableDepthTest(true);
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.OpaqueForward;

                //No depth writing for backgrounds (skybox)
                c.Add<VPRC_Manual>().ManualAction = () => EnableDepthTest(false);
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.Background;

                //Render forward transparent objects next, normal depth testing
                c.Add<VPRC_Manual>().ManualAction = () => EnableDepthTest(true);
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.TransparentForward;

                //Render forward on-top objects last
                c.Add<VPRC_RenderMeshesPass>().RenderPass = (int)ERenderPass.OnTopForward;
                c.Add<VPRC_Manual>().ManualAction = () => EnableDepthTest(false);
            }

            c.Add<VPRC_BloomPass>().SetTargetFBONames(
                HDRSceneTextureName,
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
                c.Add<VPRC_RenderQuadFBO>().FrameBufferName = PostProcessFBOName;
            }
        }
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
        var UserInterfaceFBO = new XRQuadFrameBuffer(hudMat);
        UserInterfaceFBO.SetRenderTargets((hudTexture, EFrameBufferAttachment.ColorAttachment0, 0, -1));
        return UserInterfaceFBO;
    }

    private uint w => (uint)RenderStatus.Viewport!.InternalWidth;
    private uint h => (uint)RenderStatus.Viewport!.InternalHeight;

    private bool NeedsRecreateTextureInternalSize(XRTexture t)
    {
        var t2d = t as XRTexture2D ?? throw new Exception("Must be a 2D texture.");
        return t2d.Width != w || t2d.Height != h;
    }
    private bool NeedsRecreateTextureFullSize(XRTexture t)
    {
        var t2d = t as XRTexture2D ?? throw new Exception("Must be a 2D texture.");
        uint w2 = (uint)RenderStatus.Viewport!.Width;
        uint h2 = (uint)RenderStatus.Viewport!.Height;
        return t2d.Width != w2 || t2d.Height != h2;
    }

    private (uint x, uint y) GetDesiredFBOSizeInternal() => (w, h);

    private (uint x, uint y) GetDesiredFBOSizeFull() => ((uint)RenderStatus.Viewport!.Width, (uint)RenderStatus.Viewport!.Height);

    XRTexture CreateBRDFTexture()
        => PrecomputeBRDF(2048, 2048);

    XRTexture CreateDepthStencilTexture()
    {
        var dsTex = XRTexture2D.CreateFrameBufferTexture(w, h,
            EPixelInternalFormat.Depth24Stencil8,
            EPixelFormat.DepthStencil,
            EPixelType.UnsignedInt248,
            EFrameBufferAttachment.DepthStencilAttachment);
        dsTex.MinFilter = ETexMinFilter.Nearest;
        dsTex.MagFilter = ETexMagFilter.Nearest;
        dsTex.Resizable = false;
        return dsTex;
    }

    XRTexture CreateDepthViewTexture()
        => new XRTextureView2D(
            GetTexture<XRTexture2D>(DepthStencilTextureName)!,
            0, 1, 0, 1,
            EPixelType.UnsignedInt248,
            EPixelFormat.DepthStencil,
            EPixelInternalFormat.Depth24Stencil8)
        {
            Resizable = false,
            DepthStencilFormat = EDepthStencilFmt.Depth,
            MinFilter = ETexMinFilter.Nearest,
            MagFilter = ETexMagFilter.Nearest,
            UWrap = ETexWrapMode.ClampToEdge,
            VWrap = ETexWrapMode.ClampToEdge,
        };

    XRTexture CreateStencilViewTexture()
        => new XRTextureView2D(
            GetTexture<XRTexture2D>(DepthStencilTextureName)!,
            0, 1, 0, 1,
            EPixelType.UnsignedInt248,
            EPixelFormat.DepthStencil,
            EPixelInternalFormat.Depth24Stencil8)
        {
            Resizable = false,
            DepthStencilFormat = EDepthStencilFmt.Stencil,
            MinFilter = ETexMinFilter.Nearest,
            MagFilter = ETexMagFilter.Nearest,
            UWrap = ETexWrapMode.ClampToEdge,
            VWrap = ETexWrapMode.ClampToEdge,
        };

    XRTexture CreateAlbedoOpacityTexture() =>
        XRTexture2D.CreateFrameBufferTexture(w, h,
            EPixelInternalFormat.Rgba16f,
            EPixelFormat.Rgba,
            EPixelType.HalfFloat);

    XRTexture CreateNormalTexture() =>
        XRTexture2D.CreateFrameBufferTexture(w, h,
            EPixelInternalFormat.Rgba16f,
            EPixelFormat.Rgba,
            EPixelType.HalfFloat);

    XRTexture CreateRMSITexture() =>
        XRTexture2D.CreateFrameBufferTexture(w, h,
            EPixelInternalFormat.Rgba8,
            EPixelFormat.Rgba,
            EPixelType.UnsignedByte);

    XRTexture CreateSSAOTexture()
    {
        var ssao = XRTexture2D.CreateFrameBufferTexture(w, h,
            EPixelInternalFormat.R16f,
            EPixelFormat.Red,
            EPixelType.HalfFloat);
        ssao.MinFilter = ETexMinFilter.Nearest;
        ssao.MagFilter = ETexMagFilter.Nearest;
        return ssao;
    }

    XRTexture CreateLightingTexture() =>
        XRTexture2D.CreateFrameBufferTexture(w, h,
            EPixelInternalFormat.Rgb16f,
            EPixelFormat.Rgb,
            EPixelType.HalfFloat);

    XRTexture CreateHDRSceneTexture()
    {
        var tex = XRTexture2D.CreateFrameBufferTexture(w, h,
            EPixelInternalFormat.Rgba16f,
            EPixelFormat.Rgba,
            EPixelType.HalfFloat,
            EFrameBufferAttachment.ColorAttachment0);
        tex.MinFilter = ETexMinFilter.Nearest;
        tex.MagFilter = ETexMagFilter.Nearest;
        tex.UWrap = ETexWrapMode.ClampToEdge;
        tex.VWrap = ETexWrapMode.ClampToEdge;
        tex.SamplerName = HDRSceneTextureName;
        return tex;
    }

    private void CacheTextures(ViewportRenderCommandContainer c)
    {
        //BRDF, for PBR lighting
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            BRDFTextureName,
            CreateBRDFTexture,
            null);

        //Depth + Stencil GBuffer texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            DepthStencilTextureName,
            CreateDepthStencilTexture,
            NeedsRecreateTextureInternalSize);

        //Depth view texture
        //This is a view of the depth/stencil texture that only shows the depth values.
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            DepthViewTextureName,
            CreateDepthViewTexture,
            NeedsRecreateTextureInternalSize);

        //Stencil view texture
        //This is a view of the depth/stencil texture that only shows the stencil values.
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            StencilViewTextureName,
            CreateStencilViewTexture,
            NeedsRecreateTextureInternalSize);

        //Albedo/Opacity GBuffer texture
        //RGB = Albedo, A = Opacity
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            AlbedoOpacityTextureName,
            CreateAlbedoOpacityTexture,
            NeedsRecreateTextureInternalSize);

        //Normal GBuffer texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            NormalTextureName,
            CreateNormalTexture,
            NeedsRecreateTextureInternalSize);

        //RMSI GBuffer texture
        //R = Roughness, G = Metallic, B = Specular, A = IOR
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            RMSITextureName,
            CreateRMSITexture,
            NeedsRecreateTextureInternalSize);

        //SSAO FBO texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            SSAOFBOTextureName,
            CreateSSAOTexture,
            NeedsRecreateTextureInternalSize);

        //Lighting texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            LightingTextureName,
            CreateLightingTexture,
            NeedsRecreateTextureInternalSize);

        //HDR Scene texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            HDRSceneTextureName,
            CreateHDRSceneTexture,
            NeedsRecreateTextureInternalSize);

        //HUD texture
        c.Add<VPRC_CacheOrCreateTexture>().SetOptions(
            HUDTextureName,
            () =>
            {
                var hudTexture = XRTexture2D.CreateFrameBufferTexture(
                    (uint)RenderStatus.Viewport!.Width,
                    (uint)RenderStatus.Viewport!.Height,
                    EPixelInternalFormat.Rgba16f,
                    EPixelFormat.Rgba,
                    EPixelType.HalfFloat);
                hudTexture.MinFilter = ETexMinFilter.Nearest;
                hudTexture.MagFilter = ETexMagFilter.Nearest;
                hudTexture.UWrap = ETexWrapMode.ClampToEdge;
                hudTexture.VWrap = ETexWrapMode.ClampToEdge;
                hudTexture.SamplerName = HUDTextureName;
                return hudTexture;
            },
            NeedsRecreateTextureFullSize);
    }

    private XRFrameBuffer CreatePostProcessFBO()
    {
        XRTexture2D[] postProcessRefs =
        [
            GetTexture<XRTexture2D>(HDRSceneTextureName)!,
            GetTexture<XRTexture2D>(BloomBlurTextureName)!,
            GetTexture<XRTexture2D>(DepthViewTextureName)!,
            GetTexture<XRTexture2D>(StencilViewTextureName)!,
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

    private XRFrameBuffer CreateForwardPassFBO()
    {
        XRTexture2D[] brightRefs = { GetTexture<XRTexture2D>(HDRSceneTextureName)! };
        XRMaterial brightMat = new(
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
        => RenderingCamera?.SetBloomUniforms(program);

    private XRFrameBuffer CreateLightCombineFBO()
    {
        XRMaterial lightCombineMat = new([
            GetTexture<XRTexture2D>(AlbedoOpacityTextureName)!,
            GetTexture<XRTexture2D>(NormalTextureName)!,
            GetTexture<XRTexture2D>(RMSITextureName)!,
            GetTexture<XRTexture2D>(SSAOFBOTextureName)!,
            GetTexture<XRTextureView2D>(DepthViewTextureName)!,
            GetTexture<XRTexture2D>(LightingTextureName)!,
            GetTexture<XRTexture2D>(BRDFTextureName)!,
            //irradiance
            //prefilter
        ], XRShader.EngineShader(Path.Combine(SceneShaderPath, "DeferredLightCombine.fs"), EShaderType.Fragment))
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
        lightCombineFBO.SetRenderTargets((GetTexture<XRTexture2D>(LightingTextureName)!, EFrameBufferAttachment.ColorAttachment0, 0, -1));
        lightCombineFBO.SettingUniforms += LightCombineFBO_SettingUniforms;
        return lightCombineFBO;
    }

    private void LightCombineFBO_SettingUniforms(XRRenderProgram program)
    {
        if (RenderingCamera is null)
            return;

        RenderingCamera.SetUniforms(program);

        if (RenderingWorld?.VisualScene is not VisualScene3D scene)
            return;

        var lightProbes = scene.Lights.GetNearestProbes(/*program.LightProbeTransform?.WorldTranslation ?? */Vector3.Zero);
        if (lightProbes.Length == 0)
            return;

        LightProbeComponent probe = lightProbes[0];
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

    private void PostProcess_SettingUniforms(XRRenderProgram program)
    {
        if (RenderingCamera is null)
            return;

        RenderingCamera.SetUniforms(program);
        RenderingCamera.SetPostProcessUniforms(program);
    }

}