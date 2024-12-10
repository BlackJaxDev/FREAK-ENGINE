using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class XRTexture3DView(
        XRTexture3D viewedTexture,
        uint minLevel,
        uint numLevels,
        uint minLayer,
        uint numLayers,
        EPixelInternalFormat internalFormat) : XRTextureView<XRTexture3D>(viewedTexture, minLevel, numLevels, minLayer, numLayers, internalFormat)
    {
        public override uint MaxDimension { get; } = 3u;
    }
}
