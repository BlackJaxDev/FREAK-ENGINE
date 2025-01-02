namespace XREngine.Modeling
{
    public partial class MeshGenerator
    {
        private readonly struct Edge(int vertex1, int vertex2) : IEquatable<Edge>
        {
            public readonly int vertex1 = Math.Min(vertex1, vertex2);
            public readonly int vertex2 = Math.Max(vertex1, vertex2);

            public bool Equals(Edge other)
                => vertex1 == other.vertex1 && vertex2 == other.vertex2;

            public override bool Equals(object? obj)
                => obj is Edge edge && Equals(edge);

            public override int GetHashCode()
                => HashCode.Combine(vertex1, vertex2);
        }
    }
}
