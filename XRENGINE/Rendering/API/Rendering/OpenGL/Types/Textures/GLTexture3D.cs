using XREngine.Data.Rendering;

namespace XREngine.Rendering.OpenGL
{
    public class GLTexture3D(OpenGLRenderer renderer, XRTexture3D data) : GLTexture<XRTexture3D>(renderer, data)
    {
        public override ETextureTarget TextureTarget { get; } = ETextureTarget.Texture3D;

        public override void PushData()
        {

        }

        public override string ResolveSamplerName(int textureIndex, string? samplerNameOverride)
        {
            return samplerNameOverride ?? $"uTexture3D{textureIndex}";
        }
    }
}