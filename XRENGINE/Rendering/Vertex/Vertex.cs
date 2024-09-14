using System.Numerics;
using XREngine.Scene.Transforms;

namespace XREngine.Data.Rendering
{
    public class Vertex : VertexData, IEquatable<Vertex>
    {
        public override FaceType Type => FaceType.Points;

        /// <summary>
        /// Contains weights for each bone that influences the position of this vertex.
        /// </summary>
        public Dictionary<TransformBase, float>? Weights { get; set; }
        /// <summary>
        /// Data this vertex can morph to, indexed by blendshape name.
        /// Data here is absolute, not deltas, for simplicity.
        /// </summary>
        public Dictionary<string, VertexData>? Blendshapes { get; set; }

        public Vertex() { }

        public Vertex(Dictionary<TransformBase, float>? weights)
            => Weights = weights;

        public Vertex(Vector3 position)
            => Position = position;

        public Vertex(Vector3 position, Vector4 color)
        {
            Position = position;
            ColorSets.Add(color);
        }

        public Vertex(Vector3 position, Dictionary<TransformBase, float>? weights)
            : this(position) => Weights = weights;

        public Vertex(Vector3 position, Dictionary<TransformBase, float>? weights, Vector3 normal)
            : this(position, weights) => Normal = normal;

        public Vertex(Vector3 position, Dictionary<TransformBase, float>? inf, Vector3 normal, Vector2 texCoord)
            : this(position, inf, normal) => TextureCoordinateSets.Add(texCoord);

        public Vertex(Vector3 position, Dictionary<TransformBase, float>? inf, Vector3 normal, Vector2 texCoord, Vector4 color)
            : this(position, inf, normal, texCoord) => ColorSets.Add(color);

        public Vertex(Vector3 position, Dictionary<TransformBase, float>? inf, Vector3 normal, Vector3 tangent, Vector2 texCoord, Vector4 color)
            : this(position, inf, normal, texCoord, color) => Tangent = tangent;

        public Vertex(Vector3 position, Dictionary<TransformBase, float>? inf, Vector2 texCoord)
            : this(position, inf) => TextureCoordinateSets.Add(texCoord);

        public Vertex(Vector3 position, Vector2 texCoord)
            : this(position) => TextureCoordinateSets.Add(texCoord);

        public Vertex(Vector3 position, Vector3 normal)
            : this(position, null, normal) { }

        public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord)
            : this(position, null, normal) => TextureCoordinateSets.Add(texCoord);

        public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord, Vector4 color)
            : this(position, null, normal, texCoord) => ColorSets.Add(color);

        public Vertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texCoord, Vector4 color)
            : this(position, null, normal, texCoord, color) => Tangent = tangent;

        public override bool Equals(object? obj) 
            => obj is Vertex vertex && Equals(vertex);

        public bool Equals(Vertex? other)
            => other is not null && other.GetHashCode() == GetHashCode();

        public static implicit operator Vertex(Vector3 pos) => new(pos);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Weights);
            hash.Add(Position);
            hash.Add(Normal);
            hash.Add(Tangent);
            hash.Add(TextureCoordinateSets);
            hash.Add(ColorSets);
            hash.Add(Blendshapes);
            return hash.ToHashCode();
        }

        public Vertex HardCopy()
            => new()
            {
                Weights = Weights is null ? null : new Dictionary<TransformBase, float>(Weights),
                Position = Position,
                Normal = Normal,
                Tangent = Tangent,
                TextureCoordinateSets = [.. TextureCoordinateSets],
                ColorSets = [.. ColorSets],
                Blendshapes = Blendshapes is null ? null : new Dictionary<string, VertexData>(Blendshapes),
            };
    }
}