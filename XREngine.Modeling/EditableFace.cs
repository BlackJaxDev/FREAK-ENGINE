namespace XREngine.Modeling
{
    /// <summary>
    /// CCW winding order triangle.
    /// Each edge has a reference to its opposite edge, forming a half-edge data structure.
    /// </summary>
    /// <param name="edge1"></param>
    /// <param name="edge2"></param>
    /// <param name="edge3"></param>
    public class EditableFace(EditableEdge edge1, EditableEdge edge2, EditableEdge edge3) : IEquatable<EditableFace>
    {
        public EditableEdge Edge1 { get; } = edge1;
        public EditableEdge Edge2 { get; } = edge2;
        public EditableEdge Edge3 { get; } = edge3;
        public bool Equals(EditableFace? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Edge1.Equals(other.Edge1) && Edge2.Equals(other.Edge2) && Edge3.Equals(other.Edge3);
        }
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj is EditableFace face && Equals(face);
        }
        public override int GetHashCode()
            => HashCode.Combine(Edge1, Edge2, Edge3);
    }
}
