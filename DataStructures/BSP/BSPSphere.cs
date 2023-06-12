namespace XREngine.Data.BSP
{
    public class BSPSphere : BSPShape
    {
        private readonly float _radius;
        private readonly int _segments;

        public BSPSphere(float radius, int segments)
        {
            _radius = radius;
            _segments = segments;
            GenerateMeshData();
        }

        protected override void GenerateVertices()
        {
            //... (Generate vertices for a sphere)
        }

        protected override void GenerateIndices()
        {
            //... (Generate indices for a sphere)
        }

        protected override void GenerateNormals()
        {
            //... (Calculate normals for each vertex)
        }
    }
}