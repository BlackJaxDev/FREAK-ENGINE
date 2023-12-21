using System.Numerics;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;
using XREngine.Files;

namespace XREngine.Data.Tools
{
    public class MeshSimplification
    {
        public class Vertex
        {
            public Vec3 _position;
            public List<HalfEdge> _outgoingEdges;
            public Matrix _quadric;

            public Vertex(Vec3 pos)
            {
                _position = pos;
                _outgoingEdges = new List<HalfEdge>();
                _quadric = Matrix.Zero;
            }

            public void ComputeQuadric(List<Triangle> triangles)
            {
                _quadric = Matrix.Zero;
                foreach (Triangle triangle in triangles)
                    if (triangle.Contains(this))
                        _quadric += triangle.GetQuadricMatrix();
            }
        }

        public class HalfEdge
        {
            public Vertex _origin;
            public Vertex _target;
            public Triangle? _face;
            public HalfEdge? _opposite;
            public HalfEdge? _next;

            public HalfEdge(Vertex origin, Vertex target)
            {
                _origin = origin;
                _target = target;
                _face = null;
                _opposite = null;
                _next = null;
            }
        }

        public class Triangle
        {
            public Vertex v1;
            public Vertex v2;
            public Vertex v3;
            public HalfEdge e1;
            public HalfEdge e2;
            public HalfEdge e3;
            public Vec3 normal;

            public Triangle(Vertex v1, Vertex v2, Vertex v3)
            {
                this.v1 = v1;
                this.v2 = v2;
                this.v3 = v3;

                // Create half-edges
                e1 = new HalfEdge(v1, v2);
                e2 = new HalfEdge(v2, v3);
                e3 = new HalfEdge(v3, v1);

                // Set face reference for half-edges
                e1._face = this;
                e2._face = this;
                e3._face = this;

                // Link half-edges together
                e1._next = e2;
                e2._next = e3;
                e3._next = e1;

                // Add half-edges to vertex outgoing edges
                this.v1._outgoingEdges.Add(e1);
                this.v2._outgoingEdges.Add(e2);
                this.v3._outgoingEdges.Add(e3);

                // Calculate triangle normal
                Vec3 edge1 = v2._position - v1._position;
                Vec3 edge2 = v3._position - v1._position;
                normal = Vec3.Cross(edge1, edge2).Normalized();
            }

