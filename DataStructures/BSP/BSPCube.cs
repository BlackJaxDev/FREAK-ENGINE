using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.BSP
{
    public class BSPCube : BSPShape
    {
        private readonly float _size;

        public BSPCube(float size)
        {
            _size = size;
            GenerateMeshData();
        }

        protected override void GenerateVertices()
        {
            float halfSize = _size * 0.5f;
            Vertices.AddRange(new[]
            {
                new Vec3(-halfSize, -halfSize, -halfSize),
                new Vec3(halfSize, -halfSize, -halfSize),
                new Vec3(halfSize, halfSize, -halfSize),
                new Vec3(-halfSize, halfSize, -halfSize),
                new Vec3(-halfSize, -halfSize, halfSize),
                new Vec3(halfSize, -halfSize, halfSize),
                new Vec3(halfSize, halfSize, halfSize),
                new Vec3(-halfSize, halfSize, halfSize),
            });
        }

        protected override void GenerateIndices()
        {
            Indices.AddRange(new int[]
            {
                0, 1, 2, 2, 3, 0, 1, 5, 6, 6, 2, 1,
                7, 6, 5, 5, 4, 7, 0, 3, 7, 7, 4, 0,
                0, 1, 5, 5, 4, 0, 3, 2, 6, 6, 7, 3
            });
        }

        protected override void GenerateNormals()
        {
            //... (Calculate normals for each vertex)
        }
    }
}