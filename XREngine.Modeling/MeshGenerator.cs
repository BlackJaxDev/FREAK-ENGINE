using System.Numerics;

namespace XREngine.Modeling
{
    public partial class MeshGenerator
    {
        public List<Vector3> Vertices { get; private set; }
        public List<int> Indices { get; private set; }
        public List<Vector3> Normals { get; private set; }

        private MeshGenerator()
        {
            Vertices = [];
            Indices = [];
            Normals = [];
        }
        private int GetMiddlePoint(int p1, int p2, float radius, IDictionary<long, int> cache)
        {
            long key = ((long)Math.Min(p1, p2) << 32) + Math.Max(p1, p2);
            if (cache.TryGetValue(key, out int value))
                return value;
            
            Vector3 point1 = Vertices[p1];
            Vector3 point2 = Vertices[p2];
            Vector3 middle = Vector3.Normalize((point1 + point2) * 0.5f) * radius;

            int newIndex = AddVertex(middle);
            cache[key] = newIndex;
            return newIndex;
        }
        public static MeshGenerator GenerateIcosahedron(float radius, int detailLevel)
        {
            MeshGenerator generator = new();
            generator.GenerateIcosahedron_Internal(radius, detailLevel);
            return generator;
        }
        private void GenerateIcosahedron_Internal(float radius, int detailLevel)
        {
            float t = (1.0f + (float)Math.Sqrt(5.0)) / 2.0f;

            AddVertex(new Vector3(-1, t, 0) * radius);
            AddVertex(new Vector3(1, t, 0) * radius);
            AddVertex(new Vector3(-1, -t, 0) * radius);
            AddVertex(new Vector3(1, -t, 0) * radius);

            AddVertex(new Vector3(0, -1, t) * radius);
            AddVertex(new Vector3(0, 1, t) * radius);
            AddVertex(new Vector3(0, -1, -t) * radius);
            AddVertex(new Vector3(0, 1, -t) * radius);

            AddVertex(new Vector3(t, 0, -1) * radius);
            AddVertex(new Vector3(t, 0, 1) * radius);
            AddVertex(new Vector3(-t, 0, -1) * radius);
            AddVertex(new Vector3(-t, 0, 1) * radius);

            int[][] faces =
            [
                [0, 11, 5],
                [0, 5, 1],
                [0, 1, 7],
                [0, 7, 10],
                [0, 10, 11],
                [1, 5, 9],
                [5, 11, 4],
                [11, 10, 2],
                [10, 7, 6],
                [7, 1, 8],
                [3, 9, 4],
                [3, 4, 2],
                [3, 2, 6],
                [3, 6, 8],
                [3, 8, 9],
                [4, 9, 5],
                [2, 4, 11],
                [6, 2, 10],
                [8, 6, 7],
                [9, 8, 1]
            ];

            IDictionary<long, int> middlePointCache = new Dictionary<long, int>();
            List<int[]> refinedFaces = new(faces);

            for (int i = 0; i < detailLevel; i++)
            {
                List<int[]> newFaces = [];
                foreach (int[] face in refinedFaces)
                {
                    int a = GetMiddlePoint(face[0], face[1], radius, middlePointCache);
                    int b = GetMiddlePoint(face[1], face[2], radius, middlePointCache);
                    int c = GetMiddlePoint(face[2], face[0], radius, middlePointCache);

                    newFaces.Add([face[0], a, c]);
                    newFaces.Add([face[1], b, a]);
                    newFaces.Add([face[2], c, b]);
                    newFaces.Add([a, b, c]);
                }

                refinedFaces = newFaces;
            }

            foreach (int[] face in refinedFaces)
                AddTriangle(face[0], face[1], face[2]);
            
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vector3 vertex = Vector3.Normalize(Vertices[i]) * radius;
                Vertices[i] = vertex;
                Normals.Add(Vector3.Normalize(vertex));
            }
        }
        public static MeshGenerator GenerateTwoHemisphere(float radius, int detailLevel)
        {
            MeshGenerator generator = new();
            generator.GenerateTwoHemisphere_Internal(radius, detailLevel);
            return generator;
        }
        private void GenerateTwoHemisphere_Internal(float radius, int detailLevel)
        {
            int stacks = 8 + detailLevel * 4;
            int slices = 16 + detailLevel * 8;

            // Top hemisphere
            for (int stack = 0; stack <= stacks / 2; stack++)
            {
                float phi = (float)(stack * Math.PI / stacks);
                for (int slice = 0; slice <= slices; slice++)
                {
                    float theta = (float)(slice * 2 * Math.PI / slices);

                    float x = (float)(Math.Cos(theta) * Math.Sin(phi));
                    float y = (float)(Math.Cos(phi));
                    float z = (float)(Math.Sin(theta) * Math.Sin(phi));

                    Vector3 vertex = new Vector3(x, y, z) * radius;
                    Vertices.Add(vertex);
                    Normals.Add(Vector3.Normalize(vertex));
                }
            }

            // Bottom hemisphere
            for (int stack = stacks / 2; stack <= stacks; stack++)
            {
                float phi = (float)(stack * Math.PI / stacks);
                for (int slice = 0; slice <= slices; slice++)
                {
                    float theta = (float)(slice * 2 * Math.PI / slices);

                    float x = (float)(Math.Cos(theta) * Math.Sin(phi));
                    float y = (float)(Math.Cos(phi));
                    float z = (float)(Math.Sin(theta) * Math.Sin(phi));

                    Vector3 vertex = new Vector3(x, y, z) * radius;
                    Vertices.Add(vertex);
                    Normals.Add(Vector3.Normalize(vertex));
                }
            }

            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    int a = stack * (slices + 1) + slice;
                    int b = stack * (slices + 1) + slice + 1;
                    int c = (stack + 1) * (slices + 1) + slice;
                    int d = (stack + 1) * (slices + 1) + slice + 1;

                    Indices.Add(a);
                    Indices.Add(c);
                    Indices.Add(b);

                    Indices.Add(b);
                    Indices.Add(c);
                    Indices.Add(d);
                }
            }
        }
        public static MeshGenerator GenerateUVSphere(float radius, int detailLevel)
        {
            MeshGenerator generator = new();
            generator.GenerateUVSphere_Internal(radius, detailLevel);
            return generator;
        }
        private void GenerateUVSphere_Internal(float radius, int detailLevel)
        {
            int stacks = 8 + detailLevel * 4;
            int slices = 16 + detailLevel * 8;
            GenerateUVSphere_Internal(radius, stacks, slices);
        }
        public static MeshGenerator GenerateUVSphere(float radius, int stacks, int slices)
        {
            MeshGenerator generator = new();
            generator.GenerateUVSphere_Internal(radius, stacks, slices);
            return generator;
        }
        private void GenerateUVSphere_Internal(float radius, int stacks, int slices)
        {
            for (int stack = 0; stack <= stacks; stack++)
            {
                float phi = (float)(stack * Math.PI / stacks);
                for (int slice = 0; slice <= slices; slice++)
                {
                    float theta = (float)(slice * 2 * Math.PI / slices);

                    float x = (float)(Math.Cos(theta) * Math.Sin(phi));
                    float y = (float)(Math.Cos(phi));
                    float z = (float)(Math.Sin(theta) * Math.Sin(phi));

                    Vector3 vertex = new Vector3(x, y, z) * radius;
                    Vertices.Add(vertex);
                    Normals.Add(Vector3.Normalize(vertex));
                }
            }

            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    int a = stack * (slices + 1) + slice;
                    int b = stack * (slices + 1) + slice + 1;
                    int c = (stack + 1) * (slices + 1) + slice;
                    int d = (stack + 1) * (slices + 1) + slice + 1;

                    Indices.Add(a);
                    Indices.Add(c);
                    Indices.Add(b);

                    Indices.Add(b);
                    Indices.Add(c);
                    Indices.Add(d);
                }
            }
        }
        public int AddVertex(Vector3 vertex)
        {
            Vertices.Add(Vector3.Normalize(vertex));
            return Vertices.Count - 1;
        }
        public void AddTriangle(int a, int b, int c)
        {
            Indices.Add(a);
            Indices.Add(b);
            Indices.Add(c);
        }
        public void SubdivideMesh()
        {
            Dictionary<int, List<int>> vertexNeighbors = [];
            Dictionary<Edge, int> edgeVertices = [];
            List<int> newTriangles = [];

            for (int i = 0; i < Vertices.Count; i++)
                vertexNeighbors.Add(i, []);
            
            for (int i = 0; i < Indices.Count; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    int current = Indices[i + j];
                    int next = Indices[i + (j + 1) % 3];

                    if (!vertexNeighbors[current].Contains(next))
                        vertexNeighbors[current].Add(next);
                    
                    if (!vertexNeighbors[next].Contains(current))
                        vertexNeighbors[next].Add(current);
                    
                    Edge edge = new(current, next);
                    if (!edgeVertices.ContainsKey(edge))
                    {
                        int newIndex = Indices.Count + edgeVertices.Count;
                        edgeVertices.Add(edge, newIndex);
                    }
                }
            }

            Vector3[] newVertices = new Vector3[Vertices.Count + edgeVertices.Count];
            Array.Copy(Vertices.ToArray(), newVertices, Vertices.Count);

            foreach (var edge in edgeVertices)
                newVertices[edge.Value] = (Vertices[edge.Key.vertex1] + Vertices[edge.Key.vertex2]) * 0.5f;
            
            for (int i = 0; i < Indices.Count; i += 3)
            {
                int[] cornerVertices = [Indices[i], Indices[i + 1], Indices[i + 2]];
                int[] midVertices =
                [
                    edgeVertices[new Edge(cornerVertices[0], cornerVertices[1])],
                    edgeVertices[new Edge(cornerVertices[1], cornerVertices[2])],
                    edgeVertices[new Edge(cornerVertices[2], cornerVertices[0])]
                ];

                newTriangles.Add(cornerVertices[0]);
                newTriangles.Add(midVertices[0]);
                newTriangles.Add(midVertices[2]);

                newTriangles.Add(midVertices[0]);
                newTriangles.Add(cornerVertices[1]);
                newTriangles.Add(midVertices[1]);

                newTriangles.Add(midVertices[2]);
                newTriangles.Add(midVertices[1]);
                newTriangles.Add(cornerVertices[2]);

                newTriangles.Add(midVertices[0]);
                newTriangles.Add(midVertices[1]);
                newTriangles.Add(midVertices[2]);
            }

            Vertices = [.. newVertices];
            Indices = newTriangles;

            RecalculateNormals();
        }

        public void RecalculateNormals()
        {
            Vector3[] normals = new Vector3[Vertices.Count];
            for (int i = 0; i < Indices.Count; i += 3)
            {
                Vector3 v0 = Vertices[Indices[i]];
                Vector3 v1 = Vertices[Indices[i + 1]];
                Vector3 v2 = Vertices[Indices[i + 2]];
                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                normals[Indices[i]] += normal;
                normals[Indices[i + 1]] += normal;
                normals[Indices[i + 2]] += normal;
            }

            for (int i = 0; i < normals.Length; i++)
                Vector3.Normalize(normals[i]);
            
            Normals = [.. normals];
        }
    }
}
