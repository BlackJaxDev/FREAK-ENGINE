using System.Numerics;
using XREngine.Data.Geometry;

namespace XREngine.Data.BSP
{
    public static class BSPShapeExtensions
    {
        public static BSPNode ToBSPNode(this BSPShape shape)
        {
            List<Triangle> triangles = [];
            for (int i = 0; i < shape.Indices.Count; i += 3)
            {
                Vector3 a = shape.Vertices[shape.Indices[i]];
                Vector3 b = shape.Vertices[shape.Indices[i + 1]];
                Vector3 c = shape.Vertices[shape.Indices[i + 2]];
                triangles.Add(new Triangle(a, b, c));
            }

            BSPNode node = new();
            node.Build(triangles);
            return node;
        }

        public static void FromBSPNode(this BSPShape shape, BSPNode node)
        {
            List<Triangle> triangles = [];
            node.GetAllTriangles(triangles);

            shape.Vertices.Clear();
            shape.Indices.Clear();
            shape.Normals.Clear();

            foreach (Triangle triangle in triangles)
            {
                int aIndex = shape.Vertices.Count;
                int bIndex = shape.Vertices.Count + 1;
                int cIndex = shape.Vertices.Count + 2;

                shape.Vertices.Add(triangle.A);
                shape.Vertices.Add(triangle.B);
                shape.Vertices.Add(triangle.C);

                shape.Indices.Add(aIndex);
                shape.Indices.Add(bIndex);
                shape.Indices.Add(cIndex);

                Vector3 normal = triangle.GetNormal();
                shape.Normals.Add(normal);
                shape.Normals.Add(normal);
                shape.Normals.Add(normal);
            }
        }
    }
}