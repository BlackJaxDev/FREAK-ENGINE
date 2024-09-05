using Extensions;
using ImageMagick;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.Models.Materials.Textures
{
    //TODO: replace bitmap class
#pragma warning disable CA1416 // Validate platform compatibility
    public class TextureCubeMipmap : IDisposable
    {
        public RenderCubeSide[] Sides { get; private set; } = new RenderCubeSide[6];

        public TextureCubeMipmap() { }
        public TextureCubeMipmap(MagickImage cubeCrossBmp, bool isFillerBitmap = false)
        {
            if (isFillerBitmap)
                SetSides(cubeCrossBmp);
            else if (!SetCrossCubeMap(cubeCrossBmp))
                throw new InvalidOperationException("Cubemap cross dimensions are invalid; width/height be a 4:3 or 3:4 ratio.");
        }

        public TextureCubeMipmap(
            RenderCubeSide posX, RenderCubeSide negX,
            RenderCubeSide posY, RenderCubeSide negY,
            RenderCubeSide posZ, RenderCubeSide negZ)
            => Sides = [posX, negX, posY, negY, posZ, negZ];
        
        public TextureCubeMipmap(int dim, MagickColor? color = null)
            => SetSides(dim, color);
        public TextureCubeMipmap(int dim, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type)
            => Sides.Fill(i => new RenderCubeSide(dim, dim, internalFormat, format, type));

        public bool SetCrossCubeMap(MagickImage cubeCrossBmp)
        {
            int w = cubeCrossBmp.Width;
            int h = cubeCrossBmp.Height;
            MagickGeometry[] crops;

            if (w % 4 == 0 && 
                w / 4 * 3 == h)
            {
                //Cross is on its side.
                //     __
                //  __|__|__ __        +Y
                // |__|__|__|__|   -X, -Z, +X, +Z
                //    |__|             -Y

                int dim = w / 4;
                crops =
                [
                    new(dim * 2, dim, dim, dim), //+X
                    new(0, dim, dim, dim), //-X
                    new(dim, 0, dim, dim), //+Y
                    new(dim, dim * 2, dim, dim), //-Y
                    new(dim * 3, dim, dim, dim), //+Z
                    new(dim, dim, dim, dim), //-Z
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

                int dim = h / 4;
                crops =
                [
                    new(dim * 2, dim, dim, dim), //+X
                    new(0, dim, dim, dim), //-X
                    new(dim, 0, dim, dim), //+Y
                    new(dim, dim * 2, dim, dim), //-Y
                    new(dim, dim * 3, dim, dim), //+Z
                    new(dim, dim, dim, dim), //-Z
                ];
            }
            else
                return false;

            Sides = crops.Select(x =>
            {
                var clone = cubeCrossBmp.Clone();
                clone.Crop(x);
                return new RenderCubeSide((MagickImage)clone);
            }).ToArray();

            return true;
        }

        public void SetSides(
            RenderCubeSide posX, RenderCubeSide negX,
            RenderCubeSide posY, RenderCubeSide negY,
            RenderCubeSide posZ, RenderCubeSide negZ)
            => Sides = [posX, negX, posY, negY, posZ, negZ];

        public void SetSides(MagickImage bmp)
        {
            //TODO: Determine if clones of the bitmap are necessary or not
            Sides.Fill(bmp);
            //Sides.FillWith(i => cubeCrossBmp.Clone());
        }

        public void SetSides(int dim, MagickColor? color = null)
            => Sides.Fill(i => new MagickImage(color ??= new MagickColor(0, 0, 0, 0), dim, dim));

        public void SetSides(int dim, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type)
            => Sides.Fill(i => new RenderCubeSide(dim, dim, internalFormat, format, type));

        public void Dispose()
        {
            foreach (RenderCubeSide b in Sides)
                b.Dispose();
            GC.SuppressFinalize(this);
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}
