using Silk.NET.Assimp;
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

        public static unsafe Vertex FromAssimp(Mesh* mesh, uint vertexIndex)
        {
            Vector3 pos = mesh->MVertices[vertexIndex];
            Vector3? normal = mesh->MNormals == null ? null : mesh->MNormals[vertexIndex];
            Vector3? tangent = mesh->MTangents == null ? null : mesh->MTangents[vertexIndex];
            Vector3? bitangent = mesh->MBitangents == null ? null : mesh->MBitangents[vertexIndex];

            //If two of the three vectors are zero, the normal is calculated from the cross product of the other two.
            if (normal == null)
            {
                if (tangent != null && bitangent != null)
                    normal = Vector3.Cross(tangent.Value, bitangent.Value);
                //else if (tangent != null)
                //    normal = Vector3.Cross(tangent.Value, pos);
                //else if (bitangent != null)
                //    normal = Vector3.Cross(bitangent.Value, pos);
            }
            if (tangent == null)
            {
                if (normal != null && bitangent != null)
                    tangent = Vector3.Cross(normal.Value, bitangent.Value);
                //else if (normal != null)
                //    tangent = Vector3.Cross(normal.Value, pos);
                //else if (bitangent != null)
                //    tangent = Vector3.Cross(bitangent.Value, pos);
            }
            //We don't save the bitangent, as it can be calculated from the normal and tangent on the GPU.
            //if (bitangent == null)
            //{
            //    if (normal != null && tangent != null)
            //        bitangent = Vector3.Cross(normal.Value, tangent.Value);
            //    else if (normal != null)
            //        bitangent = Vector3.Cross(normal.Value, pos);
            //    else if (tangent != null)
            //        bitangent = Vector3.Cross(tangent.Value, pos);
            //}

            Vertex v = new()
            {
                Position = pos,
                Normal = normal,
                Tangent = tangent
            };

            for (int i = 0; i < 8; ++i)
            {
                if (mesh->MTextureCoords[i] == null)
                    break;

                Vector3 uv = mesh->MTextureCoords[i][vertexIndex];
                v.TextureCoordinateSets.Add(new Vector2(uv.X, uv.Y));
            }
            for (int i = 0; i < 8; ++i)
            {
                if (mesh->MColors[i] == null)
                    break;

                Vector4 color = mesh->MColors[i][vertexIndex];
                v.ColorSets.Add(color);
            }

            //Blendshapes
            uint blendshapeCount = mesh->MNumAnimMeshes;
            if (blendshapeCount > 0)
            {
                v.Blendshapes = [];
                for (uint i = 0; i < blendshapeCount; ++i)
                {
                    var blendshape = mesh->MAnimMeshes[i];
                    if (blendshape->MVertices == null)
                        continue;

                    VertexData data = new()
                    {
                        Position = blendshape->MVertices[vertexIndex]
                    };

                    if (blendshape->MNormals != null)
                        data.Normal = blendshape->MNormals[vertexIndex];

                    if (blendshape->MTangents != null)
                        data.Tangent = blendshape->MTangents[vertexIndex];

                    for (int j = 0; j < 8; ++j)
                    {
                        if (blendshape->MTextureCoords[j] == null)
                            break;

                        Vector3 uv = blendshape->MTextureCoords[j][vertexIndex];
                        data.TextureCoordinateSets.Add(new Vector2(uv.X, uv.Y));
                    }
                    for (int j = 0; j < 8; ++j)
                    {
                        if (blendshape->MColors[j] == null)
                            break;

                        Vector4 color = blendshape->MColors[j][vertexIndex];
                        data.ColorSets.Add(color);
                    }

                    string shapeName = blendshape->MName.ToString();
                    if (v.Blendshapes.ContainsKey(shapeName))
                        shapeName += $"_{i}";
                    v.Blendshapes.Add(shapeName, data);
                }
            }

            return v;
        }
    }
}