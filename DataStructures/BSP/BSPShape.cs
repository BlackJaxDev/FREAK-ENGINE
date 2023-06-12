using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.BSP
{
    public abstract class BSPShape
    {
        public List<Vec3> Vertices { get; protected set; }
        public List<int> Indices { get; protected set; }
        public List<Vec3> Normals { get; protected set; }

        protected BSPShape()
        {
            Vertices = new List<Vec3>();
            Indices = new List<int>();
            Normals = new List<Vec3>();
        }

        public void GenerateMeshData()
        {
            Vertices.Clear();
            Indices.Clear();
            Normals.Clear();

            GenerateVertices();
            GenerateIndices();
            GenerateNormals();
        }

        protected abstract void GenerateVertices();
        protected abstract void GenerateIndices();
        protected abstract void GenerateNormals();

        public void Draw()
        {
            //GL.Begin(PrimitiveType.Triangles);
            //for (int i = 0; i < Indices.Count; i++)
            //{
            //    GL.Normal3(Normals[Indices[i]]);
            //    GL.Vertex3(Vertices[Indices[i]]);
            //}
            //GL.End();
        }
    }
}