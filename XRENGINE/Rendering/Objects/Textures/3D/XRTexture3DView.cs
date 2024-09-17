using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRTexture3DView(
        XRTexture3D viewedTexture,
        int minLevel,
        int numLevels,
        int minLayer,
        int numLayers,
        EPixelInternalFormat internalFormat) : XRTextureView<XRTexture3D>(viewedTexture, minLevel, numLevels, minLayer, numLayers, internalFormat)
    {
        public override uint MaxDimension { get; } = 3u;
    }
}
