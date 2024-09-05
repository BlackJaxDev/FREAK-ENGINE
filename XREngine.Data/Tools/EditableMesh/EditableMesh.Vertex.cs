namespace XREngine.Data.Tools
{
    //public partial class EditableMesh
    //{
    //    public class EditableVertex(Vertex vertex)
    //    {
    //        private Vertex _vertex = vertex;
    //        private List<EditableHalfEdge> _outgoingEdges = [];
    //        private Matrix4x4 _quadric = new();
    //        private Vector3? _modifiedPosition;

    //        public Vector3 Position
    //        {
    //            get => _modifiedPosition ?? Vertex.Position;
    //            set => _modifiedPosition = value;
    //        }

    //        public void ApplyPosition()
    //        {
    //            if (_modifiedPosition.HasValue)
    //                Vertex.Position = _modifiedPosition.Value;
    //        }

    //        public Vertex Vertex { get => _vertex; set => _vertex = value; }
    //        public List<EditableHalfEdge> OutgoingEdges { get => _outgoingEdges; set => _outgoingEdges = value; }
    //        public Matrix4x4 Quadric { get => _quadric; set => _quadric = value; }

    //        public void ComputeQuadric(List<EditableTriangle> triangles)
    //        {
    //            Quadric = new Matrix4x4();
    //            foreach (EditableTriangle triangle in triangles)
    //                if (triangle.Contains(this))
    //                    Quadric += triangle.GetQuadricMatrix();
    //        }

    //        public static implicit operator EditableVertex(Vertex vertex)
    //            => new(vertex);
    //    }
    //}
}
