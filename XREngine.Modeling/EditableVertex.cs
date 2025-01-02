namespace XREngine.Modeling
{
    public class EditableVertex(float x, float y, float z) : IEquatable<EditableVertex>
    {
        public float X { get; } = x;
        public float Y { get; } = y;
        public float Z { get; } = z;

        public bool Equals(EditableVertex? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj is EditableVertex vertex && Equals(vertex);
        }
        public override int GetHashCode()
            => HashCode.Combine(X, Y, Z);
    }
}
