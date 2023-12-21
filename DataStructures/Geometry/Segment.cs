using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Geometry
{
    public struct Segment
    {
        private Vec3 start;
        private Vec3 end;

        public Segment(Vec3 start, Vec3 end)
        {
            Start = start;
            End = end;
        }

        public Vec3 Start
        {
            get => start;
            set => start = value;
        }

        public Vec3 End
        {
            get => end;
            set => end = value;
        }
    }
}