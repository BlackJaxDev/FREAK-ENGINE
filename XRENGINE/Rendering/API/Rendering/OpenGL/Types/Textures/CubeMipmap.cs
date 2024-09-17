using Extensions;
using ImageMagick;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.Models.Materials.Textures
{
    //TODO: replace bitmap class
#pragma warning disable CA1416 // Validate platform compatibility
    public class CubeMipmap : IDisposable
    {
        public bool IsCrossMap => Sides.Length == 1;
        public CubeSide[] Sides { get; private set; } = new CubeSide[6];

        public CubeMipmap() { }
        public CubeMipmap(MagickImage cubeCrossBmp, EPixelInternalFormat internalFormat, bool isFillerBitmap = false)
        {
            if (isFillerBitmap)
                SetSides(cubeCrossBmp, internalFormat);
            else if (!SetCrossCubeMap(cubeCrossBmp, internalFormat))
                throw new InvalidOperationException("Cubemap cross dimensions are invalid; width/height be a 4:3 or 3:4 ratio.");
        }

        public CubeMipmap(
            CubeSide posX, CubeSide negX,
            CubeSide posY, CubeSide negY,
            CubeSide posZ, CubeSide negZ)
            => Sides = [posX, negX, posY, negY, posZ, negZ];
        
        public CubeMipmap(uint dim, EPixelInternalFormat internalFormat, MagickColor? color = null)
            => SetSides(dim, internalFormat, color);
        public CubeMipmap(uint dim, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type)
            => Sides.Fill(i => new CubeSideEmpty(dim, dim, internalFormat, format, type));

        public bool SetCrossCubeMap(MagickImage cubeCrossBmp, EPixelInternalFormat internalFormat)
        {
            uint w = cubeCrossBmp.Width;
            uint h = cubeCrossBmp.Height;
            MagickGeometry[] crops;

            if (w % 4 == 0 && 
                w / 4 * 3 == h)
            {
                //Cross is on its side.
                //     __
                //  __|__|__ __        +Y
                // |__|__|__|__|   -X, -Z, +X, +Z
                //    |__|             -Y

                uint dim = w / 4;
                crops =
                [
                    new((int)dim * 2, (int)dim, dim, dim), //+X
                    new(0, (int)dim, dim, dim), //-X
                    new((int)dim, 0, dim, dim), //+Y
                    new((int)dim, (int)dim * 2, dim, dim), //-Y
                    new((int)dim * 3, (int)dim, dim, dim), //+Z
                    new((int)dim, (int)dim, dim, dim), //-Z
                ];
            }
            else if (
                h % 4 == 0 &&
                h / 4 * 3 == w)
            {
                //Cross is standing up.
                //     __
                //  __|__|__        +Y
                // |__|__|__|   -X, -Z, +X
                //    |__|          -Y
                //    |__|          +Z

                uint dim = h / 4;
                crops =
                [
                    new((int)dim * 2, (int)dim, dim, dim), //+X
                    new(0, (int)dim, dim, dim), //-X
                    new((int)dim, 0, dim, dim), //+Y
                    new((int)dim, (int)dim * 2, dim, dim), //-Y
                    new((int)dim, (int)dim * 3, dim, dim), //+Z
                    new((int)dim, (int)dim, dim, dim), //-Z
                ];
            }
            else
                return false;

            Sides = crops.Select(x =>
            {
                var clone = cubeCrossBmp.Clone();
                clone.Crop(x);
                return new CubeSideTextured((MagickImage)clone, internalFormat);
            }).ToArray();

            return true;
        }

        public void SetSides(
            CubeSide posX, CubeSide negX,
            CubeSide posY, CubeSide negY,
            CubeSide posZ, CubeSide negZ)
            => Sides = [posX, negX, posY, negY, posZ, negZ];

        public void SetSides(MagickImage bmp, EPixelInternalFormat internalFormat)
        {
            for (int i = 0; i < 6; ++i)
                Sides[i] = new CubeSideTextured(bmp, internalFormat);
        }
        
        public void SetSides(uint dim, EPixelInternalFormat internalFormat, MagickColor? color = null)
            => Sides.Fill(i => new CubeSideTextured(new MagickImage(color ??= new MagickColor(0, 0, 0, 0), dim, dim), internalFormat));

        public void SetSides(uint dim, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type)
            => Sides.Fill(i => new CubeSideEmpty(dim, dim, internalFormat, format, type));

        public void Dispose()
        {
            //foreach (CubeSide b in Sides)
            //    b.Dispose();
            GC.SuppressFinalize(this);
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}
