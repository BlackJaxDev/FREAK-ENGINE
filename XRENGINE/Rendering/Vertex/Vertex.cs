﻿using Assimp;
using Extensions;
using System.Numerics;
using XREngine.Scene.Transforms;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XREngine.Data.Rendering
{
    public class Vertex : VertexData, IEquatable<Vertex>
    {
        private Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>? _weights;

        public override FaceType Type => FaceType.Points;

        /// <summary>
        /// Contains weights for each bone that influences the position of this vertex.
        /// </summary>
        public Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>? Weights
        {
            get => _weights;
            set => SetField(ref _weights, value);
        }

        public Vector3 GetWorldPosition()
        {
            if (Weights is null || Weights.Count == 0)
                return Position;

            Vector3 pos = Vector3.Zero;
            foreach ((TransformBase bone, (float weight, Matrix4x4 bindInvWorldMatrix) pair) in Weights)
                pos += Vector3.Transform(Position, pair.bindInvWorldMatrix * bone.WorldMatrix) * pair.weight;

            return pos;
        }

        public Matrix4x4 GetBoneTransformMatrix()
        {
            if (Weights is null || Weights.Count == 0)
                return Matrix4x4.Identity;

            Matrix4x4 matrix = new();
            foreach ((TransformBase bone, (float weight, Matrix4x4 bindInvWorldMatrix) pair) in Weights)
                matrix += (pair.bindInvWorldMatrix * bone.WorldMatrix) * pair.weight;

            return matrix;
        }
        public Matrix4x4 GetInverseBoneTransformMatrix()
        {
            if (Weights is null || Weights.Count == 0)
                return Matrix4x4.Identity;

            Matrix4x4 matrix = new();
            foreach ((TransformBase bone, (float weight, Matrix4x4 bindInvWorldMatrix) pair) in Weights)
                matrix += (bone.InverseWorldMatrix * pair.bindInvWorldMatrix.Inverted()) * pair.weight;

            return matrix;
        }

        /// <summary>
        /// Data this vertex can morph to, indexed by blendshape name.
        /// Data here is absolute, not deltas, for simplicity.
        /// </summary>
        public Dictionary<string, VertexData>? Blendshapes { get; set; }

        public Vertex() { }

        public Vertex(Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>? weights)
            => Weights = weights;

        public Vertex(Vector3 position)
            => Position = position;

        public Vertex(Vector3 position, Vector4 color)
        {
            Position = position;
            ColorSets.Add(color);
        }

        public Vertex(Vector3 position, Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>? weights)
            : this(position) => Weights = weights;

        public Vertex(Vector3 position, Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>? weights, Vector3 normal)
            : this(position, weights) => Normal = normal;

        public Vertex(Vector3 position, Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>? inf, Vector3 normal, Vector2 texCoord)
            : this(position, inf, normal) => TextureCoordinateSets.Add(texCoord);

        public Vertex(Vector3 position, Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>? inf, Vector3 normal, Vector2 texCoord, Vector4 color)
            : this(position, inf, normal, texCoord) => ColorSets.Add(color);

        public Vertex(Vector3 position, Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>? inf, Vector3 normal, Vector3 tangent, Vector2 texCoord, Vector4 color)
            : this(position, inf, normal, texCoord, color) => Tangent = tangent;

        public Vertex(Vector3 position, Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>? inf, Vector2 texCoord)
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
                Weights = Weights is null ? null : new Dictionary<TransformBase, (float weight, Matrix4x4 bindInvWorldMatrix)>(Weights),
                Position = Position,
                Normal = Normal,
                Tangent = Tangent,
                TextureCoordinateSets = [.. TextureCoordinateSets],
                ColorSets = [.. ColorSets],
                Blendshapes = Blendshapes is null ? null : new Dictionary<string, VertexData>(Blendshapes),
            };

        public static unsafe Vertex FromAssimp(Mesh mesh, int vertexIndex)
        {
            Vector3D pos = mesh.Vertices[vertexIndex];
            Vector3D? normal = (mesh.Normals?.TryGet(vertexIndex, out var temp1) ?? false) ? temp1 : null;
            Vector3D? tangent = (mesh.Tangents?.TryGet(vertexIndex, out var temp2) ?? false) ? temp2 : null;
            Vector3D? bitangent = (mesh.BiTangents?.TryGet(vertexIndex, out var temp3) ?? false) ? temp3 : null;

            //If two of the three vectors are zero, the normal is calculated from the cross product of the other two.
            if (normal == null)
            {
                if (tangent != null && bitangent != null)
                    normal = Vector3D.Cross(tangent.Value, bitangent.Value);
                //else if (tangent != null)
                //    normal = Vector3.Cross(tangent.Value, pos);
                //else if (bitangent != null)
                //    normal = Vector3.Cross(bitangent.Value, pos);
            }
            if (tangent == null)
            {
                if (normal != null && bitangent != null)
                    tangent = Vector3D.Cross(normal.Value, bitangent.Value);
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
                Position = pos.ToNumerics(),
                Normal = normal?.ToNumerics(),
                Tangent = tangent?.ToNumerics()
            };

            for (int i = 0; i < mesh.TextureCoordinateChannelCount; ++i)
            {
                var channel = mesh.TextureCoordinateChannels[i];
                if (channel is null || vertexIndex >= channel.Count)
                    break;

                Vector3D uv = channel[vertexIndex];
                v.TextureCoordinateSets.Add(new Vector2(uv.X, uv.Y));
            }

            for (int i = 0; i < mesh.VertexColorChannelCount; ++i)
            {
                var channel = mesh.VertexColorChannels[i];
                if (channel is null || vertexIndex >= channel.Count)
                    break;

                v.ColorSets.Add(channel[vertexIndex].ToNumerics());
            }

            //Blendshapes
            int blendshapeCount = mesh.MeshAnimationAttachmentCount;
            if (blendshapeCount > 0)
            {
                v.Blendshapes = [];
                for (int i = 0; i < blendshapeCount; ++i)
                {
                    var blendshape = mesh.MeshAnimationAttachments[i];
                    if (blendshape.VertexCount == 0)
                        continue;

                    VertexData data = new()
                    {
                        Position = blendshape.Vertices[vertexIndex].ToNumerics()
                    };

                    if (blendshape.Normals != null)
                        data.Normal = blendshape.Normals[vertexIndex].ToNumerics();

                    if (blendshape.Tangents != null)
                        data.Tangent = blendshape.Tangents[vertexIndex].ToNumerics();

                    for (int j = 0; j < blendshape.TextureCoordinateChannelCount; ++j)
                    {
                        if (blendshape.TextureCoordinateChannels[j] == null)
                            break;

                        Vector3D uv = blendshape.TextureCoordinateChannels[j][vertexIndex];
                        data.TextureCoordinateSets.Add(new Vector2(uv.X, uv.Y));
                    }
                    for (int j = 0; j < blendshape.VertexColorChannelCount; ++j)
                    {
                        if (blendshape.VertexColorChannels[j] == null)
                            break;

                        Color4D color = blendshape.VertexColorChannels[j][vertexIndex];
                        data.ColorSets.Add(color.ToNumerics());
                    }

                    string shapeName = blendshape.Name;
                    if (v.Blendshapes.ContainsKey(shapeName))
                        shapeName += $"_{i}";
                    v.Blendshapes.Add(shapeName, data);
                }
            }

            return v;
        }
    }
}