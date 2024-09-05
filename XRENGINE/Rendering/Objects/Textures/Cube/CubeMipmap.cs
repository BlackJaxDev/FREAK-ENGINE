using Extensions;
using ImageMagick;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials.Textures;

namespace XREngine.Rendering
{
    public class CubeMipmap
    {
        public bool IsCrossMap => Sides.Length == 1;

        public RefCubeSide[] Sides { get; private set; }
        
        public CubeMipmap(MagickImage crossMap)
            => Sides = [new RefCubeSideTextured(crossMap)];

        public CubeMipmap(
            RefCubeSide posX, RefCubeSide negX,
            RefCubeSide posY, RefCubeSide negY,
            RefCubeSide posZ, RefCubeSide negZ)
            => Sides = [posX, negX, posY, negY, posZ, negZ];

        public CubeMipmap(int width, int height, EPixelInternalFormat internalFormat, EPixelFormat format, EPixelType type)
        {
            Sides = new RefCubeSide[6];
            Sides.Fill(i => new RefCubeSideEmpty(width, height, internalFormat, format, type));
        }

        public CubeMipmap(int width, int height)
        {
            Sides = new RefCubeSide[6];
            Sides.Fill(i => new RefCubeSideTextured(width, height));
        }

        public void FillRenderMap(TextureCubeMipmap mip, int mipIndex)
        {
            if (mip is null)
                return;
            if (IsCrossMap)
            {
                RefCubeSideTextured crossTex = ((RefCubeSideTextured)Sides[0]);
                var tex = crossTex.Map;
                //Task.Run(() => crossTex.Map.GetInstance()).ContinueWith(x =>
                {
                    MagickImage[]? maps = [tex];//x.Result.Bitmaps;
                    if (maps is null)
                        return;
                    
                    MagickImage bmp;
                    //If the mip index is greater than the amount of mipmaps, resize the last mip to the desired size.
                    if (mipIndex >= maps.Length)
                    {
                        bmp = maps[^1];
                        int w = bmp.Width;
                        int h = bmp.Height;
                        for (int i = maps.Length; i <= mipIndex && w >= 1 && h >= 1; ++i)
                        {
                            w /= 2;
                            h /= 2;
                        }
                        bmp.InterpolativeResize(w, h, PixelInterpolateMethod.Bilinear); //TODO: use NearestNeighbor instead?
                    }
                    else
                        bmp = maps[mipIndex];

                    if (!mip.SetCrossCubeMap(bmp))
                        Debug.Out("Cubemap cross dimensions are invalid; width/height be a 4:3 or 3:4 ratio.");
                }//);
            }
            else
            {
                mip.SetSides(
                    Sides[0].AsRenderSide(mipIndex),
                    Sides[1].AsRenderSide(mipIndex),
                    Sides[2].AsRenderSide(mipIndex),
                    Sides[3].AsRenderSide(mipIndex),
                    Sides[4].AsRenderSide(mipIndex),
                    Sides[5].AsRenderSide(mipIndex));
            }
        }
        public TextureCubeMipmap AsRenderMipmap(int mipIndex)
        {
            TextureCubeMipmap mip = new();
            FillRenderMap(mip, mipIndex);
            return mip;
        }
    }
}
