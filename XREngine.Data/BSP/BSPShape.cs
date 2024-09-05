using System.Numerics;

namespace XREngine.Data.BSP
{
    public abstract class BSPShape
    {
        public List<Vector3> Vertices { get; protected set; }
        public List<int> Indices { get; protected set; }
        public List<Vector3> Normals { get; protected set; }

        protected BSPShape()
        {
            Vertices = [];
            Indices = [];
            Normals = [];
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