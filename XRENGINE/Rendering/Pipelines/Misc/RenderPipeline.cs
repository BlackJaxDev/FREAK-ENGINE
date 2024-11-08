﻿using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Pipelines.Commands;
using static XREngine.Engine.Rendering.State;
using static XREngine.Rendering.XRRenderPipelineInstance;

namespace XREngine.Rendering;

public abstract class RenderPipeline : XRBase
{
    public List<XRRenderPipelineInstance> Instances { get; } = [];

    protected abstract Lazy<XRMaterial> InvalidMaterialFactory { get; }
    public XRMaterial InvalidMaterial 
        => InvalidMaterialFactory.Value;

    private bool _isShadowPass;
    public bool IsShadowPass
    {
        get => _isShadowPass;
        set => SetField(ref _isShadowPass, value);
    }

    public ViewportRenderCommandContainer CommandChain { get; }
    public Dictionary<int, IComparer<RenderCommand>?> PassIndicesAndSorters { get; }

    public RenderPipeline()
    {
        CommandChain = GenerateCommandChain();
        PassIndicesAndSorters = GetPassIndicesAndSorters();
    }

    protected abstract ViewportRenderCommandContainer GenerateCommandChain();
    protected abstract Dictionary<int, IComparer<RenderCommand>?> GetPassIndicesAndSorters();

    public static RenderingState State 
        => CurrentPipeline!.State;

    public static T? GetTexture<T>(string name) where T : XRTexture
        => CurrentPipeline!.GetTexture<T>(name);

    public static bool TryGetTexture(string name, out XRTexture? texture)
        => CurrentPipeline!.TryGetTexture(name, out texture);

    public static void SetTexture(XRTexture texture)
        => CurrentPipeline!.SetTexture(texture);

    public static T? GetFBO<T>(string name) where T : XRFrameBuffer
        => CurrentPipeline!.GetFBO<T>(name);

    public static bool TryGetFBO(string name, out XRFrameBuffer? fbo)
        => CurrentPipeline!.TryGetFBO(name, out fbo);

    public static void SetFBO(XRFrameBuffer fbo)
        => CurrentPipeline!.SetFBO(fbo);

    protected static uint InternalWidth
        => (uint)State.WindowViewport!.InternalWidth;
    protected static uint InternalHeight
        => (uint)State.WindowViewport!.InternalHeight;
    protected static uint FullWidth
        => (uint)State.WindowViewport!.Width;
    protected static uint FullHeight
        => (uint)State.WindowViewport!.Height;

    protected static bool NeedsRecreateTextureInternalSize(XRTexture t)
        => t is XRTexture2D t2d && (t2d.Width != InternalWidth || t2d.Height != InternalHeight);
    protected static bool NeedsRecreateTextureFullSize(XRTexture t)
        => t is XRTexture2D t2d && (t2d.Width != FullWidth || t2d.Height != FullHeight);

    protected static void ResizeTextureInternalSize(XRTexture t)
    {
        switch (t)
        {
            case XRTexture2D t2d:
                if (t2d.Resizable)
                    t2d.Resize(InternalWidth, InternalHeight);
                else
                    t2d.Destroy();
                break;
        }
    }
    protected static void ResizeTextureFullSize(XRTexture t)
    {
        switch (t)
        {
            case XRTexture2D t2d:
                if (t2d.Resizable)
                    t2d.Resize(FullWidth, FullHeight);
                else
                    t2d.Destroy();
                break;
        }
    }

    protected static (uint x, uint y) GetDesiredFBOSizeInternal()
        => (InternalWidth, InternalHeight);
    protected static (uint x, uint y) GetDesiredFBOSizeFull()
        => ((uint)State.WindowViewport!.Width, (uint)State.WindowViewport!.Height);

    /// <summary>
    /// Creates a texture used by PBR shading to light an opaque surface.
    /// Input is an incoming light direction and an outgoing direction (calculated using the normal)
    /// Output from this texture is ratio of refleced radiance in the outgoing direction to irradiance from the incoming direction.
    /// https://en.wikipedia.org/wiki/Bidirectional_reflectance_distribution_function
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static XRTexture2D PrecomputeBRDF(uint width = 2048, uint height = 2048)
    {
        XRTexture2D brdf = new(
            width, height,
            EPixelInternalFormat.RG16f,
            EPixelFormat.Rg,
            EPixelType.HalfFloat,
            false)
        {
            UWrap = ETexWrapMode.ClampToEdge,
            VWrap = ETexWrapMode.ClampToEdge,
            MinFilter = ETexMinFilter.Linear,
            MagFilter = ETexMagFilter.Linear,
            SamplerName = "BRDF",
            Name = "BRDF",
            Resizable = true,
            SizedInternalFormat = ESizedInternalFormat.Rg16f
        };

        XRShader shader = XRShader.EngineShader(Path.Combine("Scene3D", "BRDF.fs"), EShaderType.Fragment);
        XRMaterial mat = new(shader)
        {
            RenderOptions = new()
            {
                DepthTest = new()
                {
                    Enabled = ERenderParamUsage.Disabled,
                    Function = EComparison.Always,
                    UpdateDepth = false,
                },
            }
        };

        XRQuadFrameBuffer fbo = new(mat, false);
        fbo.SetRenderTargets((brdf, EFrameBufferAttachment.ColorAttachment0, 0, -1));
        BoundingRectangle region = new(IVector2.Zero, new IVector2((int)width, (int)height));

        //Now render the texture to the FBO using the quad
        using (fbo.BindForWriting())
        {
            using (State.PushRenderArea(region))
            {
                //ClearColor(new ColorF4(0.0f, 0.0f, 0.0f, 1.0f));
                //Clear(true, true, false);
                fbo.Render();
            }
        }
        return brdf;
    }
}
