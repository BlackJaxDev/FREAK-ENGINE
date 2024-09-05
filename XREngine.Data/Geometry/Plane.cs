using System.ComponentModel;
using System.Numerics;
using XREngine.Data.Core;

namespace XREngine.Data.Geometry
{
    public static class PlaneHelper
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
        public static System.Numerics.Plane FromTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            return new System.Numerics.Plane()
            {
                Normal = n,
                D = Vector3.Dot(n, a),
            };
        }

        /// <summary>
        /// Constructs a plane given a point.
        /// The normal points in the direction of the origin.
        /// </summary>
        public static System.Numerics.Plane FromPoint(Vector3 point, bool normalTowardsOrigin)
        {
            Vector3 normal = Vector3.Normalize(normalTowardsOrigin ? -point : point);
            return new System.Numerics.Plane()
            {
                Normal = normal,
                D = point.Length(),
            };
        }
        /// <summary>
        /// Constructs a plane given a point and normal.
        /// </summary>
        public static System.Numerics.Plane FromPoint(Vector3 point, Vector3 normal)
            => new()
            {
                Normal = Vector3.Normalize(normal),
                D = Vector3.Dot(-point, normal),
            };

        /// <summary>
        /// Returns distance from the plane defined by a point and normal to the origin.
        /// </summary>
        /// <param name="planePoint">Point in space the plane intersects.</param>
        /// <param name="planeNormal">The normal of the plane.</param>
        /// <returns>Shortest distance to the origin from the plane.</returns>
        public static float ComputeDistance(Vector3 planePoint, Vector3 planeNormal)
            => Vector3.Dot(-planePoint, planeNormal);

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
        public static Vector3 GetIntersectionPoint(System.Numerics.Plane p)
            => p.Normal * -p.D;

        public static void SetIntersectionPoint(ref System.Numerics.Plane p, Vector3 value)
            => p.D = Vector3.Dot(-value, p.Normal);

        public static System.Numerics.Plane Flipped(System.Numerics.Plane p)
            => new(-p.Normal, -p.D);

        public static EPlaneIntersection IntersectsBox(System.Numerics.Plane p, AABB box)
        {
            Vector3 min = Vector3.Zero;
            Vector3 max = Vector3.Zero;

            max.X = (p.Normal.X >= 0.0f) ? box.Min.X : box.Max.X;
            max.Y = (p.Normal.Y >= 0.0f) ? box.Min.Y : box.Max.Y;
            max.Z = (p.Normal.Z >= 0.0f) ? box.Min.Z : box.Max.Z;
            min.X = (p.Normal.X >= 0.0f) ? box.Max.X : box.Min.X;
            min.Y = (p.Normal.Y >= 0.0f) ? box.Max.Y : box.Min.Y;
            min.Z = (p.Normal.Z >= 0.0f) ? box.Max.Z : box.Min.Z;

            if (Vector3.Dot(p.Normal, max) + p.D > 0.0f)
                return EPlaneIntersection.Front;

            if (Vector3.Dot(p.Normal, min) + p.D < 0.0f)
                return EPlaneIntersection.Back;

            return EPlaneIntersection.Intersecting;
        }
        public static EPlaneIntersection IntersectsSphere(System.Numerics.Plane p, float radius, Vector3 center)
        {
            float dot = Vector3.Dot(center, p.Normal) + p.D;

            if (dot > radius)
                return EPlaneIntersection.Front;

            if (dot < -radius)
                return EPlaneIntersection.Back;

            return EPlaneIntersection.Intersecting;
        }

        public static System.Numerics.Plane Transform(System.Numerics.Plane p, Matrix4x4 transform)
            => FromPoint(Vector3.Transform(GetIntersectionPoint(p), transform), Vector3.TransformNormal(p.Normal, transform));

        public static void GetPlanePoints(System.Numerics.Plane p, float xExtent, float yExtent, out Vector3 bottomLeft, out Vector3 bottomRight, out Vector3 topLeft, out Vector3 topRight)
            => GetPlanePoints(GetIntersectionPoint(p), p.Normal, xExtent, yExtent, out bottomLeft, out bottomRight, out topLeft, out topRight);
        public static void GetPlanePoints(Vector3 position, Vector3 normal, float xExtent, float yExtent, out Vector3 bottomLeft, out Vector3 bottomRight, out Vector3 topLeft, out Vector3 topRight)
        {
            Quaternion r = XRMath.LookatAngles(normal).ToQuaternion();
            bottomLeft = position + Vector3.Transform(new Vector3(-0.5f * xExtent, -0.5f * yExtent, 0.0f), r);
            bottomRight = position + Vector3.Transform(new Vector3(0.5f * xExtent, -0.5f * yExtent, 0.0f), r);
            topLeft = position + Vector3.Transform(new Vector3(-0.5f * xExtent, 0.5f * yExtent, 0.0f), r);
            topRight = position + Vector3.Transform(new Vector3(0.5f * xExtent, 0.5f * yExtent, 0.0f), r);
        }

        /// <summary>
        /// Returns true if the plane intersects the triangle and front and back are populated with the resulting triangles.
        /// Returns false if the plane does not intersect the triangle and is coplanar.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="triangle"></param>
        /// <param name="front"></param>
        /// <param name="back"></param>
        /// <returns></returns>
        public static bool SplitTriangle(System.Numerics.Plane p, Triangle triangle, List<Triangle> front, List<Triangle> back)
        {
            const float epsilon = 0.00001f;

            float da = Vector3.Dot(p.Normal, triangle.A) - p.D;
            float db = Vector3.Dot(p.Normal, triangle.B) - p.D;
            float dc = Vector3.Dot(p.Normal, triangle.C) - p.D;

            if (da >= -epsilon && db >= -epsilon && dc >= -epsilon)
            {
                front.Add(triangle);
                return true;
            }

            if (da <= epsilon && db <= epsilon && dc <= epsilon)
            {
                back.Add(triangle);
                return true;
            }

            //Coplanar?
            if (Math.Abs(da) < epsilon && Math.Abs(db) < epsilon && Math.Abs(dc) < epsilon)
                return false;
            
            Vector3[] vertices = [triangle.A, triangle.B, triangle.C];
            int[] indices = new int[3];

            for (int i = 0; i < 3; i++)
            {
                int j = (i + 1) % 3;
                int k = (i + 2) % 3;
                float di = Vector3.Dot(p.Normal, vertices[i]) - p.D;
                float dj = Vector3.Dot(p.Normal, vertices[j]) - p.D;
                float dk = Vector3.Dot(p.Normal, vertices[k]) - p.D;

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
                    Vector3 v = vertices[i] + s * (vertices[j] - vertices[i]);

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
                    Vector3 v = vertices[i] + s * (vertices[k] - vertices[i]);

                    var ind = indices[i];
                    var f = front[ind];
                    var b = back[ind];
                    f.C = v;
                    b.A = v;
                    front[ind] = f;
                    back[ind] = b;
                }
            }
            return true;
        }
    }
}