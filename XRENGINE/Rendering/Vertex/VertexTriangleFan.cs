namespace XREngine.Data.Rendering
{
    public class VertexTriangleFan : VertexPolygon
    {
        public VertexTriangleFan(params Vertex[] vertices) : base(vertices) { }
        public VertexTriangleFan(IEnumerable<Vertex> vertices) : base(vertices) { }

        public override FaceType Type => FaceType.TriangleFan;

        public override VertexTriangle[] ToTriangles()
        {
            int triangleCount = _vertices.Count - 2;
            VertexTriangle[] list = new VertexTriangle[triangleCount];
            for (int i = 1; i < triangleCount; ++i)
                list[i] = new VertexTriangle(_vertices[0], _vertices[i], _vertices[i + 1]);
            return list;
        }
    }
}
