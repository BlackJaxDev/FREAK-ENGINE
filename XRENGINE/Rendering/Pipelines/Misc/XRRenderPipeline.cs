using Extensions;
using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Vectors;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Pipelines.Commands;
using XREngine.Scene;
using static XREngine.Engine.Rendering.State;

namespace XREngine.Rendering;

/// <summary>
/// This class is the base class for all render pipelines.
/// A render pipeline is responsible for all rendering operations to render a scene to a viewport.
/// </summary>
public abstract class XRRenderPipeline : XRBase
{
    protected abstract Lazy<XRMaterial> LazyInvalidMaterial { get; }

    /// <summary>
    /// Static global invalid material for the default deferred rendering pipeline
    /// </summary>
    public XRMaterial InvalidMaterial => LazyInvalidMaterial.Value;

    public const string SceneShaderPath = "Scene3D";

    //TODO: stereoscopic rendering output

    public XRFrameBuffer? DefaultRenderTarget { get; set; } = null;

    /// <summary>
    /// This collection contains mesh rendering commands pre-sorted for consuption by a render pass.
    /// </summary>
    public RenderCommandCollection MeshRenderCommands { get; } = new();

    private readonly Dictionary<string, XRTexture> _textures = [];
    private readonly Dictionary<string, XRFrameBuffer> _frameBuffers = [];

    public XRRenderPipeline()
    {
        Commands = new(this);
    }

    public T? GetTexture<T>(string name) where T : XRTexture
    {
        T? texture = null;
        if (_textures.TryGetValue(name, out XRTexture? value))
            texture = value as T;
        if (texture is null)
            Debug.Out($"Render pipeline texture {name} of type {typeof(T).GetFriendlyName()} was not found.");
        return texture;
    }

    public bool TryGetTexture(string name, out XRTexture? texture)
        => _textures.TryGetValue(name, out texture);

    public void SetTexture(XRTexture texture)
    {
        string? name = texture.Name ?? throw new ArgumentNullException(nameof(texture.Name), "Texture name must be set before adding to the pipeline.");
        if (!_textures.TryAdd(name, texture))
            _textures[name] = texture;
    }

    public T? GetFBO<T>(string name) where T : XRFrameBuffer
    {
        T? fbo = null;
        if (_frameBuffers.TryGetValue(name, out XRFrameBuffer? value))
            fbo = value as T;
        if (fbo is null)
            Debug.Out($"Render pipeline FBO {name} of type {typeof(T).GetFriendlyName()} was not found.");
        return fbo;
    }

    public bool TryGetFBO(string name, out XRFrameBuffer? fbo)
        => _frameBuffers.TryGetValue(name, out fbo);

    public void SetFBO(XRFrameBuffer fbo)
    {
        string? name = fbo.Name ?? throw new ArgumentNullException(nameof(fbo.Name), "Framebuffer name must be set before adding to the pipeline.");
        if (!_frameBuffers.TryAdd(name, fbo))
            _frameBuffers[name] = fbo;
    }

    private bool _isInitialized = false;

    public void GenerateCommandChain()
    {
        DestroyCache();
        _frameBuffers.Clear();
        _textures.Clear();
        Commands = GenerateCommandChainInternal();
        _isInitialized = true;
    }

    public class RenderingStatus
    {
        /// <summary>
        /// The viewport being rendered to.
        /// May be null if rendering directly to a framebuffer.
        /// </summary>
        public XRViewport? Viewport { get; private set; }
        /// <summary>
        /// The scene being rendered.
        /// </summary>
        public VisualScene? Scene { get; private set; }
        /// <summary>
        /// The camera this render pipeline is rendering the scene through.
        /// </summary>
        public XRCamera? Camera { get; private set; }
        /// <summary>
        /// The output FBO target for the render pass.
        /// May be null if rendering to the screen.
        /// </summary>
        public XRFrameBuffer? OutputFBO { get; private set; }
        /// <summary>
        /// If this pipeline is rendering a shadow pass.
        /// Shadow passes do not need to execute all rendering commands.
        /// </summary>
        public bool ShadowPass { get; private set; }

        public void Set(XRViewport? viewport, VisualScene? scene, XRCamera? camera, XRFrameBuffer? target, bool shadowPass)
        {
            Viewport = viewport;
            Scene = scene;
            Camera = camera;
            OutputFBO = target;
            ShadowPass = shadowPass;
        }

        public void Clear()
        {
            Viewport = null;
            Scene = null;
            Camera = null;
            OutputFBO = null;
            ShadowPass = false;
        }
    }

    public RenderingStatus RenderStatus { get; } = new();

    public ViewportRenderCommandContainer Commands { get; private set; }

    /// <summary>
    /// Renders the scene to the viewport or framebuffer.
    /// </summary>
    /// <param name="visualScene"></param>
    /// <param name="camera"></param>
    /// <param name="viewport"></param>
    /// <param name="targetFBO"></param>
    /// <param name="shadowPass"></param>
    public void Render(VisualScene visualScene, XRCamera camera, XRViewport? viewport, XRFrameBuffer? targetFBO, bool shadowPass)
    {
        if (RenderPipeline is not null)
        {
            Debug.Out("Render pipeline is already rendering. Cannot render again until the current render is complete.");
            return;
        }

        RenderPipeline = this;
        RenderStatus.Set(viewport, visualScene, camera, targetFBO, shadowPass);

        //_timeQuery.BeginQuery(EQueryTarget.TimeElapsed);
        using (PushRenderingCamera(camera))
        {
            try
            {
                Commands.Execute();
            }
            catch (Exception e)
            {
                Debug.Out(EOutputVerbosity.Verbose, true, true, true, true, 0, 10, e.Message);
            }
        }
        //_renderFPS = 1.0f / (_timeQuery.EndAndGetQueryInt() * 1e-9f);
        //Engine.PrintLine(_renderMS.ToString() + " ms");

        RenderStatus.Clear();
        RenderPipeline = null;
    }

    protected abstract ViewportRenderCommandContainer GenerateCommandChainInternal();

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
        XRTexture2D brdf = XRTexture2D.CreateFrameBufferTexture(width, height, EPixelInternalFormat.Rg16f, EPixelFormat.Rg, EPixelType.HalfFloat);
        brdf.Resizable = true;
        brdf.UWrap = ETexWrapMode.ClampToEdge;
        brdf.VWrap = ETexWrapMode.ClampToEdge;
        brdf.MinFilter = ETexMinFilter.Linear;
        brdf.MagFilter = ETexMagFilter.Linear;
        brdf.SamplerName = "BRDF";
        XRTexture2D[] texRefs = [brdf];

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
        fbo.SetRenderTargets((brdf, EFrameBufferAttachment.ColorAttachment0, 0, -1));

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

    public void DestroyCache()
    {
        foreach (var fbo in _frameBuffers)
            fbo.Value.Destroy();
        _frameBuffers.Clear();

        foreach (var tex in _textures)
            tex.Value.Destroy();
        _textures.Clear();
    }

    //TODO: actually resize fbos and textures instead of recreating them
    public void ViewportResized(int width, int height)
    {
        DestroyCache();
    }
    public void InternalResolutionResized(int internalWidth, int internalHeight)
    {
        DestroyCache();
    }
}