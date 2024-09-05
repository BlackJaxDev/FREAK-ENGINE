namespace XREngine.Modeling
{
    public partial class MeshGenerator
    {
        private readonly struct Edge : IEquatable<Edge>
        {
            public readonly int vertex1;
            public readonly int vertex2;

            public Edge(int vertex1, int vertex2)
            {
                this.vertex1 = Math.Min(vertex1, vertex2);
                this.vertex2 = Math.Max(vertex1, vertex2);
            }

            public bool Equals(Edge other)
                => vertex1 == other.vertex1 && vertex2 == other.vertex2;

            public override bool Equals(object? obj)
                => obj is Edge edge && Equals(edge);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + vertex1.GetHashCode();
                    hash = hash * 31 + vertex2.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
