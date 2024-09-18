using static XREngine.Rendering.OpenGL.OpenGLRenderer;

namespace XREngine.Rendering.OpenGL
{
    internal class GLTextureView(OpenGLRenderer renderer, XRTextureViewBase data) : GLObject<XRTextureViewBase>(renderer, data)
    {
        public override GLObjectType Type => GLObjectType.Texture;
    }
}