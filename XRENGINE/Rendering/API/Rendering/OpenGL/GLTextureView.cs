using static XREngine.Rendering.OpenGL.OpenGLRenderer;

namespace XREngine.Rendering.OpenGL
{
    internal class GLTextureView(OpenGLRenderer renderer, XRTextureCubeView data) : GLObject<XRTextureViewBase>(renderer, data)
    {
        public override GLObjectType Type => GLObjectType.Texture;
    }
}