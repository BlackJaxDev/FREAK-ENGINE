using XREngine.Data.Geometry;

namespace XREngine.Data.BSP
{
    public static class BSPBoolean
    {
        public static List<Triangle> Union(BSPNode a, BSPNode b)
        {
            a = a.Clone();
            b = b.Clone();

            a.ClipTo(b);
            b.ClipTo(a);
            b.Invert();
            b.ClipTo(a);
            b.Invert();

            a.GetAllTriangles(b.Triangles);
            return a.Triangles;
        }

        public static List<Triangle> Intersect(BSPNode a, BSPNode b)
        {
            a = a.Clone();
            b = b.Clone();

            a.Invert();
            a.ClipTo(b);
            b.ClipTo(a);
            a.Invert();

            a.GetAllTriangles(b.Triangles);
            return a.Triangles;
        }

        public static List<Triangle> Subtract(BSPNode a, BSPNode b)
        {
            a = a.Clone();
            b = b.Clone();

            a.ClipTo(b);
            b.Invert();
            b.ClipTo(a);
            b.Invert();

            a.GetAllTriangles(b.Triangles);
            return a.Triangles;
        }

        public static List<Triangle> XOR(BSPNode a, BSPNode b)
            => Subtract(new(Union(a, b)), new(Intersect(a, b)));
    }
}