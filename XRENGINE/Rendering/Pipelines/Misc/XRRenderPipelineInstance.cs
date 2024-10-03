using XREngine.Data.Core;
using XREngine.Rendering.Commands;
using XREngine.Scene;
using static XREngine.Engine.Rendering.State;

namespace XREngine.Rendering;

/// <summary>
/// This class is the base class for all render pipelines.
/// A render pipeline is responsible for all rendering operations to render a scene to a viewport.
/// </summary>
public sealed partial class XRRenderPipelineInstance : XRBase
{
    //TODO: stereoscopic rendering output

    /// <summary>
    /// This collection contains mesh rendering commands pre-sorted for consuption by a render pass.
    /// </summary>
    public RenderCommandCollection MeshRenderCommands { get; } = new();

    private readonly Dictionary<string, XRTexture> _textures = [];
    private readonly Dictionary<string, XRFrameBuffer> _frameBuffers = [];

    private RenderPipeline? _pipeline;
    public RenderPipeline? Pipeline
    {
        get => _pipeline;
        set => SetField(ref _pipeline, value);
    }

    protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
    {
        base.OnPropertyChanged(propName, prev, field);
        switch (propName)
        {
            case nameof(Pipeline):
                if (Pipeline is not null)
                {
                    MeshRenderCommands.SetRenderPasses(Pipeline.PassIndicesAndSorters);
                    InvalidMaterial = Pipeline.InvalidMaterial;
                }
                else
                    InvalidMaterial = null;
                DestroyCache();
                break;
        }
    }

    public RenderingStatus RenderStatus { get; } = new();
    public XRMaterial? InvalidMaterial { get; set; }

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
        if (Pipeline is null)
        {
            Debug.LogWarning("No render pipeline is set.");
            return;
        }

        if (CurrentPipeline is not null)
        {
            Debug.LogWarning("Render pipeline is already rendering. Cannot render again until the current render is complete.");
            return;
        }

        CurrentPipeline = this;
        RenderStatus.Set(viewport, visualScene, camera, targetFBO, shadowPass);

        //_timeQuery.BeginQuery(EQueryTarget.TimeElapsed);
        //using (PushRenderingCamera(camera))
        //{
            //try
            //{
                Pipeline.CommandChain.Execute();
            //}
            //catch (Exception e)
            //{
            //    Debug.Out(EOutputVerbosity.Verbose, true, true, true, true, 3, 10, e.Message);
            //}
        //}
        //_renderFPS = 1.0f / (_timeQuery.EndAndGetQueryInt() * 1e-9f);
        //Engine.PrintLine(_renderMS.ToString() + " ms");

        RenderStatus.Clear();
        CurrentPipeline = null;
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
        //DestroyCache();
    }
    public void InternalResolutionResized(int internalWidth, int internalHeight)
    {
        //DestroyCache();
    }

    public T? GetTexture<T>(string name) where T : XRTexture
    {
        T? texture = null;
        if (_textures.TryGetValue(name, out XRTexture? value))
            texture = value as T;
        //if (texture is null)
        //    Debug.LogWarning($"Render pipeline texture {name} of type {typeof(T).GetFriendlyName()} was not found.");
        return texture;
    }

    public bool TryGetTexture(string name, out XRTexture? texture)
    {
        bool found = _textures.TryGetValue(name, out texture);
        //if (!found || texture is null)
        //    Debug.Out($"Render pipeline texture {name} was not found.");
        return found;
    }

    public void SetTexture(XRTexture texture)
    {
        string? name = texture.Name;
        if (name is null)
        {
            Debug.LogWarning("Texture name must be set before adding to the pipeline.");
            return;
        }
        if (!_textures.TryAdd(name, texture))
        {
            Debug.Out($"Render pipeline texture {name} already exists. Overwriting.");
            _textures[name]?.Destroy();
            _textures[name] = texture;
        }
    }

    public T? GetFBO<T>(string name) where T : XRFrameBuffer
    {
        T? fbo = null;
        if (_frameBuffers.TryGetValue(name, out XRFrameBuffer? value))
            fbo = value as T;
        //if (fbo is null)
        //    Debug.LogWarning($"Render pipeline FBO {name} of type {typeof(T).GetFriendlyName()} was not found.");
        return fbo;
    }

    public bool TryGetFBO(string name, out XRFrameBuffer? fbo)
    {
        bool found = _frameBuffers.TryGetValue(name, out fbo);
        //if (!found || fbo is null)
        //    Debug.Out($"Render pipeline FBO {name} was not found.");
        return found;
    }

    public void SetFBO(XRFrameBuffer fbo)
    {
        string? name = fbo.Name;
        if (name is null)
        {
            Debug.LogWarning("FBO name must be set before adding to the pipeline.");
            return;
        }
        if (!_frameBuffers.TryAdd(name, fbo))
        {
            Debug.Out($"Render pipeline FBO {name} already exists. Overwriting.");
            _frameBuffers[name]?.Destroy();
            _frameBuffers[name] = fbo;
        }
    }

    internal T GetFBO<T>(object userInterfaceFBOName)
    {
        throw new NotImplementedException();
    }
}