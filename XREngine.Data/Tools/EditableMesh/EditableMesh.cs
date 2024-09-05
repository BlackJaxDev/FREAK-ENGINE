namespace XREngine.Data.Tools
{
    //public partial class EditableMesh
    //{
    //    private XRMesh? _display = null;
    //    private readonly List<EditableVertex> _vertices;
    //    private readonly List<EditableHalfEdge> _halfEdges;
    //    private readonly List<EditableTriangle> _triangles;
    //    private readonly SimplePriorityQueue<EditableHalfEdge, float> _edgeQueue;

    //    public EditableMesh(XRMesh mesh)
    //    {
    //        _vertices = [];
    //        _halfEdges = [];
    //        _triangles = [];
    //        _edgeQueue = new();

    //        // Create vertices, halfEdges, and triangles from the input mesh
    //        _vertices.AddRange(mesh.Vertices.Select(x => new EditableVertex(x)));

    //        // Initialize the edge priority queue
    //        _edgeQueue = new SimplePriorityQueue<EditableHalfEdge, float>();

    //        // Compute quadric matrices for each vertex and enqueue half-edges
    //        foreach (EditableVertex vertex in _vertices)
    //            vertex.ComputeQuadric(_triangles);

    //        foreach (EditableHalfEdge halfEdge in _halfEdges)
    //            EnqueueHalfEdge(halfEdge);
    //    }

    //    private void EnqueueHalfEdge(EditableHalfEdge halfEdge)
    //        => _edgeQueue.Enqueue(
    //            halfEdge,
    //            ComputeQuadricError(
    //                halfEdge._origin.Quadric + halfEdge._target.Quadric,
    //                (halfEdge._origin.Position + halfEdge._target.Position) * 0.5f));

    //    private static float ComputeQuadricError(Matrix4x4 Q, Vector3 newPos)
    //    {
    //        Vector4 newPosHomo = new(newPos.X, newPos.Y, newPos.Z, 1);
    //        return Vector4.Dot(newPosHomo, Vector4.Transform(newPosHomo, Q));
    //    }

    //    public void CollapseEdge(EditableHalfEdge halfEdge)
    //    {
    //        var opp = halfEdge._opposite;
    //        var origin = halfEdge._origin;
    //        var next = halfEdge._next;
    //        var prev = halfEdge._prev;
    //        var target = halfEdge._target;
    //        var outgoing = origin.OutgoingEdges;
    //        var faceCW = halfEdge._faceCW;
    //        var faceCCW = halfEdge._faceCCW;

    //        // Remove the edge from the priority queue
    //        _edgeQueue.Remove(halfEdge);

    //        // Calculate the new position for the collapsed edge
    //        // Update the position of the origin vertex
    //        origin.Position = (origin.Position + target.Position) * 0.5f;

    //        // Remove the target vertex from the vertex list
    //        _vertices.Remove(target);

    //        // Update the connectivity information
    //        // Remove the affected triangles
    //        // Update the outgoing half-edges of the origin vertex

    //        if (prev != null)
    //            prev._next = next;

    //        if (next != null)
    //            next._prev = prev;

    //        if (opp != null)
    //        {
    //            opp._origin = origin;

    //            if (opp._prev != null)
    //                opp._prev._next = opp._next;

    //            if (opp._next != null)
    //                opp._next._prev = opp._prev;

    //            if (opp._faceCW != null)
    //                _triangles.Remove(opp._faceCW);

    //            outgoing.Remove(opp);

    //            if (opp._next != null)
    //                outgoing.Add(opp._next);
    //        }

    //        if (faceCW != null)
    //            _triangles.Remove(faceCW);

    //        outgoing.Remove(halfEdge);

    //        // Update quadric matrices for the affected vertices and update the edgeQueue
    //        var list = new List<EditableVertex> { origin };
    //        if (prev != null)
    //            list.Add(prev._origin);
    //        if (next != null)
    //            list.Add(next._target);

    //        foreach (EditableVertex vertex in list)
    //        {
    //            vertex.ComputeQuadric(_triangles);
    //            foreach (EditableHalfEdge edge in vertex.OutgoingEdges)
    //                if (_edgeQueue.Contains(edge))
    //                    _edgeQueue.UpdatePriority(
    //                        edge,
    //                        ComputeQuadricError(
    //                            edge._origin.Quadric + edge._target.Quadric,
    //                            (edge._origin.Position + edge._target.Position) * 0.5f));
    //        }
    //    }
    //    public static XRMesh Simplify(XRMesh mesh, float threshold)
    //    {
    //        var vertices = mesh.GetBufferNumeric(0, out Vector3[]? array);
    //        uint[] triangles = mesh.FaceIndices;
    //        Vector3[] newPositions = new Vector3[vertices.Length];

    //        // Create a list of Triangle objects
    //        List<EditableTriangle> triangleList = [];
    //        for (int i = 0; i < triangles.Length; i += 3)
    //        {
    //            triangleList.Add(new EditableTriangle(
    //                vertices[triangles[i]],
    //                vertices[triangles[i + 1]],
    //                vertices[triangles[i + 2]]));
    //        }

    //        // Perform edge collapses
    //        while (true)
    //        {
    //            // Find best edge to collapse
    //            float smallestError = float.MaxValue;
    //            int bestV1 = -1, bestV2 = -1;
    //            for (int i = 0; i < vertices.Length; i++)
    //            {
    //                for (int j = i + 1; j < vertices.Length; j++)
    //                {
    //                    float error = QuadricError(vertices[i], vertices[j], triangleList);
    //                    if (error < smallestError && error < threshold)
    //                    {
    //                        smallestError = error;
    //                        bestV1 = i;
    //                        bestV2 = j;
    //                    }
    //                }
    //            }

    //            // No more edges to collapse
    //            if (bestV1 == -1 || bestV2 == -1)
    //                break;

    //            // Collapse edge
    //            Vector3 newPos = (vertices[bestV1].Position + vertices[bestV2].Position) * 0.5f;
    //            vertices[bestV1].Position = newPos;
    //            vertices[bestV2].Position = newPos;

    //            // Remove degenerate triangles
    //            triangleList.RemoveAll(triangle =>
    //                (triangle.v1 == bestV1 || triangle.v2 == bestV1 || triangle.v3 == bestV1) &&
    //                (triangle.v1 == bestV2 || triangle.v2 == bestV2 || triangle.v3 == bestV2));
    //        }

    //        // Rebuild mesh
    //        uint[] newTriangles = new uint[triangleList.Count * 3];
    //        for (int i = 0; i < triangleList.Count; i++)
    //        {
    //            EditableTriangle triangle = triangleList[i];
    //            newTriangles[i * 3] = (uint)triangle.v1;
    //            newTriangles[i * 3 + 1] = (uint)triangle.v2;
    //            newTriangles[i * 3 + 2] = (uint)triangle.v3;
    //        }

    //        XRMesh newMesh = new(vertices, newTriangles);
    //        newMesh.RecalculateNormals();

    //        return newMesh;
    //    }

    //    private static float QuadricError(EditableVertex v1, EditableVertex v2, List<EditableTriangle> triangleList)
    //    {
    //        float error = 0;
    //        Vector3 newPos = (v1.Position + v2.Position) * 0.5f;

    //        // Calculate the quadric error for the new position
    //        foreach (EditableTriangle triangle in triangleList)
    //        {
    //            if (triangle.v1 == v1 || triangle.v2 == v1 || triangle.v3 == v1 ||
    //                triangle.v1 == v2 || triangle.v2 == v2 || triangle.v3 == v2)
    //            {
    //                // Calculate the distance from the new position to the triangle plane
    //                float distance = Vector3.Dot(newPos - v1.Position, triangle.normal);
    //                error += distance * distance;
    //            }
    //        }

    //        return error;
    //    }
    //}
}