            public Matrix GetQuadricMatrix()
            {
                Plane plane = new(normal.x, normal.y, normal.z, -Vec3.Dot(normal, v1._position));
                Matrix Kp = new();
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Kp[i, j] = plane[i] * plane[j];
                    }
                }
                return Kp;
            }
        }
        private List<Vertex> vertices;
        private List<HalfEdge> halfEdges;
        private List<Triangle> triangles;
        private SimplePriorityQueue<HalfEdge, float> edgeQueue;

        public MeshSimplification(Mesh mesh)
        {
            // Create vertices, halfEdges, and triangles from the input mesh
            // ...

            // Initialize the edge priority queue
            edgeQueue = new SimplePriorityQueue<HalfEdge, float>();

            // Compute quadric matrices for each vertex and enqueue half-edges
            foreach (Vertex vertex in vertices)
            {
                vertex.ComputeQuadric(triangles);
            }
            foreach (HalfEdge halfEdge in halfEdges)
            {
                EnqueueHalfEdge(halfEdge);
            }
        }

        private void EnqueueHalfEdge(HalfEdge halfEdge)
        {
            Vec3 newPos = (halfEdge._origin._position + halfEdge._target._position) * 0.5f;
            Matrix Q = halfEdge._origin._quadric + halfEdge._target._quadric;
            float error = ComputeQuadricError(Q, newPos);
            edgeQueue.Enqueue(halfEdge, error);
        }

        private float ComputeQuadricError(Matrix Q, Vec3 newPos)
        {
            Vec4 newPosHomo = new(newPos.X, newPos.Y, newPos.Z, 1);
            float error = Vec4.Dot(newPosHomo, Q * newPosHomo);
            return error;
        }

        public void CollapseEdge(HalfEdge halfEdge)
        {
            // Remove the edge from the priority queue
            edgeQueue.Remove(halfEdge);

            // Calculate the new position for the collapsed edge
            Vec3 newPos = (halfEdge._origin._position + halfEdge._target._position) * 0.5f;

            // Update the position of the origin vertex
            halfEdge._origin._position = newPos;

            // Remove the target vertex from the vertex list
            vertices.Remove(halfEdge._target);

            // Update the connectivity information
            halfEdge._opposite._origin = halfEdge._origin;
            halfEdge._prev._next = halfEdge._next;
            halfEdge._next._prev = halfEdge._prev;
            halfEdge._opposite._prev.next = halfEdge._opposite._next;
            halfEdge._opposite._next.prev = halfEdge._opposite._prev;

            // Remove the affected triangles
            triangles.Remove(halfEdge.face);
            triangles.Remove(halfEdge.opposite.face);

            // Update the outgoing half-edges of the origin vertex
            halfEdge._origin._outgoingEdges.Remove(halfEdge);
            halfEdge._origin._outgoingEdges.Remove(halfEdge._opposite);
            halfEdge._origin._outgoingEdges.Add(halfEdge._opposite._next);

            // Update quadric matrices for the affected vertices and update the edgeQueue
            foreach (Vertex vertex in new List<Vertex> { halfEdge._origin, halfEdge._prev.origin, halfEdge._next._target })
            {
                vertex.ComputeQuadric(triangles);
                foreach (HalfEdge edge in vertex._outgoingEdges)
                {
                    if (edgeQueue.Contains(edge))
                    {
                        edgeQueue.UpdatePriority(edge, ComputeQuadricError(edge._origin._quadric + edge._target._quadric, (edge._origin._position + edge._target._position) * 0.5f));
                    }
                }
            }
        }
        public static Mesh Simplify(Mesh mesh, float threshold)
        {
            Vec3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Create a list of Triangle objects
            List<Triangle> triangleList = new();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                triangleList.Add(new Triangle(triangles[i], triangles[i + 1], triangles[i + 2], vertices));
            }

            // Perform edge collapses
            while (true)
            {
                // Find best edge to collapse
                float smallestError = float.MaxValue;
                int bestV1 = -1, bestV2 = -1;
                for (int i = 0; i < vertices.Length; i++)
                {
                    for (int j = i + 1; j < vertices.Length; j++)
                    {
                        float error = QuadricError(vertices[i], vertices[j], triangleList);
                        if (error < smallestError && error < threshold)
                        {
                            smallestError = error;
                            bestV1 = i;
                            bestV2 = j;
                        }
                    }
                }

                // No more edges to collapse
                if (bestV1 == -1 || bestV2 == -1)
                    break;

                // Collapse edge
                Vec3 newPos = (vertices[bestV1] + vertices[bestV2]) * 0.5f;
                vertices[bestV1] = newPos;
                vertices[bestV2] = newPos;

                // Remove degenerate triangles
                triangleList.RemoveAll(triangle =>
                    (triangle.v1 == bestV1 || triangle.v2 == bestV1 || triangle.v3 == bestV1) &&
                    (triangle.v1 == bestV2 || triangle.v2 == bestV2 || triangle.v3 == bestV2));
            }

            // Rebuild mesh
            Mesh simplifiedMesh = new Mesh();
            List<int> newTriangles = new();

            foreach (Triangle triangle in triangleList)
            {
                newTriangles.Add(triangle.v1);
                newTriangles.Add(triangle.v2);
                newTriangles.Add(triangle.v3);
            }

            simplifiedMesh.vertices = vertices;
            simplifiedMesh.triangles = newTriangles.ToArray();
            simplifiedMesh.RecalculateNormals();

            return simplifiedMesh;
        }

        private static float QuadricError(Vec3 v1, Vec3 v2, List<Triangle> triangleList)
        {
            float error = 0;
            Vec3 newPos = (v1 + v2) * 0.5f;// Calculate the quadric error for the new position
            foreach (Triangle triangle in triangleList)
            {
                if (triangle.v1 == v1 || triangle.v2 == v1 || triangle.v3 == v1 ||
                    triangle.v1 == v2 || triangle.v2 == v2 || triangle.v3 == v2)
                {
                    // Calculate the distance from the new position to the triangle plane
                    float distance = Vec3.Dot(newPos - v1, triangle.normal);
                    error += distance * distance;
                }
            }

            return error;
        }
    }
    public class SimplePriorityQueue<T, TKey> where TKey : IComparable<TKey>
    {
        private SortedDictionary<TKey, List<T>> sortedDictionary;

        public SimplePriorityQueue()
        {
            sortedDictionary = new SortedDictionary<TKey, List<T>>();
        }

        public void Enqueue(T item, TKey priority)
        {
            if (!sortedDictionary.ContainsKey(priority))
            {
                sortedDictionary[priority] = new List<T>();
            }
            sortedDictionary[priority].Add(item);
        }

        public T Dequeue()
        {
            if (sortedDictionary.Count == 0)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            TKey minKey = default;
            foreach (var key in sortedDictionary.Keys)
            {
                minKey = key;
                break;
            }

            List<T> itemList = sortedDictionary[minKey];
            T item = itemList[0];
            itemList.RemoveAt(0);

            if (itemList.Count == 0)
            {
                sortedDictionary.Remove(minKey);
            }

            return item;
        }

        public int Count()
        {
            int count = 0;
            foreach (var itemList in sortedDictionary.Values)
            {
                count += itemList.Count;
            }
            return count;
        }
        public bool Contains(T item)
        {
            foreach (var itemList in sortedDictionary.Values)
            {
                if (itemList.Contains(item))
                {
                    return true;
                }
            }
            return false;
        }

        public void Remove(T item)
        {
            TKey? keyToRemove = default;
            bool found = false;
            foreach (var keyValuePair in sortedDictionary)
            {
                if (keyValuePair.Value.Remove(item))
                {
                    keyToRemove = keyValuePair.Key;
                    found = true;
                    break;
                }
            }

            if (keyToRemove != null && found && sortedDictionary[keyToRemove].Count == 0)
            {
                sortedDictionary.Remove(keyToRemove);
            }
        }

        public void UpdatePriority(T item, TKey newPriority)
        {
            Remove(item);
            Enqueue(item, newPriority);
        }
    }
    public class SimplePriorityQueue<T>
    {
        private List<KeyValuePair<T, float>> heap;

        public SimplePriorityQueue()
        {
            heap = new List<KeyValuePair<T, float>>();
        }

        public void Enqueue(T item, float priority)
        {
            heap.Add(new KeyValuePair<T, float>(item, priority));
            int index = heap.Count - 1;
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;

                if (heap[parentIndex].Value <= heap[index].Value)
                    break;

                (heap[parentIndex], heap[index]) = (heap[index], heap[parentIndex]);
                index = parentIndex;
            }
        }

        public T Dequeue()
        {
            if (heap.Count == 0)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            T result = heap[0].Key;
            int lastIndex = heap.Count - 1;
            heap[0] = heap[lastIndex];
            heap.RemoveAt(lastIndex);

            int index = 0;
            while (true)
            {
                int leftChildIndex = 2 * index + 1;
                int rightChildIndex = 2 * index + 2;
                int minChildIndex;

                if (leftChildIndex >= heap.Count)
                    break;

                if (rightChildIndex >= heap.Count)
                    minChildIndex = leftChildIndex;
                else
                    minChildIndex = heap[leftChildIndex].Value < heap[rightChildIndex].Value
                        ? leftChildIndex
                        : rightChildIndex;

                if (heap[index].Value <= heap[minChildIndex].Value)
                    break;

                (heap[minChildIndex], heap[index]) = (heap[index], heap[minChildIndex]);
                index = minChildIndex;
            }

            return result;
        }

        private void UpHeap(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (heap[parentIndex].Value <= heap[index].Value) break;

                (heap[parentIndex], heap[index]) = (heap[index], heap[parentIndex]);
                index = parentIndex;
            }
        }

        private void DownHeap(int index)
        {
            while (true)
            {
                int leftChildIndex = 2 * index + 1;
                int rightChildIndex = 2 * index + 2;
                int minChildIndex;

                if (leftChildIndex >= heap.Count) break;
                if (rightChildIndex >= heap.Count) minChildIndex = leftChildIndex;
                else minChildIndex = heap[leftChildIndex].Value < heap[rightChildIndex].Value ? leftChildIndex : rightChildIndex;

                if (heap[index].Value <= heap[minChildIndex].Value) break;

                KeyValuePair<T, float> temp = heap[index];
                heap[index] = heap[minChildIndex];
                heap[minChildIndex] = temp;
                index = minChildIndex;
            }
        }

        public void Remove(T item)
        {
            int index = heap.FindIndex(pair => pair.Key.Equals(item));
            if (index == -1) return;

            int lastIndex = heap.Count - 1;
            heap[index] = heap[lastIndex];
            heap.RemoveAt(lastIndex);

            if (index < lastIndex)
            {
                UpHeap(index);
                DownHeap(index);
            }
        }

        public void UpdatePriority(T item, float newPriority)
        {
            int index = heap.FindIndex(pair => pair.Key.Equals(item));
            if (index == -1) return;

            float oldPriority = heap[index].Value;
            heap[index] = new KeyValuePair<T, float>(item, newPriority);

            if (newPriority < oldPriority)
            {
                UpHeap(index);
            }
            else
            {
                DownHeap(index);
            }
        }

        public int Count()
        {
            return heap.Count;
        }
    }
}
