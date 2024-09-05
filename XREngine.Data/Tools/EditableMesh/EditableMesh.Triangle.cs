namespace XREngine.Data.Tools
{
    //public partial class EditableMesh
    //{
    //    /// <summary>
    //    /// This class represents a triangle face of a mesh.
    //    /// </summary>
    //    public class EditableTriangle
    //    {
    //        /// <summary>
    //        /// This is the first vertex of the triangle, in clockwise order.
    //        /// The first half-edge of the triangle points from this vertex to the second vertex.
    //        /// </summary>
    //        public EditableVertex v1;
    //        /// <summary>
    //        /// This is the second vertex of the triangle, in clockwise order.
    //        /// The second half-edge of the triangle points from this vertex to the third vertex.
    //        /// </summary>
    //        public EditableVertex v2;
    //        /// <summary>
    //        /// This is the third vertex of the triangle, in clockwise order.
    //        /// The third half-edge of the triangle points from this vertex to the first vertex.
    //        /// </summary>
    //        public EditableVertex v3;

    //        /// <summary>
    //        /// This is the first half-edge of the triangle, pointing in clockwise order.
    //        /// </summary>
    //        public EditableHalfEdge e1;
    //        /// <summary>
    //        /// This is the second half-edge of the triangle, pointing in clockwise order.
    //        /// </summary>
    //        public EditableHalfEdge e2;
    //        /// <summary>
    //        /// This is the third half-edge of the triangle, pointing in clockwise order.
    //        /// </summary>
    //        public EditableHalfEdge e3;

    //        public Vector3 normal;

    //        //TODO: Add support for UV coordinate management

    //        public bool Contains(EditableVertex vertex) =>
    //            vertex == v1 ||
    //            vertex == v2 ||
    //            vertex == v3;

    //        public bool Contains(EditableHalfEdge halfEdge) =>
    //            halfEdge == e1 ||
    //            halfEdge == e2 ||
    //            halfEdge == e3;

    //        public EditableTriangle(EditableVertex v1, EditableVertex v2, EditableVertex v3)
    //        {
    //            this.v1 = v1;
    //            this.v2 = v2;
    //            this.v3 = v3;

    //            // Create half-edges
    //            e1 = new EditableHalfEdge(v1, v2);
    //            e2 = new EditableHalfEdge(v2, v3);
    //            e3 = new EditableHalfEdge(v3, v1);

    //            // Set face reference for half-edges
    //            e1._faceCW = this;
    //            e2._faceCW = this;
    //            e3._faceCW = this;

    //            // Link half-edges together
    //            e1._next = e2;
    //            e2._next = e3;
    //            e3._next = e1;

    //            // Add half-edges to vertex outgoing edges
    //            this.v1.OutgoingEdges.Add(e1);
    //            this.v2.OutgoingEdges.Add(e2);
    //            this.v3.OutgoingEdges.Add(e3);

    //            // Calculate triangle normal
    //            Vector3 edge1 = v2._position - v1._position;
    //            Vector3 edge2 = v3._position - v1._position;
    //            normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
    //        }

    //        public Matrix4x4 GetQuadricMatrix()
    //        {
    //            Plane plane = Plane.CreateFromVertices(v1._position, v2._position, v3._position); //new(normal.X, normal.Y, normal.Z, -Vector3.Dot(normal, v1._position));
    //            Matrix4x4 Kp = new();
    //            Vector4 v = new(plane.Normal.X, plane.Normal.Y, plane.Normal.Z, plane.D);
    //            for (int i = 0; i < 4; i++)
    //                for (int j = 0; j < 4; j++)
    //                    Kp[i, j] = v[i] * v[j];
    //            return Kp;
    //        }
    //    }
    //}
}
