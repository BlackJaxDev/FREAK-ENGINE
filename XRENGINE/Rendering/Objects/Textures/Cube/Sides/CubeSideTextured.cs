using ImageMagick;
using XREngine.Data.Rendering;

namespace XREngine.Rendering
{
    public class CubeSideTextured : CubeSide
    {
        public MagickImage Map { get; private set; }

        public CubeSideTextured(MagickImage map, EPixelInternalFormat internalFormat) : base(map.Width, map.Height, internalFormat)
            => Map = map;

        public CubeSideTextured(uint width, uint height, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type) : base(width, height, internalFormat)
            => Map = XRTexture.NewImage(width, height, format, type);

        public static implicit operator CubeSideTextured(MagickImage map)
            => new(map, EPixelInternalFormat.Rgba);
    }
}
