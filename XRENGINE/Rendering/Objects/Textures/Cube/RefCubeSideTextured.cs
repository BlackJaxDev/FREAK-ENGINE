using ImageMagick;

namespace XREngine.Rendering
{
    public class RefCubeSideTextured : RefCubeSide
    {
        public MagickImage Map { get; private set; }

        public RefCubeSideTextured(MagickImage map)
            => Map = map;

        public RefCubeSideTextured(uint width, uint height)
            => Map = new MagickImage(MagickColors.Transparent, width, height);

        public override RenderCubeSide AsRenderSide(int mipIndex)
            => new(Map);

        public static implicit operator RefCubeSideTextured(MagickImage map)
            => new(map);
    }
}
