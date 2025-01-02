namespace XREngine.Modeling
{
    public class EditableEdge(EditableVertex vertex1, EditableVertex vertex2) : IEquatable<EditableEdge>
    {
        public EditableVertex Vertex1 { get; } = vertex1;
        public EditableVertex Vertex2 { get; } = vertex2;
        public EditableEdge? Opposite { get; set; }
        /// <summary>
        /// The next edge in the CCW winding order.
        /// </summary>
        public EditableEdge? Next { get; set; }
        /// <summary>
        /// The previous edge in the CCW winding order.
        /// </summary>
        public EditableEdge? Previous { get; set; }

        public bool Equals(EditableEdge? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return (Vertex1.Equals(other.Vertex1) && Vertex2.Equals(other.Vertex2)) ||
                   (Vertex1.Equals(other.Vertex2) && Vertex2.Equals(other.Vertex1));
        }
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj is EditableEdge edge && Equals(edge);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return (Vertex1.GetHashCode() * 397) ^ Vertex2.GetHashCode();
            }
        }
    }
}
