using System.Numerics;

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
            Vertices.AddRange(
            [
                new Vector3(-halfSize, -halfSize, -halfSize),
                new Vector3(halfSize, -halfSize, -halfSize),
                new Vector3(halfSize, halfSize, -halfSize),
                new Vector3(-halfSize, halfSize, -halfSize),
                new Vector3(-halfSize, -halfSize, halfSize),
                new Vector3(halfSize, -halfSize, halfSize),
                new Vector3(halfSize, halfSize, halfSize),
                new Vector3(-halfSize, halfSize, halfSize),
            ]);
        }

        protected override void GenerateIndices()
        {
            Indices.AddRange(
            [
                0, 1, 2, 2, 3, 0, 1, 5, 6, 6, 2, 1,
                7, 6, 5, 5, 4, 7, 0, 3, 7, 7, 4, 0,
                0, 1, 5, 5, 4, 0, 3, 2, 6, 6, 7, 3
            ]);
        }

        protected override void GenerateNormals()
        {
            Normals.AddRange(
            [
                new Vector3(0, 0, -1),
                new Vector3(0, 0, 1),
                new Vector3(0, -1, 0),
                new Vector3(0, 1, 0),
                new Vector3(-1, 0, 0),
                new Vector3(1, 0, 0)
            ]);
        }
    }
}