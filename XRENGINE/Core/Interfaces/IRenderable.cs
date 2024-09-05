using XREngine.Rendering.Info;

namespace XREngine
{
    /// <summary>
    /// Defines an object with any number of cullable sub-objects that can be rendered.
    /// </summary>
    public interface IRenderable
    {
        RenderInfo[] RenderedObjects { get; }
    }
}
