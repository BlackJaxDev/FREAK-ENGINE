using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Pipelines.Commands;
using static XREngine.Rendering.XRRenderPipelineInstance;
using static XREngine.Engine.Rendering.State;
using XREngine.Data.Core;

namespace XREngine.Rendering;

public abstract class RenderPipeline : XRBase
{
    protected abstract Lazy<XRMaterial> InvalidMaterialFactory { get; }
    public XRMaterial InvalidMaterial 
        => InvalidMaterialFactory.Value;

    public abstract string GetUserInterfaceFBOName();

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

    public static RenderingStatus RenderStatus 
        => CurrentPipeline!.RenderStatus;

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
        XRTexture2D brdf = XRTexture2D.CreateFrameBufferTexture(
            width, height,
            EPixelInternalFormat.RG16f,
            EPixelFormat.Rgb,
            EPixelType.Float,
            EFrameBufferAttachment.ColorAttachment0);
        brdf.Resizable = true;
        brdf.UWrap = ETexWrapMode.ClampToEdge;
        brdf.VWrap = ETexWrapMode.ClampToEdge;
        brdf.MinFilter = ETexMinFilter.Linear;
        brdf.MagFilter = ETexMagFilter.Linear;
        brdf.SamplerName = "BRDF";
        brdf.Name = "BRDF";
        XRTexture2D[] texRefs = [];

        XRShader shader = XRShader.EngineShader(Path.Combine("Scene3D", "BRDF.fs"), EShaderType.Fragment);
        XRMaterial mat = new(texRefs, shader)
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

        //ndc space quad, so we don't have to load any camera matrices
        VertexTriangle[] tris = VertexQuad.Make(
                new Vector3(-1.0f, -1.0f, -0.5f),
                new Vector3(1.0f, -1.0f, -0.5f),
                new Vector3(1.0f, 1.0f, -0.5f),
                new Vector3(-1.0f, 1.0f, -0.5f),
                false, false).ToTriangles();

        using XRMaterialFrameBuffer fbo = new(mat);
        fbo.SetRenderTargets((brdf, EFrameBufferAttachment.ColorAttachment0, 0 , -1));
        fbo.Generate();

        using XRMesh data = XRMesh.Create(tris);
        using XRMeshRenderer quad = new(data, mat);
        BoundingRectangle region = new(IVector2.Zero, new IVector2((int)width, (int)height));

        //Now render the texture to the FBO using the quad
        using (fbo.BindForWriting())
        {
            using (PushRenderArea(region))
            {
                Clear(true, false, false);
                quad.Render();
            }
        }
        return brdf;
    }
}
