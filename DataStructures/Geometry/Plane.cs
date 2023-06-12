using System.ComponentModel;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Geometry
{
    public struct Plane
    {
        /*
        * Represents a plane a certain distance from the origin.
        * Ax + By + Cz + D = 0
        * D is distance from the origin
        *      ________
        *     / .     /
        *    /   \   /
        *   /_____\_/
        *          \
        *           .origin
        */
        
        public Vec3 Normal;
        public float Distance;

        public Plane(Vec3 normal, float distance)
        {
            Normal = normal;
            Distance = distance;
        }
        public Plane(float a, float b, float c, float d)
        {
            Normal = new Vec3(a, b, c);
            Distance = d;
        }

        /// <summary>
        /// Constructs a plane given three points.
        /// Points must be specified in this order 
        /// to ensure the normal points in the right direction.
        ///   ^
        ///   |   c
        /// n |  /
        ///   | / u
        ///   |/_______ b
        ///  a    v
        /// </summary>
        public Plane(Vec3 a, Vec3 b, Vec3 c)
        {
            Normal = ~((b - a) ^ (c - a));
            Distance = Normal | a;
        }
        /// <summary>
        /// Constructs a plane given a point.
        /// The normal points in the direction of the origin.
        /// </summary>
        public Plane(Vec3 point)
        {
            Normal = (-point).Normalized();
            Distance = point.Length;
        }
        /// <summary>
        /// Constructs a plane given a normal and distance from the origin.
        /// </summary>
        public Plane(float distance, Vec3 normal)
        {
            Normal = normal.Normalized();
            Distance = distance;
        }
        /// <summary>
        /// Constructs a plane given a point and normal.
        /// </summary>
        public Plane(Vec3 point, Vec3 normal)
        {
            Normal = normal.Normalized();
            //Ax + By + Cz + D = 0
            //Ax + By + Cz = -D
            //-(Ax + By + Cz) = D
            //normal = (A, B, C)
            //point = (x, y, z)
            //Distance is negative dot product between normal and point
            Distance = -point.Dot(Normal);
        }

        /// <summary>
        /// Returns distance from the plane defined by a point and normal to the origin.
        /// </summary>
        /// <param name="planePoint">Point in space the plane intersects.</param>
        /// <param name="planeNormal">The normal of the plane.</param>
        /// <returns>Shortest distance to the origin from the plane.</returns>
        public static float ComputeDistance(Vec3 planePoint, Vec3 planeNormal)
            => -planePoint.Dot(planeNormal);

        /// <summary>
        /// The intersection point of a line, which is perpendicular to the plane and passes through the origin, and the plane.
        /// Note that while you can set this point to anything, the original world position will be lost and the distance value will be updated
        /// so that the plane is coplanar with the point, using same normal.
        /// </summary>
        [Category("Plane")]
        [Description("The intersection point of a line, which is perpendicular to the plane " +
            "and passes through the origin, and the plane. " +
            "Note that while you can set this point to anything, the original world position will be lost +" +
            "and the distance value will be updated so that the plane is coplanar with the point, using same normal.")]
        public Vec3 IntersectionPoint
        {
            //Ax + By + Cz + D = 0
            //Ax + By + Cz = -D
            //(x, y, z) = -D * (A, B, C)
            get => Normal * -Distance;
            set => Distance = -value.Dot(Normal);
        }

        public Plane Flipped()
            => new(-Normal, -Distance);

        public void Normalize()
        {
            float mag = 1.0f / Normal.Length;
            Normal *= mag;
            Distance *= mag;
        }
        public void FlipNormal() => Normal = -Normal;
        public EPlaneIntersection IntersectsBox(BoundingBox box)
        {
            Vec3 min;
            Vec3 max;

            max.X = (Normal.X >= 0.0f) ? box.Minimum.X : box.Maximum.X;
            max.Y = (Normal.Y >= 0.0f) ? box.Minimum.Y : box.Maximum.Y;
            max.Z = (Normal.Z >= 0.0f) ? box.Minimum.Z : box.Maximum.Z;
            min.X = (Normal.X >= 0.0f) ? box.Maximum.X : box.Minimum.X;
            min.Y = (Normal.Y >= 0.0f) ? box.Maximum.Y : box.Minimum.Y;
            min.Z = (Normal.Z >= 0.0f) ? box.Maximum.Z : box.Minimum.Z;

            if (Normal.Dot(max) + Distance > 0.0f)
                return EPlaneIntersection.Front;

            if (Normal.Dot(min) + Distance < 0.0f)
                return EPlaneIntersection.Back;

            return EPlaneIntersection.Intersecting;
        }
        public EPlaneIntersection IntersectsSphere(float radius, Vec3 center)
        {
            float dot = center.Dot(Normal) + Distance;

            if (dot > radius)
                return EPlaneIntersection.Front;

            if (dot < -radius)
                return EPlaneIntersection.Back;

            return EPlaneIntersection.Intersecting;
        }

        public Vec3 ClosestPoint(Vec3 point)
            => Collision.ClosestPointPlanePoint(this, point);
        public float DistanceTo(Vec3 point)
            => Collision.DistancePlanePoint(this, point);

        //public Plane TransformedBy(Matrix transform)
        //    => new Plane(Vec3.TransformPosition(IntersectionPoint, transform), Normal * transform.GetRotationMatrix4());

        //public void TransformBy(Matrix transform)
        //{
        //    Normal *= transform.GetRotationMatrix();
        //    IntersectionPoint *= transform;
        //}

        //public TMesh GetWireframeMesh(float xExtent, float yExtent)
        //    => WireframeMesh(IntersectionPoint, Normal, xExtent, yExtent);
        //public TMesh GetSolidMesh(float xExtent, float yExtent)
        //    => SolidMesh(IntersectionPoint, Normal, xExtent, yExtent);
        //public static TMesh WireframeMesh(Vec3 position, Vec3 normal, float xExtent, float yExtent)
        //{
        //    Quat r = normal.LookatAngles().ToQuaternion();
        //    Vec3 bottomLeft = position + new Vec3(-0.5f * xExtent, -0.5f * yExtent, 0.0f) * r;
        //    Vec3 bottomRight = position + new Vec3(0.5f * xExtent, -0.5f * yExtent, 0.0f) * r;
        //    Vec3 topLeft = position + new Vec3(-0.5f * xExtent, 0.5f * yExtent, 0.0f) * r;
        //    Vec3 topRight = position + new Vec3(0.5f * xExtent, 0.5f * yExtent, 0.0f) * r;
        //    return TMesh.Create(VertexShaderDesc.JustPositions(), new VertexLineStrip(true, bottomLeft, bottomRight, topRight, topLeft));
        //}
        //public static TMesh SolidMesh(Vec3 position, Vec3 normal, float xExtent, float yExtent)
        //{
        //    Quat r = normal.LookatAngles().ToQuaternion();
        //    Vec3 bottomLeft = position + new Vec3(-0.5f * xExtent, -0.5f * yExtent, 0.0f) * r;
        //    Vec3 bottomRight = position + new Vec3(0.5f * xExtent, -0.5f * yExtent, 0.0f) * r;
        //    Vec3 topLeft = position + new Vec3(-0.5f * xExtent, 0.5f * yExtent, 0.0f) * r;
        //    Vec3 topRight = position + new Vec3(0.5f * xExtent, 0.5f * yExtent, 0.0f) * r;
        //    TVertexQuad q = TVertexQuad.Make(bottomLeft, bottomRight, topRight, topLeft, normal);
        //    return TMesh.Create(VertexShaderDesc.PosNormTex(), q);
        //}

        public void SplitTriangle(Triangle triangle, List<Triangle>? coplanar, List<Triangle> front, List<Triangle> back)
        {
            const float epsilon = 0.00001f;

            float da = Vec3.Dot(Normal, triangle.A) - Distance;
            float db = Vec3.Dot(Normal, triangle.B) - Distance;
            float dc = Vec3.Dot(Normal, triangle.C) - Distance;

            if (da >= -epsilon && db >= -epsilon && dc >= -epsilon)
            {
                front.Add(triangle);
                return;
            }

            if (da <= epsilon && db <= epsilon && dc <= epsilon)
            {
                back.Add(triangle);
                return;
            }

            if (coplanar != null && Math.Abs(da) < epsilon && Math.Abs(db) < epsilon && Math.Abs(dc) < epsilon)
            {
                coplanar.Add(triangle);
                return;
            }

            Vec3[] vertices = new Vec3[] { triangle.A, triangle.B, triangle.C };
            int[] indices = new int[3];

            for (int i = 0; i < 3; i++)
            {
                int j = (i + 1) % 3;
                int k = (i + 2) % 3;
                float di = Vec3.Dot(Normal, vertices[i]) - Distance;
                float dj = Vec3.Dot(Normal, vertices[j]) - Distance;
                float dk = Vec3.Dot(Normal, vertices[k]) - Distance;

                if (di < -epsilon)
                {
                    indices[i] = back.Count;
                    back.Add(new Triangle(vertices[i], vertices[j], vertices[k]));
                }
                else
                {
                    indices[i] = front.Count;
                    front.Add(new Triangle(vertices[i], vertices[j], vertices[k]));
                }

                if (di < -epsilon && dj > epsilon || di > epsilon && dj < -epsilon)
                {
                    float s = di / (di - dj);
                    Vec3 v = vertices[i] + s * (vertices[j] - vertices[i]);

                    var ind = indices[i];
                    var f = front[ind];
                    var b = back[ind];
                    f.C = v;
                    b.B = v;
                    front[ind] = f;
                    back[ind] = b;
                }

                if (di < -epsilon && dk > epsilon || di > epsilon && dk < -epsilon)
                {
                    float s = di / (di - dk);
                    Vec3 v = vertices[i] + s * (vertices[k] - vertices[i]);

                    var ind = indices[i];
                    var f = front[ind];
                    var b = back[ind];
                    f.C = v;
                    b.A = v;
                    front[ind] = f;
                    back[ind] = b;
                }
            }
        }
    }
}