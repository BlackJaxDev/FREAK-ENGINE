using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRTextureBufferView(
        XRTextureBuffer viewedTexture,
        int minLevel,
        int numLevels,
        int minLayer,
        int numLayers,
        EPixelInternalFormat internalFormat) : XRTextureView<XRTextureBuffer>(viewedTexture, minLevel, numLevels, minLayer, numLayers, internalFormat)
    {
        public override uint MaxDimension { get; } = 0;
    }
}
