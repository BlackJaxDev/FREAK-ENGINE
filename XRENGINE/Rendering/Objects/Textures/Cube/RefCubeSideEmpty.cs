using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class RefCubeSideEmpty(uint width, uint height, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type) : RefCubeSide(width, height)
    {
        public EPixelFormat PixelFormat { get; private set; } = format;
        public EPixelType PixelType { get; private set; } = type;
        public EPixelInternalFormat InternalFormat { get; private set; } = internalFormat;

        public override RenderCubeSide AsRenderSide(int mipIndex)
            => new(Width, Height, InternalFormat, PixelFormat, PixelType);
    }
}
