using Extensions;
using System.Collections;
using System.ComponentModel;
using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering
{
    /// <summary>
    /// This class contains buffer-organized mesh data that can be rendered using an XRMeshRenderer.
    /// </summary>
    public partial class XRMesh : XRObjectBase, IEnumerable<XRDataBuffer>
    {
        public XREvent<XRMesh> DataChanged;

        public XRMesh()
        {
            _utilizedBones = [];
            _weightsPerVertex = [];
            _faceIndices = [];
        }

        /// <summary>
        /// This constructor converts a simple list of vertices into a mesh optimized for rendering.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="primitives"></param>
        /// <param name="type"></param>
        public XRMesh(IEnumerable<VertexPrimitive> primitives)
        {
            //TODO: convert triangles to tristrips and use primitive restart to render them all in one call? is this more efficient?

            //Convert all primitives to simple primitives
            List<Vertex> points = [];
            List<Vertex> lines = [];
            List<Vertex> triangles = [];

            Dictionary<TransformBase, float>[]? weights = null;
            Vector3[] posBuffer;
            Vector3[]? normBuffer = null;
            Vector3[]? tanBuffer = null;
            Vector4[][]? colorBuffers = null;
            Vector2[][]? uvBuffers = null;

            //Create an action for each vertex attribute to set the buffer data
            //This lets us avoid redundant LINQ code by looping through the vertices only once
            List<Action<int, int, Vertex>> vertexActions = [];

            bool hasSkinning = false;
            bool hasNormals = false;
            bool hasTangents = false;
            int maxColorCount = 0;
            int maxTexCoordCount = 0;
            bool hasBlendshapes = false;
            _bounds = new AABB(Vector3.Zero, Vector3.Zero);

            //For each vertex, we double check what data it has and add verify that the corresponding add-to-buffer action is added
            void AddVertex(List<Vertex> vertices, Vertex v)
            {
                vertices.Add(v);
                _bounds.ExpandToInclude(v.Position);

                if (!hasSkinning && v.Weights is not null && v.Weights.Count > 0)
                {
                    hasSkinning = true;
                    vertexActions.Add((i, x, vtx) =>
                    {
                        weights![i] = vtx.Weights ?? [];
                    });
                }

                if (!hasNormals && v.Normal is not null)
                {
                    hasNormals = true;
                    vertexActions.Add((i, x, vtx) =>
                    {
                        normBuffer![i] = vtx.Normal ?? Vector3.Zero;
                    });
                }

                if (!hasTangents && v.Tangent is not null)
                {
                    hasTangents = true;
                    vertexActions.Add((i, x, vtx) =>
                    {
                        tanBuffer![i] = vtx.Tangent ?? Vector3.Zero;
                    });
                }

                maxTexCoordCount = Math.Max(maxTexCoordCount, v.TextureCoordinateSets.Count);
                if (v.TextureCoordinateSets is not null && v.TextureCoordinateSets.Count > 0)
                {
                    vertexActions.Add((i, x, vtx) =>
                    {
                        for (int texCoordIndex = 0; texCoordIndex < v.TextureCoordinateSets.Count; ++texCoordIndex)
                            uvBuffers![texCoordIndex][i] = vtx.TextureCoordinateSets != null && texCoordIndex < vtx.TextureCoordinateSets.Count
                                ? vtx.TextureCoordinateSets[texCoordIndex]
                                : Vector2.Zero;
                    });
                }

                maxColorCount = Math.Max(maxColorCount, v.ColorSets.Count);
                if (v.ColorSets is not null && v.ColorSets.Count > 0)
                {
                    vertexActions.Add((i, x, vtx) =>
                    {
                        for (int colorIndex = maxColorCount; colorIndex < v.ColorSets.Count; ++colorIndex)
                            colorBuffers![colorIndex][i] = vtx.ColorSets != null && colorIndex < vtx.ColorSets.Count
                                ? vtx.ColorSets[colorIndex]
                                : Vector4.Zero;
                    });
                }

                if (!hasBlendshapes && v.Blendshapes is not null && v.Blendshapes.Count > 0)
                {
                    hasBlendshapes = true;
                    vertexActions.Add((i, x, vtx) =>
                    {
                        foreach (var pair in vtx.Blendshapes!)
                        {
                            string name = pair.Key;
                            var data = pair.Value;
                            Vector3 deltaPos = data.Position - vtx.Position;
                            Vector3? deltaNorm = data.Normal - vtx.Normal;
                            Vector3? deltaTan = data.Tangent - vtx.Tangent;
                            //List<Vector4> colors = data.ColorSets;
                            //List<Vector2> texCoords = data.TextureCoordinateSets;

                        }
                    });
                }
            }

            //Convert all primitives to simple primitives
            //While doing this, compile a command list of actions to set buffer data
            foreach (VertexPrimitive prim in primitives)
            {
                switch (prim)
                {
                    case Vertex v:
                        AddVertex(points, v);
                        break;
                    case VertexLinePrimitive l:
                        {
                            var asLines = l.ToLines();
                            foreach (VertexLine line in asLines)
                                foreach (Vertex v in line.Vertices)
                                    AddVertex(lines, v);
                        }
                        break;
                    case VertexPolygon t:
                        {
                            var asTris = t.ToTriangles();
                            foreach (VertexTriangle tri in asTris)
                                foreach (Vertex v in tri.Vertices)
                                    AddVertex(triangles, v);
                        }
                        break;
                }
            }

            //Remap vertices to unique indices for each type of simple primitive
            Remapper? triRemap = SetTriangleIndices(triangles);
            Remapper? lineRemap = SetLineIndices(lines);
            Remapper? pointRemap = SetPointIndices(points);

            //Determine which type of primitive has the most data and use that as the primary type
            int count;
            Remapper? remapper;
            List<Vertex> sourceList;
            if (triangles.Count > lines.Count && triangles.Count > points.Count)
            {
                _type = EPrimitiveType.Triangles;
                count = triangles.Count;
                remapper = triRemap;
                sourceList = triangles;
            }
            else if (lines.Count > triangles.Count && lines.Count > points.Count)
            {
                _type = EPrimitiveType.Lines;
                count = lines.Count;
                remapper = lineRemap;
                sourceList = lines;
            }
            else
            {
                _type = EPrimitiveType.Points;
                count = points.Count;
                remapper = pointRemap;
                sourceList = points;
            }

            //Generate the unallocated buffers
            int[] firstAppearanceArray;
            if (remapper?.ImplementationTable is null)
            {
                firstAppearanceArray = new int[count];
                for (int i = 0; i < count; ++i)
                    firstAppearanceArray[i] = i;
            }
            else
                firstAppearanceArray = remapper.ImplementationTable!;

            _faceIndices = new VertexIndices[firstAppearanceArray.Length];
            _faceIndices.Fill();

            InitBuffers(
                ref weights,
                out posBuffer,
                ref normBuffer,
                ref tanBuffer,
                ref colorBuffers,
                ref uvBuffers,
                hasSkinning,
                hasNormals,
                hasTangents,
                maxColorCount,
                maxTexCoordCount,
                firstAppearanceArray.Length);

            vertexActions.Add((i, x, vtx) => posBuffer[i] = vtx.Position);

            //Fill the buffers with the vertex data using the command list
            //We can do this in parallel since each vertex is independent
            Parallel.For(0, firstAppearanceArray.Length, i =>
            //for (int i = 0; i < firstAppearanceArray.Length; ++i)
            {
                int x = firstAppearanceArray[i];
                Vertex vtx = sourceList[x];
                foreach (var action in vertexActions)
                    action.Invoke(i, x, vtx);
            });

            if (weights is not null)
                SetBoneWeights(weights);

            string binding = ECommonBufferType.Position.ToString();
            PositionsBuffer = new XRDataBuffer(binding, EBufferTarget.ArrayBuffer, false);
            PositionsBuffer.SetDataRaw(posBuffer);
            Buffers.Add(binding, PositionsBuffer);

            if (normBuffer is not null)
            {
                binding = ECommonBufferType.Normal.ToString();
                NormalsBuffer = new XRDataBuffer(binding, EBufferTarget.ArrayBuffer, false);
                NormalsBuffer.SetDataRaw(normBuffer);
                Buffers.Add(binding, NormalsBuffer);
            }

            if (tanBuffer is not null)
            {
                binding = ECommonBufferType.Tangent.ToString();
                TangentsBuffer = new XRDataBuffer(binding, EBufferTarget.ArrayBuffer, false);
                TangentsBuffer.SetDataRaw(tanBuffer);
                Buffers.Add(binding, TangentsBuffer);
            }

            if (colorBuffers is not null)
            {
                for (int colorIndex = 0; colorIndex < colorBuffers.Length; ++colorIndex)
                {
                    binding = $"{ECommonBufferType.Color}{colorIndex}";
                    ColorBuffers[colorIndex] = new XRDataBuffer(binding, EBufferTarget.ArrayBuffer, false);
                    ColorBuffers[colorIndex].SetDataRaw(colorBuffers[colorIndex]);
                    Buffers.Add(binding, ColorBuffers[colorIndex]);
                }
            }

            if (uvBuffers is not null)
            {
                for (int texCoordIndex = 0; texCoordIndex < uvBuffers.Length; ++texCoordIndex)
                {
                    binding = $"{ECommonBufferType.TexCoord}{texCoordIndex}";
                    TexCoordBuffers[texCoordIndex] = new XRDataBuffer(binding, EBufferTarget.ArrayBuffer, false);
                    TexCoordBuffers[texCoordIndex].SetDataRaw(uvBuffers[texCoordIndex]);
                    Buffers.Add(binding, TexCoordBuffers[texCoordIndex]);
                }
            }
        }

        private void InitBuffers(
            ref Dictionary<TransformBase, float>[]? weights,
            out Vector3[] posBuffer,
            ref Vector3[]? normBuffer,
            ref Vector3[]? tanBuffer,
            ref Vector4[][]? colorBuffers,
            ref Vector2[][]? uvBuffers,
            bool hasSkinning,
            bool hasNormals,
            bool hasTangents,
            int maxColorCount,
            int maxTexCoordCount,
            int firstAppearanceArrayCount)
        {
            posBuffer = new Vector3[firstAppearanceArrayCount];

            if (hasSkinning)
                weights = new Dictionary<TransformBase, float>[firstAppearanceArrayCount];

            if (hasNormals)
                normBuffer = new Vector3[firstAppearanceArrayCount];

            if (hasTangents)
                tanBuffer = new Vector3[firstAppearanceArrayCount];

            if (maxColorCount > 0)
            {
                colorBuffers = new Vector4[maxColorCount][];
                ColorBuffers = new XRDataBuffer[maxColorCount];
                for (int colorIndex = 0; colorIndex < maxColorCount; ++colorIndex)
                {
                    if (colorBuffers[colorIndex] is null)
                        colorBuffers[colorIndex] = new Vector4[firstAppearanceArrayCount];
                }
            }

            if (maxTexCoordCount > 0)
            {
                uvBuffers = new Vector2[maxTexCoordCount][];
                TexCoordBuffers = new XRDataBuffer[maxTexCoordCount];
                for (int texCoordIndex = 0; texCoordIndex < maxTexCoordCount; ++texCoordIndex)
                {
                    if (uvBuffers[texCoordIndex] is null)
                        uvBuffers[texCoordIndex] = new Vector2[firstAppearanceArrayCount];
                }
            }
        }

        public bool HasSkinning => _utilizedBones is not null && _utilizedBones.Length > 0;

        //Specific common buffers

        #region Per-Facepoint Buffers

        #region Data Buffers
        public XRDataBuffer? PositionsBuffer { get; private set; } //Required
        public XRDataBuffer? NormalsBuffer { get; private set; }
        public XRDataBuffer? TangentsBuffer { get; private set; }
        public XRDataBuffer[]? ColorBuffers { get; private set; } = [];
        public XRDataBuffer[]? TexCoordBuffers { get; private set; } = [];
        #endregion

        //On-GPU transformations
        /// <summary>
        /// Offset into the bone index/weight buffers to find each bone weighted to this vertex.
        /// </summary>
        public XRDataBuffer? BoneWeightOffsets { get; private set; }
        /// <summary>
        /// Number of indices/weights in the bone index/weight buffers for each vertex.
        /// </summary>
        public XRDataBuffer? BoneWeightCounts { get; private set; }
        /// <summary>
        /// Offset into the blendshape index/weight buffers to find each blendshape that actually changes this vertex.
        /// </summary>
        public XRDataBuffer? BlendshapeOffsets { get; private set; }
        /// <summary>
        /// Number of indices/weights in the blendshape index/weight buffers for each vertex.
        /// </summary>
        public XRDataBuffer? BlendshapeCounts { get; private set; }

        #endregion

        #region Non-Per-Facepoint Buffers

        #region Bone Weighting Buffers
        //Bone weights
        /// <summary>
        /// Indices into the UtilizedBones list for each bone that affects this vertex.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BoneWeightIndices { get; private set; }
        /// <summary>
        /// Weight values from 0.0 to 1.0 for each bone that affects this vertex.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BoneWeightValues { get; private set; }
        #endregion

        #region Blendshape Buffers
        //Deltas for each blendshape on this mesh
        /// <summary>
        /// Remapped array of position deltas for all blendshapes on this mesh.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BlendshapePositionDeltasBuffer { get; private set; }
        /// <summary>
        /// Remapped array of normal deltas for all blendshapes on this mesh.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BlendshapeNormalDeltasBuffer { get; private set; }
        /// <summary>
        /// Remapped array of tangent deltas for all blendshapes on this mesh.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BlendshapeTangentDeltasBuffer { get; private set; }
        /// <summary>
        /// Remapped array of color deltas for all blendshapes on this mesh.
        /// Static read-only buffers.
        /// </summary>
        public XRDataBuffer[]? BlendshapeColorDeltaBuffers { get; private set; } = [];
        /// <summary>
        /// Remapped array of texture coordinate deltas for all blendshapes on this mesh.
        /// Static read-only buffers.
        /// </summary>
        public XRDataBuffer[]? BlendshapeTexCoordDeltaBuffers { get; private set; } = [];
        //Weights for each blendshape on this mesh
        /// <summary>
        /// Indices into the blendshape delta buffers for each blendshape that affects each vertex.
        /// Static read-only buffer.
        /// </summary>
        public XRDataBuffer? BlendshapeIndices { get; private set; }
        #endregion

        #endregion

        public EventDictionary<string, XRDataBuffer> Buffers
        {
            get => _buffers;
            set => SetField(ref _buffers, value);
        }

        [Browsable(false)]
        public VertexWeightGroup[] Weights
        {
            get => _weightsPerVertex;
            set => SetField(ref _weightsPerVertex, value);
        }

        [Browsable(false)]
        public VertexIndices[] FaceIndices
        {
            get => _faceIndices;
            set => SetField(ref _faceIndices, value);
        }

        [Browsable(false)]
        public List<IndexTriangle>? Triangles
        {
            get => _triangles;
            set => SetField(ref _triangles, value);
        }

        [Browsable(false)]
        public List<IndexLine>? Lines
        {
            get => _lines;
            set => SetField(ref _lines, value);
        }

        [Browsable(false)]
        public List<IndexPoint>? Points
        {
            get => _points;
            set => SetField(ref _points, value);
        }

        [Browsable(false)]
        public EPrimitiveType Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        public TransformBase[] UtilizedBones
        {
            get => _utilizedBones;
            set => SetField(ref _utilizedBones, value);
        }

        public bool IsSingleBound => _utilizedBones is not null && _utilizedBones.Length == 1;
        public bool IsUnskinned => _utilizedBones is null || _utilizedBones.Length == 0;
        public uint BlendshapeCount { get; set; } = 0u;

        private TransformBase[] _utilizedBones = [];
        private VertexWeightGroup[] _weightsPerVertex = [];
        //This is the buffer data that will be passed to the shader.
        //Each buffer may have repeated values, as there must be a value for each remapped face point.
        //The key is the binding name and the value is the buffer.
        private EventDictionary<string, XRDataBuffer> _buffers = [];
        //Face data last
        //Face points have indices that refer to each buffer.
        //These may contain repeat buffer indices but each point is unique.
        private VertexIndices[] _faceIndices;

        //Each point, line and triangle has indices that refer to the face indices array.
        //These may contain repeat vertex indices but each primitive is unique.
        private List<IndexPoint>? _points = null;
        private List<IndexLine>? _lines = null;
        private List<IndexTriangle>? _triangles = null;

        private EPrimitiveType _type = EPrimitiveType.Triangles;
        private AABB _bounds = new AABB(Vector3.Zero, Vector3.Zero);

        public void GetTriangle(int index, out VertexIndices point0, out VertexIndices point1, out VertexIndices point2)
        {
            if (_triangles is null)
                throw new InvalidOperationException();

            if (index < 0 || index >= _triangles.Count)
                throw new IndexOutOfRangeException();

            IndexTriangle face = _triangles[index];
            point0 = _faceIndices[face.Point0];
            point1 = _faceIndices[face.Point1];
            point2 = _faceIndices[face.Point2];
        }
        public void SetTriangle(int index, VertexIndices point0, VertexIndices point1, VertexIndices point2)
        {
            if (_triangles is null)
                throw new InvalidOperationException();

            if (index < 0 || index >= _triangles.Count)
                throw new IndexOutOfRangeException();

            IndexTriangle face = _triangles[index];
            _faceIndices[face.Point0] = point0;
            _faceIndices[face.Point1] = point1;
            _faceIndices[face.Point2] = point2;
        }
        public void GetLine(int index, out VertexIndices point0, out VertexIndices point1)
        {
            if (_lines is null)
                throw new InvalidOperationException();

            if (index < 0 || index >= _lines.Count)
                throw new IndexOutOfRangeException();

            IndexLine line = _lines[index];
            point0 = _faceIndices[line.Point0];
            point1 = _faceIndices[line.Point1];
        }
        public void SetLine(int index, VertexIndices point0, VertexIndices point1)
        {
            if (_lines is null)
                throw new InvalidOperationException();

            if (index < 0 || index >= _lines.Count)
                throw new IndexOutOfRangeException();

            IndexLine line = _lines[index];
            _faceIndices[line.Point0] = point0;
            _faceIndices[line.Point1] = point1;
        }
        public void GetPoint(int index, out VertexIndices point)
        {
            if (_points is null)
                throw new InvalidOperationException();

            if (index < 0 || index >= _points.Count)
                throw new IndexOutOfRangeException();

            point = _faceIndices[_points[index]];
        }
        public void SetPoint(int index, VertexIndices point)
        {
            if (_points is null)
                throw new InvalidOperationException();

            if (index < 0 || index >= _points.Count)
                throw new IndexOutOfRangeException();

            _faceIndices[_points[index]] = point;
        }

        //private static Dictionary<Type, EPrimitiveType> PrimTypeDic { get; }
        //    = new Dictionary<Type, EPrimitiveType>()
        //    {
        //        { typeof(VertexQuad), EPrimitiveType.Quads },
        //        { typeof(VertexTriangle), EPrimitiveType.Triangles },
        //        { typeof(VertexTriangleFan), EPrimitiveType.TriangleFan },
        //        { typeof(VertexTriangleStrip), EPrimitiveType.TriangleStrip },
        //        { typeof(VertexLine), EPrimitiveType.Lines },
        //        { typeof(VertexLineStrip), EPrimitiveType.LineStrip },
        //        { typeof(Vertex), EPrimitiveType.Points },
        //    };

        //private static Dictionary<EPrimitiveType, Func<IEnumerable<VertexPrimitive>, IEnumerable<Vertex>>> PrimConvDic { get; }
        //    = new Dictionary<EPrimitiveType, Func<IEnumerable<VertexPrimitive>, IEnumerable<Vertex>>>()
        //    {
        //        { EPrimitiveType.Quads, p => p.SelectMany(x => ((VertexQuad)x).ToTriangles()).SelectMany(x => x.Vertices) },
        //        { EPrimitiveType.Triangles, p => p.SelectMany(x => x.Vertices) },
        //        { EPrimitiveType.TriangleFan, p => p.SelectMany(x => ((VertexTriangleFan)x).ToTriangles()).SelectMany(x => x.Vertices) },
        //        { EPrimitiveType.TriangleStrip, p => p.SelectMany(x => ((VertexTriangleStrip)x).ToTriangles()).SelectMany(x => x.Vertices) },
        //        { EPrimitiveType.Lines, p => p.SelectMany(x => x.Vertices) },
        //        { EPrimitiveType.LineStrip, p => p.SelectMany(x => ((VertexLineStrip)x).ToLines()).SelectMany(x => x.Vertices) },
        //        { EPrimitiveType.Points, p => p.Select(x => (Vertex)x) },
        //    };

        /// <summary>
        /// The axis-aligned bounds of this mesh before any vertex transformations.
        /// </summary>
        public AABB Bounds
        {
            get => _bounds;
            private set => _bounds = value;
        }
        //private static EPrimitiveType ConvertType(EPrimitiveType type)
        //    => type switch
        //    {
        //        EPrimitiveType.Quads or
        //        EPrimitiveType.QuadStrip or
        //        EPrimitiveType.Triangles or
        //        EPrimitiveType.TriangleFan or
        //        EPrimitiveType.TriangleStrip => EPrimitiveType.Triangles,

        //        EPrimitiveType.Lines or
        //        EPrimitiveType.LineLoop or
        //        EPrimitiveType.LineStrip => EPrimitiveType.Lines,

        //        _ => EPrimitiveType.Points,
        //    };

        public static XRMesh Create<T>(params T[] prims) where T : VertexPrimitive
        {
            //No more need to convert types, this is handled by the constructor now
            //if (!GetPrimType<T>(out EPrimitiveType type))
            //    type = EPrimitiveType.Triangles;

            //return new(PrimConvDic[type](prims));
            return new(prims);
        }

        public static XRMesh Create<T>(IEnumerable<T> prims) where T : VertexPrimitive
            => new(prims);
        public static XRMesh CreateTriangles(params Vector3[] positions)
            => new(positions.SelectEvery(3, x => new VertexTriangle(x[0], x[1], x[2])));
        public static XRMesh CreateTriangles(IEnumerable<Vector3> positions)
            => new(positions.SelectEvery(3, x => new VertexTriangle(x[0], x[1], x[2])));
        public static XRMesh CreateLines(params Vector3[] positions)
            => new(positions.SelectEvery(2, x => new VertexLine(x[0], x[1])));
        public static XRMesh CreateLines(IEnumerable<Vector3> positions)
            => new(positions.SelectEvery(2, x => new VertexLine(x[0], x[1])));
        public static XRMesh CreatePoints(params Vector3[] positions)
            => new(positions.Select(x => new Vertex(x)));
        public static XRMesh CreatePoints(IEnumerable<Vector3> positions)
            => new(positions.Select(x => new Vertex(x)));

        //private static bool GetPrimType<T>(out EPrimitiveType type) where T : VertexPrimitive
        //    => PrimTypeDic.TryGetValue(typeof(T), out type);

        public void RemoveBuffer(string name)
        {
            if (_buffers is null)
                return;

            if (_buffers.TryGetValue(name, out XRDataBuffer? buffer))
            {
                _buffers.Remove(name);
                buffer.Dispose();
            }
        }

        //public VertexTriangle? GetFace(int index)
        //{
        //    IndexTriangle? face = _triangles?[index];
        //    if (face is null)
        //        return null;

        //    return new VertexTriangle(
        //        BuffersToVertex(_faceIndices[face.Point0]),
        //        BuffersToVertex(_faceIndices[face.Point1]),
        //        BuffersToVertex(_faceIndices[face.Point2]));
        //}

        //public VertexLine? GetLine(int index)
        //{
        //    IndexLine? line = _lines?[index];
        //    if (line is null)
        //        return null;

        //    return new VertexLine(
        //        BuffersToVertex(_faceIndices[line.Value.Point0]),
        //        BuffersToVertex(_faceIndices[line.Value.Point1]));
        //}

        //public Vertex? GetPoint(int index)
        //{
        //    IndexPoint? point = _points?[index];
        //    if (point is null)
        //        return null;

        //    return BuffersToVertex(_faceIndices[point.Value]);
        //}

        //public void VertexToBuffers(Vertex v, VertexIndices facepoint)
        //{
        //    if (facepoint.BufferBindings is null)
        //        return;

        //    for (int i = 0; i < facepoint.BufferBindings.Count; ++i)
        //    {
        //        XRDataBuffer b = Buffers[i];
        //        uint index = facepoint.BufferBindings[i];
        //        var binding = b.Binding;
        //        switch (binding)
        //        {
        //            case EBufferType.Position:
        //                b.Set(index, v.Position);
        //                break;
        //            case EBufferType.Normal:
        //                b.Set(index, v.Normal ?? Vector3.Zero);
        //                break;
        //            //case EBufferType.Binormal:
        //            //    b.Set(index, Binormal);
        //            //    break;
        //            case EBufferType.Tangent:
        //                b.Set(index, v.Tangent ?? Vector3.Zero);
        //                break;
        //            case EBufferType.Color:
        //                //b.Set(index, Color);
        //                break;
        //            case EBufferType.TexCoord:
        //                //b.Set(index, TexCoord);
        //                break;
        //        }
        //    }
        //}

        /// <summary>
        /// Sets the vertex data from the buffers into the vertex using the facepoint to match indices.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="facepoint"></param>
        /// <param name="buffers"></param>
        //public void BuffersToVertex(Vertex v, VertexIndices facepoint)
        //{
        //    if (facepoint.BufferBindings is null)
        //        return;

        //    for (int i = 0; i < facepoint.BufferBindings.Count; ++i)
        //    {
        //        XRDataBuffer b = Buffers[i];
        //        uint index = facepoint.BufferBindings[i];
        //        EBufferType type = b.Binding;
        //        switch (type)
        //        {
        //            case EBufferType.Position:
        //                v.Position = b.Get<Vector3>(index * 12) ?? Vector3.Zero;
        //                break;
        //            case EBufferType.Normal:
        //                v.Normal = b.Get<Vector3>(index * 12);
        //                break;
        //            //case EBufferType.Binormal:
        //            //    Binormal = b.Get<Vector3>(index * 12);
        //            //    break;
        //            case EBufferType.Tangent:
        //                v.Tangent = b.Get<Vector3>(index * 12);
        //                break;
        //            case EBufferType.Color:
        //                //TexCoord = b.Get<Vector2>(index << 4);
        //                break;
        //            case EBufferType.TexCoord:
        //                //TexCoord = b.Get<Vector2>(index << 3);
        //                break;
        //        }
        //    }
        //}
        /// <summary>
        /// Sets the vertex data from the buffers into the vertex using the facepoint to match indices.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="facepoint"></param>
        /// <param name="buffers"></param>
        //public Vertex BuffersToVertex(VertexIndices facepoint)
        //{
        //    Vertex v = new();
        //    if (facepoint.BufferBindings is null)
        //        return v;

        //    for (int i = 0; i < facepoint.BufferBindings.Count; ++i)
        //    {
        //        XRDataBuffer b = Buffers[i];
        //        uint index = facepoint.BufferBindings[i];
        //        EBufferType type = b.Binding;
        //        switch (type)
        //        {
        //            case EBufferType.Position:
        //                v.Position = b.Get<Vector3>(index * 12) ?? Vector3.Zero;
        //                break;
        //            case EBufferType.Normal:
        //                v.Normal = b.Get<Vector3>(index * 12);
        //                break;
        //            //case EBufferType.Binormal:
        //            //    Binormal = b.Get<Vector3>(index * 12);
        //            //    break;
        //            case EBufferType.Tangent:
        //                v.Tangent = b.Get<Vector3>(index * 12);
        //                break;
        //            case EBufferType.Color:
        //                //TexCoord = b.Get<Vector2>(index << 4);
        //                break;
        //            case EBufferType.TexCoord:
        //                //TexCoord = b.Get<Vector2>(index << 3);
        //                break;
        //        }
        //    }
        //    return v;
        //}

        //public void GenerateTangents(int positionIndex, int normalIndex, int uvIndex, bool addTangents)
        //{
        //    if (_triangles is null)
        //        return;

        //    XRDataBuffer[] pBuffs = GetAllBuffersOfType(EBufferType.Position);
        //    if (pBuffs.Length == 0)
        //    {
        //        //Engine.LogWarning("No position buffers found.");
        //        return;
        //    }
        //    if (!pBuffs.IndexInRangeArrayT(positionIndex))
        //    {
        //        //Engine.LogWarning("Position index out of range of available position buffers.");
        //        return;
        //    }
        //    XRDataBuffer[] nBuffs = GetAllBuffersOfType(EBufferType.Normal);
        //    if (nBuffs.Length == 0)
        //    {
        //        //Engine.LogWarning("No normal buffers found.");
        //        return;
        //    }
        //    if (!nBuffs.IndexInRangeArrayT(normalIndex))
        //    {
        //        //Engine.LogWarning("Normal index out of range of available normal buffers.");
        //        return;
        //    }
        //    XRDataBuffer[] tBuffs = GetAllBuffersOfType(EBufferType.TexCoord);
        //    if (tBuffs.Length == 0)
        //    {
        //        //Engine.LogWarning("No texcoord buffers found.");
        //        return;
        //    }
        //    if (!tBuffs.IndexInRangeArrayT(uvIndex))
        //    {
        //        //Engine.LogWarning("UV index out of range of available texcoord buffers.");
        //        return;
        //    }

        //    Vector3 pos1, pos2, pos3;
        //    //Vector3 n0, n1, n2;
        //    Vector2 uv1, uv2, uv3;

        //    XRDataBuffer pBuff = pBuffs[positionIndex];
        //    //VertexBuffer nBuff = pBuffs[normalIndex];
        //    XRDataBuffer tBuff = pBuffs[uvIndex];
        //    int pointCount = _triangles.Count * 3;
        //    //List<Vector3> binormals = new(pointCount);
        //    List<Vector3> tangents = new(pointCount);
            
        //    for (int i = 0; i < _triangles.Count; ++i)
        //    {
        //        IndexTriangle t = _triangles[i];

        //        VertexIndices fp0 = _faceIndices[t.Point0];
        //        VertexIndices fp1 = _faceIndices[t.Point1];
        //        VertexIndices fp2 = _faceIndices[t.Point2];

        //        pos1 = pBuff.Get<Vector3>(fp0.BufferBindings[pBuff.Index] * 12) ?? Vector3.Zero;
        //        pos2 = pBuff.Get<Vector3>(fp1.BufferBindings[pBuff.Index] * 12) ?? Vector3.Zero;
        //        pos3 = pBuff.Get<Vector3>(fp2.BufferBindings[pBuff.Index] * 12) ?? Vector3.Zero;

        //        uv1 = tBuff.Get<Vector2>(fp0.BufferBindings[tBuff.Index] * 8) ?? Vector2.Zero;
        //        uv2 = tBuff.Get<Vector2>(fp1.BufferBindings[tBuff.Index] * 8) ?? Vector2.Zero;
        //        uv3 = tBuff.Get<Vector2>(fp2.BufferBindings[tBuff.Index] * 8) ?? Vector2.Zero;

        //        Vector3 deltaPos1 = pos2 - pos1;
        //        Vector3 deltaPos2 = pos3 - pos1;

        //        Vector2 deltaUV1 = uv2 - uv1;
        //        Vector2 deltaUV2 = uv3 - uv1;

        //        Vector3 tangent;
        //        //Vector3 binormal;

        //        float m = deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X;
        //        if (m == 0.0f)
        //        {
        //            tangent = Globals.Up;
        //            //binormal = Globals.Up;
        //        }
        //        else
        //        {
        //            float r = 1.0f / m;
        //            tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
        //            //binormal = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;
        //        }

        //        //3 for each triangle
        //        //binormals.Add(binormal);
        //        //binormals.Add(binormal);
        //        //binormals.Add(binormal);

        //        //3 for each triangle
        //        tangents.Add(tangent);
        //        tangents.Add(tangent);
        //        tangents.Add(tangent);
        //    }

        //    //AddBuffer(binormals, new VertexAttribInfo(EBufferType.Binormal));
        //    //_bufferInfo.HasBinormals = true;

        //    if (addTangents)
        //    {
        //        AddBuffer(tangents, new VertexAttribInfo(EBufferType.Tangent));
        //        _bufferInfo.HasTangents = true;
        //    }

        //    OnBufferInfoChanged();
        //}
        private void SetBoneWeights(params Dictionary<TransformBase, float>[] weightsPerVertex)
        {
            Dictionary<TransformBase, int> boneToIndexTable = [];
            _weightsPerVertex = new VertexWeightGroup[weightsPerVertex.Length];
            int boneIndex = 0;
            for (int i = 0; i < weightsPerVertex.Length; i++)
            {
                //TODO: remap?
                _faceIndices[i].WeightIndex = i;
                Dictionary<int, float> weights = [];

                Dictionary<TransformBase, float> w = weightsPerVertex[i];
                foreach (var pair in w)
                {
                    var bone = pair.Key;
                    var weight = pair.Value;
                    if (!boneToIndexTable.ContainsKey(bone))
                        boneToIndexTable.Add(bone, boneIndex++);
                    weights.Add(boneToIndexTable[bone], weight);
                }
            }
            _utilizedBones = [.. boneToIndexTable.Keys];
        }
        
        #region Buffers

        public XRDataBuffer? this[string bindingName]
        {
            get => _buffers.TryGetValue(bindingName, out XRDataBuffer? buffer) ? buffer : null;
            set
            {
                if (value is null)
                    _buffers.Remove(bindingName);
                else if (!_buffers.TryAdd(bindingName, value))
                    _buffers[bindingName] = value;
            }
        }

        public XRDataBuffer SetBufferRaw<T>(
            IList<T> bufferData,
            string bindingName,
            bool remap = false,
            bool integral = false,
            bool isMapped = false,
            uint instanceDivisor = 0,
            EBufferTarget target = EBufferTarget.ArrayBuffer) where T : struct
        {
            XRDataBuffer buffer = new(bindingName, target, integral)
            {
                InstanceDivisor = instanceDivisor,
                Mapped = isMapped,
            };
            AddOrUpdateBufferRaw(
                bufferData,
                bindingName,
                remap,
                instanceDivisor,
                buffer);
            return buffer;
        }

        public XRDataBuffer SetBuffer<T>(
            IList<T> bufferData,
            string bindingName,
            bool remap = false,
            bool integral = false,
            bool isMapped = false,
            uint instanceDivisor = 0,
            EBufferTarget target = EBufferTarget.ArrayBuffer) where T : unmanaged, IBufferable
        {
            _buffers ??= [];
            XRDataBuffer buffer = new(bindingName, target, integral)
            {
                InstanceDivisor = instanceDivisor,
                Mapped = isMapped
            };
            AddOrUpdateBuffer(bufferData, bindingName, remap, instanceDivisor, buffer);
            return buffer;
        }

        public Remapper? GetBuffer<T>(string bindingName, out T[]? array, bool remap = false) where T : unmanaged, IBufferable
        {
            array = null;
            return _buffers.TryGetValue(bindingName, out var buffer) ? buffer.GetData(out array, remap) : null;
        }

        public Remapper? GetBufferRaw<T>(string bindingName, out T[]? array, bool remap = false) where T : struct
        {
            array = null;
            return _buffers.TryGetValue(bindingName, out var buffer) ? buffer.GetDataRaw(out array, remap) : null;
        }

        private void AddOrUpdateBufferRaw<T>(IList<T> bufferData, string bindingName, bool remap, uint instanceDivisor, XRDataBuffer buffer) where T : struct
        {
            if (!_buffers.TryAdd(bindingName, buffer))
                _buffers[bindingName] = buffer;
            UpdateFaceIndices(bufferData.Count, bindingName, remap, instanceDivisor, buffer.SetDataRaw(bufferData, remap));
        }

        private void AddOrUpdateBuffer<T>(IList<T> bufferData, string bindingName, bool remap, uint instanceDivisor, XRDataBuffer buffer) where T : unmanaged, IBufferable
        {
            if (!_buffers.TryAdd(bindingName, buffer))
                _buffers[bindingName] = buffer;
            UpdateFaceIndices(bufferData.Count, bindingName, remap, instanceDivisor, buffer.SetData(bufferData, remap));
        }

        private void UpdateFaceIndices(int dataCount, string bindingName, bool remap, uint instanceDivisor, Remapper? remapper)
        {
            if (instanceDivisor != 0)
                return;

            Func<uint, uint> getter = remap && remapper is not null && remapper.RemapTable is not null && remapper.ImplementationTable is not null
                ? i => (uint)remapper.ImplementationTable[remapper.RemapTable[i]]
                : i => i;

            for (uint i = 0; i < dataCount; ++i)
            {
                var bindings = _faceIndices[(int)i].BufferBindings;
                if (bindings.ContainsKey(bindingName))
                    bindings[bindingName] = getter(i);
                else
                    bindings.Add(bindingName, getter(i));
            }
        }

        #endregion

        #region Indices
        public int[] GetIndices()
        {
            int[]? indices = _type switch
            {
                EPrimitiveType.Triangles => _triangles?.SelectMany(x => new int[] { x.Point0, x.Point1, x.Point2 }).ToArray(),
                EPrimitiveType.Lines => _lines?.SelectMany(x => new int[] { x.Point0, x.Point1 }).ToArray(),
                EPrimitiveType.Points => _points?.Select(x => (int)x).ToArray(),
                _ => null,
            };
            return indices is null ? throw new InvalidOperationException($"{_type} mesh has no face indices.") : indices;
        }

        public int[]? GetIndices(EPrimitiveType type)
        {
            int[]? indices = type switch
            {
                EPrimitiveType.Triangles => _triangles?.SelectMany(x => new int[] { x.Point0, x.Point1, x.Point2 }).ToArray(),
                EPrimitiveType.Lines => _lines?.SelectMany(x => new int[] { x.Point0, x.Point1 }).ToArray(),
                EPrimitiveType.Points => _points?.Select(x => (int)x).ToArray(),
                _ => null,
            };
            return indices is null ? throw new InvalidOperationException($"{type} mesh has no face indices.") : indices;
        }

        private Remapper? SetTriangleIndices(List<Vertex> vertices, bool remap = true)
        {
            _triangles = [];

            while (vertices.Count % 3 != 0)
                vertices.RemoveAt(vertices.Count - 1);
            
            if (remap)
            {
                Remapper remapper = new();
                remapper.Remap(vertices, null);
                for (int i = 0; i < remapper.RemapTable?.Length;)
                {
                    _triangles.Add(new IndexTriangle(
                        remapper.RemapTable[i++],
                        remapper.RemapTable[i++],
                        remapper.RemapTable[i++]));
                }
                return remapper;
            }
            else
            {
                for (int i = 0; i < vertices.Count;)
                    _triangles.Add(new IndexTriangle(i++, i++, i++));
                return null;
            }
        }
        private Remapper? SetLineIndices(List<Vertex> vertices, bool remap = true)
        {
            if (vertices.Count % 2 != 0)
                vertices.RemoveAt(vertices.Count - 1);

            _lines = [];
            if (remap)
            {
                Remapper remapper = new();
                remapper.Remap(vertices, null);
                for (int i = 0; i < remapper.RemapTable?.Length;)
                {
                    _lines.Add(new IndexLine(
                        remapper.RemapTable[i++],
                        remapper.RemapTable[i++]));
                }
                return remapper;
            }
            else
            {
                for (int i = 0; i < vertices.Count;)
                    _lines.Add(new IndexLine(i++, i++));
                return null;
            }
        }
        private Remapper? SetPointIndices(List<Vertex> vertices, bool remap = true)
        {
            _points = [];
            if (remap)
            {
                Remapper remapper = new();
                remapper.Remap(vertices, null);
                for (int i = 0; i < remapper.RemapTable?.Length;)
                    _points.Add(new IndexPoint(remapper.RemapTable[i++]));
                return remapper;
            }
            else
            {
                for (int i = 0; i < vertices.Count;)
                    _points.Add(new IndexPoint(i++));
                return null;
            }
        }
        #endregion

        protected override void OnDestroying()
            => _buffers?.ForEach(x => x.Value.Dispose());

        public IEnumerator<XRDataBuffer> GetEnumerator() => ((IEnumerable<XRDataBuffer>)_buffers).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<XRDataBuffer>)_buffers).GetEnumerator();
    }
}
