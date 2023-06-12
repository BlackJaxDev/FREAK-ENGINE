using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Geometry
{
    public struct Triangle
    {
        public Vec3 A;
        public Vec3 B;
        public Vec3 C;

        public Triangle(Vec3 a, Vec3 b, Vec3 c)
        {
            A = a;
            B = b;
            C = c;
        }

        public Plane GetPlane()
            => new(A, B, C);

        public Vec3 GetNormal()
            => Vec3.Normalize(Vec3.Cross(B - A, C - A));

        public void Flip()
            => (C, A) = (A, C);
    }
}