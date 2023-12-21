using Extensions;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Geometry
{
    /// <summary>
    /// Helper class to handle all intersections, containment, distances, and closest point situations between various types of goemetry.
    /// </summary>
    public static class Collision
    {
        /// <summary>
        /// Determines the closest point between a point and a triangle.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="vertex1">The first vertex to test.</param>
        /// <param name="vertex2">The second vertex to test.</param>
        /// <param name="vertex3">The third vertex to test.</param>
        /// <param name="result">When the method completes, contains the closest point between the two objects.</param>
        public static Vec3 ClosestPointPointTriangle(Vec3 point, Vec3 vertex1, Vec3 vertex2, Vec3 vertex3)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 136

            //Check if P in vertex region outside A
            Vec3 ab = vertex2 - vertex1;
            Vec3 ac = vertex3 - vertex1;
            Vec3 ap = point - vertex1;

            float d1 = Vec3.Dot(ab, ap);
            float d2 = Vec3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f)
                return vertex1; //Barycentric coordinates (1,0,0)

            //Check if P in vertex region outside B
            Vec3 bp = point - vertex2;
            float d3 = Vec3.Dot(ab, bp);
            float d4 = Vec3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3)
                return vertex2; // Barycentric coordinates (0,1,0)

            //Check if P in edge region of AB, if so return projection of P onto AB
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                float v = d1 / (d1 - d3);
                return vertex1 + v * ab; //Barycentric coordinates (1-v,v,0)
            }

            //Check if P in vertex region outside C
            Vec3 cp = point - vertex3;
            float d5 = Vec3.Dot(ab, cp);
            float d6 = Vec3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6)
                return vertex3; //Barycentric coordinates (0,0,1)

            //Check if P in edge region of AC, if so return projection of P onto AC
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                float w = d2 / (d2 - d6);
                return vertex1 + w * ac; //Barycentric coordinates (1-w,0,w)
            }

            //Check if P in edge region of BC, if so return projection of P onto BC
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return vertex2 + w * (vertex3 - vertex2); //Barycentric coordinates (0,1-w,w)
            }

            //P inside face region. Compute Q through its Barycentric coordinates (u,v,w)
            float denom = 1.0f / (va + vb + vc);
            float v2 = vb * denom;
            float w2 = vc * denom;
            return vertex1 + ab * v2 + ac * w2; //= u*vertex1 + v*vertex2 + w*vertex3, u = va * denom = 1.0f - v - w
        }
        public static Vec3 ClosestPointPlanePoint(Plane plane, Vec3 point)
            => point - ((Vec3.Dot(plane.Normal, point) - plane.Distance) * plane.Normal);

        public static Vec3 ClosestPointAABBPoint(Vec3 min, Vec3 max, Vec3 point)
            => Vec3.ComponentMin(Vec3.ComponentMax(point, min), max);

        public static Vec3 ClosestPointSpherePoint(Vec3 center, float radius, Vec3 point)
        {
            Vec3 dir = point - center;
            dir.Normalize();
            return dir * radius + center;
        }

        /// <summary>
        /// Determines the closest point between a <see cref="Sphere"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere1">The first sphere to test.</param>
        /// <param name="sphere2">The second sphere to test.</param>
        /// <param name="result">When the method completes, contains the closest point between the two objects;
        /// or, if the point is directly in the center of the sphere, contains <see cref="Vec3.Zero"/>.</param>
        /// <remarks>
        /// If the two spheres are overlapping, but not directly on top of each other, the closest point
        /// is the 'closest' point of intersection. This can also be considered is the deepest point of
        /// intersection.
        /// </remarks>
        public static Vec3 ClosestPointSphereSphere(Vec3 sphere1Center, float sphere1Radius, Vec3 sphere2Center)
            => ClosestPointSpherePoint(sphere1Center, sphere1Radius, sphere2Center);

        public static float DistancePlanePoint(Plane plane, Vec3 point)
            => DistancePlanePoint(plane.Normal, plane.Distance, point);
        public static float DistancePlanePoint(Vec3 planeNormal, float planeOriginDistance, Vec3 point)
            => Vec3.Dot(planeNormal, point) + planeOriginDistance;
        public static Vec3 ClosestPlanePointToPoint(Vec3 planeNormal, float planeOriginDistance, Vec3 point)
            => point - (planeNormal * DistancePlanePoint(planeNormal, planeOriginDistance, point));

        public static EContainment SphereContainsAABB(Vec3 center, float radius, Vec3 minimum, Vec3 maximum)
        {
            float r2 = radius * radius;
            if ((center - minimum).LengthSquared < r2 &&
                (center - maximum).LengthSquared < r2)
                return EContainment.Contains;

            Sphere sphere = new Sphere(center, radius);
            EPlaneIntersection[] intersections = new EPlaneIntersection[]
            {
                PlaneIntersectsSphere(new Plane(maximum, Vec3.UnitX), sphere),
                PlaneIntersectsSphere(new Plane(minimum, -Vec3.UnitX), sphere),
                PlaneIntersectsSphere(new Plane(maximum, Vec3.UnitY), sphere),
                PlaneIntersectsSphere(new Plane(minimum, -Vec3.UnitY), sphere),
                PlaneIntersectsSphere(new Plane(maximum, Vec3.UnitZ), sphere),
                PlaneIntersectsSphere(new Plane(minimum, -Vec3.UnitZ), sphere),
            };
            if (intersections.Any(x => x == EPlaneIntersection.Front))
                return EContainment.Disjoint;

            return EContainment.Intersects;
        }

        public static float DistancePlanePoint(Vec3 normal, Vec3 planePoint, Vec3 point)
        {
            return Vec3.Dot(normal, point) + planePoint.Dot(normal);
        }
        public static float DistanceAABBPoint(Vec3 min, Vec3 max, Vec3 point)
        {
            float distance = 0.0f;

            if (point.X < min.X)
                distance += (min.X - point.X) * (min.X - point.X);
            if (point.X > max.X)
                distance += (point.X - max.X) * (point.X - max.X);

            if (point.Y < min.Y)
                distance += (min.Y - point.Y) * (min.Y - point.Y);
            if (point.Y > max.Y)
                distance += (point.Y - max.Y) * (point.Y - max.Y);

            if (point.Z < min.Z)
                distance += (min.Z - point.Z) * (min.Z - point.Z);
            if (point.Z > max.Z)
                distance += (point.Z - max.Z) * (point.Z - max.Z);

            return (float)Math.Sqrt(distance);
        }

        /// <summary>
        /// Determines the distance between a <see cref="BoundingBox"/> and a <see cref="BoundingBox"/>.
        /// </summary>
        /// <param name="box1">The first box to test.</param>
        /// <param name="box2">The second box to test.</param>
        /// <returns>The distance between the two objects.</returns>
        public static float DistanceAABBAABB(Vec3 box1Min, Vec3 box1Max, Vec3 box2Min, Vec3 box2Max)
        {
            float distance = 0.0f;

            for (int i = 0; i < 3; ++i)
                if (box1Min[i] > box2Max[i])
                {
                    float delta = box2Max[i] - box1Min[i];
                    distance += delta * delta;
                }
                else if (box2Min[i] > box1Max[i])
                {
                    float delta = box1Max[i] - box2Min[i];
                    distance += delta * delta;
                }

            return (float)Math.Sqrt(distance);
        }

        public static EContainment AABBContainsCapsule(Vec3 boxMin, Vec3 boxMax, CapsuleY capsule)
        {
            throw new NotImplementedException();
        }
        public static float DistanceSpherePoint(Vec3 sphereCenter, float sphereRadius, Vec3 point)
        {
            return (sphereCenter.DistanceTo(point) - sphereRadius).ClampMin(0.0f);
        }
        public static float DistanceSphereSphere(float sphere1Radius, Vec3 sphere1Pos, float sphere2Radius, Vec3 sphere2Pos)
        {
            return Math.Max(sphere1Pos.DistanceTo(sphere2Pos) - sphere1Radius - sphere2Radius, 0f);
        }
        public static bool RayIntersectsPoint(Ray ray, Vec3 point)
        {
            Vec3 m = ray.StartPoint - point;

            //Same thing as RayIntersectsSphere except that the radius of the sphere (point)
            //is the epsilon for zero.
            float b = Vec3.Dot(m, ray.Direction);
            float c = Vec3.Dot(m, m) - SingleExtensions.ZeroTolerance;

            if (c > 0.0f && b > 0.0f)
                return false;

            float discriminant = b * b - c;

            if (discriminant < 0.0f)
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Ray"/>.
        /// </summary>
        /// <param name="ray1">The first ray to test.</param>
        /// <param name="ray2">The second ray to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="Vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersect.</returns>
        /// <remarks>
        /// This method performs a ray vs ray intersection test based on the following formula
        /// from Goldman.
        /// <code>s = det([o_2 - o_1, d_2, d_1 x d_2]) / ||d_1 x d_2||^2</code>
        /// <code>t = det([o_2 - o_1, d_1, d_1 x d_2]) / ||d_1 x d_2||^2</code>
        /// Where o_1 is the position of the first ray, o_2 is the position of the second ray,
        /// d_1 is the normalized direction of the first ray, d_2 is the normalized direction
        /// of the second ray, det denotes the determinant of a matrix, x denotes the cross
        /// product, [ ] denotes a matrix, and || || denotes the length or magnitude of a vector.
        /// </remarks>
        public static bool RayIntersectsRay(Ray ray1, Ray ray2, out Vec3 point)
        {
            //Source: Real-Time Rendering, Third Edition
            //Reference: Page 780

            Vec3 cross = Vec3.Cross(ray1.Direction, ray2.Direction);
            float denominator = cross.Length;

            //Lines are parallel.
            if (denominator.IsZero())
            {
                //Lines are parallel and on top of each other.
                if (ray2.StartPoint.X.EqualTo(ray1.StartPoint.X) &&
                    ray2.StartPoint.Y.EqualTo(ray1.StartPoint.Y) &&
                    ray2.StartPoint.Z.EqualTo(ray1.StartPoint.Z))
                {
                    point = Vec3.Zero;
                    return true;
                }
            }

            denominator *= denominator;

            //3x3 matrix for the first ray.
            float m11 = ray2.StartPoint.X - ray1.StartPoint.X;
            float m12 = ray2.StartPoint.Y - ray1.StartPoint.Y;
            float m13 = ray2.StartPoint.Z - ray1.StartPoint.Z;
            float m21 = ray2.Direction.X;
            float m22 = ray2.Direction.Y;
            float m23 = ray2.Direction.Z;
            float m31 = cross.X;
            float m32 = cross.Y;
            float m33 = cross.Z;

            //Determinant of first matrix.
            float dets =
                m11 * m22 * m33 +
                m12 * m23 * m31 +
                m13 * m21 * m32 -
                m11 * m23 * m32 -
                m12 * m21 * m33 -
                m13 * m22 * m31;

            //3x3 matrix for the second ray.
            m21 = ray1.Direction.X;
            m22 = ray1.Direction.Y;
            m23 = ray1.Direction.Z;

            //Determinant of the second matrix.
            float dett =
                m11 * m22 * m33 +
                m12 * m23 * m31 +
                m13 * m21 * m32 -
                m11 * m23 * m32 -
                m12 * m21 * m33 -
                m13 * m22 * m31;

            //t values of the point of intersection.
            float s = dets / denominator;
            float t = dett / denominator;

            //The points of intersection.
            Vec3 point1 = ray1.StartPoint + (s * ray1.Direction);
            Vec3 point2 = ray2.StartPoint + (t * ray2.Direction);

            //If the points are not equal, no intersection has occurred.
            if (!point2.X.EqualTo(point1.X) ||
                !point2.Y.EqualTo(point1.Y) ||
                !point2.Z.EqualTo(point1.Z))
            {
                point = Vec3.Zero;
                return false;
            }

            point = point1;
            return true;
        }

        public static EContainment AABBContainsBox(Vec3 box1Min, Vec3 box1Max, Vec3 box2HalfExtents, Matrix box2Transform)
        {
            Vec3[] corners = BoundingBox.GetCorners(box2HalfExtents, box2Transform);
            int numIn = 0, numOut = 0;
            for (int i = 0; i < 8; ++i)
            {
                if (AABBContainsPoint(box1Min, box1Max, corners[i]))
                    ++numIn;
                else
                    ++numOut;
            }
            if (numOut == 0)
                return EContainment.Contains;
            if (numIn == 0)
                return EContainment.Disjoint;
            return EContainment.Intersects;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="plane">The plane to test.</param>
        /// <param name="distance">When the method completes, contains the distance of the intersection,
        /// or 0 if there was no intersection.</param>
        /// <returns>Whether the two objects intersect.</returns>
        public static bool RayIntersectsPlane(Vec3 rayStartPoint, Vec3 rayDirection, Vec3 planePoint, Vec3 planeNormal, out float distance)
        {
            rayDirection.Normalize();
            planeNormal.Normalize();

            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 175

            float direction = Vec3.Dot(planeNormal, rayDirection);

            if (direction.IsZero())
            {
                distance = 0.0f;
                return false;
            }

            float position = Vec3.Dot(planeNormal, rayStartPoint);
            distance = (-Plane.ComputeDistance(planePoint, planeNormal) - position) / direction;

            if (distance < 0.0f)
            {
                distance = 0.0f;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="plane">The plane to test</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="Vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsPlane(Vec3 rayStartPoint, Vec3 rayDirection, Vec3 planePoint, Vec3 planeNormal, out Vec3 point)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 175

            if (!RayIntersectsPlane(rayStartPoint, rayDirection, planePoint, planeNormal, out float distance))
            {
                point = Vec3.Zero;
                return false;
            }

            point = rayStartPoint + (rayDirection.Normalized() * distance);
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a triangle.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <param name="distance">When the method completes, contains the distance of the intersection,
        /// or 0 if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        /// <remarks>
        /// This method tests if the ray intersects either the front or back of the triangle.
        /// If the ray is parallel to the triangle's plane, no intersection is assumed to have
        /// happened. If the intersection of the ray and the triangle is behind the origin of
        /// the ray, no intersection is assumed to have happened. In both cases of assumptions,
        /// this method returns false.
        /// </remarks>
        public static bool RayIntersectsTriangle(Ray ray, Vec3 vertex1, Vec3 vertex2, Vec3 vertex3, out float distance)
        {
            //Source: Fast Minimum Storage Ray / Triangle Intersection
            //Reference: http://www.cs.virginia.edu/~gfx/Courses/2003/ImageSynthesis/papers/Acceleration/Fast%20MinimumStorage%20RayTriangle%20Intersection.pdf

            //Compute vectors along two edges of the triangle.
            Vec3 edge1 = Vec3.Zero, edge2 = Vec3.Zero;

            //Edge 1
            edge1.X = vertex2.X - vertex1.X;
            edge1.Y = vertex2.Y - vertex1.Y;
            edge1.Z = vertex2.Z - vertex1.Z;

            //Edge2
            edge2.X = vertex3.X - vertex1.X;
            edge2.Y = vertex3.Y - vertex1.Y;
            edge2.Z = vertex3.Z - vertex1.Z;

            //Cross product of ray direction and edge2 - first part of determinant.
            Vec3 directioncrossedge2 = Vec3.Zero;
            directioncrossedge2.X = (ray.Direction.Y * edge2.Z) - (ray.Direction.Z * edge2.Y);
            directioncrossedge2.Y = (ray.Direction.Z * edge2.X) - (ray.Direction.X * edge2.Z);
            directioncrossedge2.Z = (ray.Direction.X * edge2.Y) - (ray.Direction.Y * edge2.X);

            //Compute the determinant.
            float determinant;
            //Dot product of edge1 and the first part of determinant.
            determinant = (edge1.X * directioncrossedge2.X) + (edge1.Y * directioncrossedge2.Y) + (edge1.Z * directioncrossedge2.Z);

            //If the ray is parallel to the triangle plane, there is no collision.
            //This also means that we are not culling, the ray may hit both the
            //back and the front of the triangle.
            if (determinant.IsZero())
            {
                distance = 0f;
                return false;
            }

            float inversedeterminant = 1.0f / determinant;

            //Calculate the U parameter of the intersection point.
            Vec3 distanceVector = Vec3.Zero;
            distanceVector.X = ray.StartPoint.X - vertex1.X;
            distanceVector.Y = ray.StartPoint.Y - vertex1.Y;
            distanceVector.Z = ray.StartPoint.Z - vertex1.Z;

            float triangleU;
            triangleU = (distanceVector.X * directioncrossedge2.X) + (distanceVector.Y * directioncrossedge2.Y) + (distanceVector.Z * directioncrossedge2.Z);
            triangleU *= inversedeterminant;

            //Make sure it is inside the triangle.
            if (triangleU < 0f || triangleU > 1f)
            {
                distance = 0f;
                return false;
            }

            //Calculate the V parameter of the intersection point.
            Vec3 distancecrossedge1 = Vec3.Zero;
            distancecrossedge1.X = (distanceVector.Y * edge1.Z) - (distanceVector.Z * edge1.Y);
            distancecrossedge1.Y = (distanceVector.Z * edge1.X) - (distanceVector.X * edge1.Z);
            distancecrossedge1.Z = (distanceVector.X * edge1.Y) - (distanceVector.Y * edge1.X);

            float triangleV;
            triangleV = ((ray.Direction.X * distancecrossedge1.X) + (ray.Direction.Y * distancecrossedge1.Y)) + (ray.Direction.Z * distancecrossedge1.Z);
            triangleV *= inversedeterminant;

            //Make sure it is inside the triangle.
            if (triangleV < 0f || triangleU + triangleV > 1f)
            {
                distance = 0f;
                return false;
            }

            //Compute the distance along the ray to the triangle.
            float raydistance;
            raydistance = (edge2.X * distancecrossedge1.X) + (edge2.Y * distancecrossedge1.Y) + (edge2.Z * distancecrossedge1.Z);
            raydistance *= inversedeterminant;

            //Is the triangle behind the ray origin?
            if (raydistance < 0f)
            {
                distance = 0f;
                return false;
            }

            distance = raydistance;
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a triangle.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="Vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsTriangle(Ray ray, Vec3 vertex1, Vec3 vertex2, Vec3 vertex3, out Vec3 point)
        {
            if (!RayIntersectsTriangle(ray, vertex1, vertex2, vertex3, out float distance))
            {
                point = Vec3.Zero;
                return false;
            }

            point = ray.StartPoint + (ray.Direction * distance);
            return true;
        }
        public static bool RayIntersectsBoxDistance(Vec3 rayStartPoint, Vec3 rayDirection, Vec3 boxHalfExtents, Matrix boxInverseTransform, out float distance)
        {
            //Transform ray to untransformed box space
            Vec3 rayEndPoint = rayStartPoint + rayDirection;
            rayStartPoint = Vec3.TransformPosition(rayStartPoint, boxInverseTransform);
            rayEndPoint = Vec3.TransformPosition(rayEndPoint, boxInverseTransform);
            rayDirection = rayEndPoint - rayStartPoint;
            return RayIntersectsAABBDistance(rayStartPoint, rayDirection, -boxHalfExtents, boxHalfExtents, out distance);
        }

        #region RayIntersectsAABBDistance
        public static bool RayIntersectsAABBDistance(Ray ray, BoundingBox box, out float distance)
            => RayIntersectsAABBDistance(ray.StartPoint, ray.Direction, box.Minimum, box.Maximum, out distance);
        public static bool RayIntersectsAABBDistance(Ray ray, Vec3 boxMin, Vec3 boxMax, out float distance)
            => RayIntersectsAABBDistance(ray.StartPoint, ray.Direction, boxMin, boxMax, out distance);
        public static bool RayIntersectsAABBDistance(Vec3 rayStartPoint, Vec3 rayDirection, BoundingBox box, out float distance)
             => RayIntersectsAABBDistance(rayStartPoint, rayDirection, box.Minimum, box.Maximum, out distance);
        public static bool RayIntersectsAABBDistance(Vec3 rayStartPoint, Vec3 rayDirection, Vec3 boxMin, Vec3 boxMax, out float distance, ENormalizeOption normalize = ENormalizeOption.FastSafe)
        {
            rayDirection.Normalize(normalize);

            distance = 0.0f;
            float tmax = float.MaxValue;

            for (int i = 0; i < 3; ++i)
                if (rayDirection[i].IsZero())
                {
                    if (rayStartPoint[i] < boxMin[i] || rayStartPoint[i] > boxMax[i])
                    {
                        distance = 0.0f;
                        return false;
                    }
                }
                else
                {
                    float inverse = 1.0f / rayDirection[i];
                    float t1 = (boxMin[i] - rayStartPoint[i]) * inverse;
                    float t2 = (boxMax[i] - rayStartPoint[i]) * inverse;

                    if (t1 > t2)
                    {
                        float temp = t1;
                        t1 = t2;
                        t2 = temp;
                    }

                    distance = Math.Max(t1, distance);
                    tmax = Math.Min(t2, tmax);

                    if (distance > tmax)
                    {
                        distance = 0.0f;
                        return false;
                    }
                }
            return true;
        }
        #endregion

        private static bool RaySlabIntersect(float slabmin, float slabmax, float raystart, float rayend, ref float tbenter, ref float tbexit)
        {
            float raydir = rayend - raystart;

            // ray parallel to the slab
            if (Math.Abs(raydir) < 1.0E-9f)
            {
                // ray parallel to the slab, but ray not inside the slab planes
                if (raystart < slabmin || raystart > slabmax)
                    return false;
                // ray parallel to the slab, but ray inside the slab planes
                else
                    return true;
            }

            // slab's enter and exit parameters
            float tsenter = (slabmin - raystart) / raydir;
            float tsexit = (slabmax - raystart) / raydir;

            // order the enter / exit values.
            if (tsenter > tsexit)
                (tsenter, tsexit) = (tsexit, tsenter);

            // make sure the slab interval and the current box intersection interval overlap
            if (tbenter > tsexit || tsenter > tbexit)
            {
                // nope. Ray missed the box.
                return false;
            }
            // yep, the slab and current intersection interval overlap
            else
            {
                // update the intersection interval
                tbenter = Math.Max(tbenter, tsenter);
                tbexit = Math.Min(tbexit, tsexit);
                return true;
            }
        }

        public static bool SegmentIntersectsAABB(Vec3 segmentStart, Vec3 segmentEnd, Vec3 boxMin, Vec3 boxMax, out Vec3 enterPoint, out Vec3 exitPoint)
        {
            enterPoint = segmentStart;
            exitPoint = segmentEnd;

            // initialise to the segment's boundaries. 
            float tenter = 0.0f;
            float texit = 1.0f;

            // test X slab
            if (!RaySlabIntersect(boxMin.X, boxMax.X, segmentStart.X, segmentEnd.X, ref tenter, ref texit))
                return false;

            // test Y slab
            if (!RaySlabIntersect(boxMin.Y, boxMax.Y, segmentStart.Y, segmentEnd.Y, ref tenter, ref texit))
                return false;

            // test Z slab
            if (!RaySlabIntersect(boxMin.Z, boxMax.Z, segmentStart.Z, segmentEnd.Z, ref tenter, ref texit))
                return false;

            enterPoint = Interp.Lerp(segmentStart, segmentEnd, tenter);
            exitPoint = Interp.Lerp(segmentStart, segmentEnd, texit);

            // all intersections in the green.
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="box">The box to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="Vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsAABB(Ray ray, Vec3 boxMin, Vec3 boxMax, out Vec3 point)
        {
            if (!RayIntersectsAABBDistance(ray.StartPoint, ray.Direction, boxMin, boxMax, out float distance))
            {
                point = Vec3.Zero;
                return false;
            }

            point = ray.StartPoint + (ray.Direction * distance);
            return true;
        }
        public static bool RayIntersectsBox(Vec3 rayStartPoint, Vec3 rayDirection, Vec3 boxHalfExtents, Matrix boxInverseTransform, out Vec3 point)
        {
            if (!RayIntersectsBoxDistance(rayStartPoint, rayDirection, boxHalfExtents, boxInverseTransform, out float distance))
            {
                point = Vec3.Zero;
                return false;
            }

            point = rayStartPoint + (rayDirection * distance);
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="distance">When the method completes, contains the distance of the intersection,
        /// or 0 if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsSphere(Vec3 rayStart, Vec3 rayDir, Vec3 sphereCenter, float sphereRadius, out float distance)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 177

            rayDir.Normalize();

            Vec3 m = rayStart - sphereCenter;

            float b = Vec3.Dot(m, rayDir);
            float c = Vec3.Dot(m, m) - (sphereRadius * sphereRadius);

            if (c > 0f && b > 0f)
            {
                distance = 0f;
                return false;
            }

            float discriminant = b * b - c;

            if (discriminant < 0f)
            {
                distance = 0f;
                return false;
            }

            distance = -b - (float)Math.Sqrt(discriminant);

            if (distance < 0f)
                distance = 0f;

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Ray"/> and a <see cref="Sphere"/>. 
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="point">When the method completes, contains the point of intersection,
        /// or <see cref="Vec3.Zero"/> if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool RayIntersectsSphere(Vec3 rayStart, Vec3 rayDir, Vec3 sphereCenter, float sphereRadius, out Vec3 point)
        {
            if (!RayIntersectsSphere(rayStart, rayDir, sphereCenter, sphereRadius, out float distance))
            {
                point = Vec3.Zero;
                return false;
            }

            point = rayStart + (rayDir * distance);
            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a point.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static EPlaneIntersection PlaneIntersectsPoint(Plane plane, Vec3 point)
        {
            float distance = Vec3.Dot(plane.Normal, point);
            distance += plane.Distance;

            if (distance > 0.0f)
                return EPlaneIntersection.Front;

            if (distance < 0.0f)
                return EPlaneIntersection.Back;

            return EPlaneIntersection.Intersecting;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane1">The first plane to test.</param>
        /// <param name="plane2">The second plane to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool PlaneIntersectsPlane(Plane plane1, Plane plane2)
        {
            Vec3 direction = Vec3.Cross(plane1.Normal, plane2.Normal);

            //If direction is the zero vector, the planes are parallel and possibly
            //coincident. It is not an intersection. The dot product will tell us.
            float denominator = Vec3.Dot(direction, direction);

            if (denominator.IsZero())
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a <see cref="Plane"/>.
        /// </summary>
        /// <param name="plane1">The first plane to test.</param>
        /// <param name="plane2">The second plane to test.</param>
        /// <param name="line">When the method completes, contains the line of intersection
        /// as a <see cref="Ray"/>, or a zero ray if there was no intersection.</param>
        /// <returns>Whether the two objects intersected.</returns>
        /// <remarks>
        /// Although a ray is set to have an origin, the ray returned by this method is really
        /// a line in three dimensions which has no real origin. The ray is considered valid when
        /// both the positive direction is used and when the negative direction is used.
        /// </remarks>
        public static bool PlaneIntersectsPlane(Plane plane1, Plane plane2, out Ray line)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 207

            Vec3 direction = Vec3.Cross(plane1.Normal, plane2.Normal);

            //If direction is the zero vector, the planes are parallel and possibly
            //coincident. It is not an intersection. The dot product will tell us.
            float denominator = Vec3.Dot(direction, direction);

            //We assume the planes are normalized, therefore the denominator
            //only serves as a parallel and coincident check. Otherwise we need
            //to divide the point by the denominator.
            if (denominator.IsZero())
            {
                line = new Ray();
                return false;
            }

            Vec3 temp = plane1.Distance * plane2.Normal - plane2.Distance * plane1.Normal;
            Vec3 point = Vec3.Cross(temp, direction);

            line = new Ray(point, point + direction.Normalized());

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a triangle.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static EPlaneIntersection PlaneIntersectsTriangle(Plane plane, Vec3 vertex1, Vec3 vertex2, Vec3 vertex3)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 207

            EPlaneIntersection test1 = PlaneIntersectsPoint(plane, vertex1);
            EPlaneIntersection test2 = PlaneIntersectsPoint(plane, vertex2);
            EPlaneIntersection test3 = PlaneIntersectsPoint(plane, vertex3);

            if (test1 == EPlaneIntersection.Front && test2 == EPlaneIntersection.Front && test3 == EPlaneIntersection.Front)
                return EPlaneIntersection.Front;

            if (test1 == EPlaneIntersection.Back && test2 == EPlaneIntersection.Back && test3 == EPlaneIntersection.Back)
                return EPlaneIntersection.Back;

            return EPlaneIntersection.Intersecting;
        }
        public static EPlaneIntersection PlaneIntersectsBox(Plane plane, Vec3 boxMin, Vec3 boxMax, Matrix boxInverseMatrix)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 161

            //Transform plane into untransformed box space
            plane = boxInverseMatrix.TransformPlane(plane);

            Vec3 min = Vec3.Zero;
            Vec3 max = Vec3.Zero;

            max.X = (plane.Normal.X >= 0.0f) ? boxMin.X : boxMax.X;
            max.Y = (plane.Normal.Y >= 0.0f) ? boxMin.Y : boxMax.Y;
            max.Z = (plane.Normal.Z >= 0.0f) ? boxMin.Z : boxMax.Z;
            min.X = (plane.Normal.X >= 0.0f) ? boxMax.X : boxMin.X;
            min.Y = (plane.Normal.Y >= 0.0f) ? boxMax.Y : boxMin.Y;
            min.Z = (plane.Normal.Z >= 0.0f) ? boxMax.Z : boxMin.Z;

            if (Vec3.Dot(plane.Normal, max) + plane.Distance > 0.0f)
                return EPlaneIntersection.Front;

            if (Vec3.Dot(plane.Normal, min) + plane.Distance < 0.0f)
                return EPlaneIntersection.Back;

            return EPlaneIntersection.Intersecting;
        }


        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Plane"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="plane">The plane to test.</param>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static EPlaneIntersection PlaneIntersectsSphere(Plane plane, Sphere sphere)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 160

            float distance = Vec3.Dot(plane.Normal, sphere.Center);
            distance += plane.Distance;

            if (distance > sphere.Radius)
                return EPlaneIntersection.Front;

            if (distance < -sphere.Radius)
                return EPlaneIntersection.Back;

            return EPlaneIntersection.Intersecting;
        }

        /* This implementation is wrong
        /// <summary>
        /// Determines whether there is an intersection between a <see cref="SharpDX.Box"/> and a triangle.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool BoxIntersectsTriangle(Box box, Vec3 vertex1, Vec3 vertex2, Vec3 vertex3)
        {
            if (BoxContainsPoint(box, vertex1) == EContainment.Contains)
                return true;
            if (BoxContainsPoint(box, vertex2) == EContainment.Contains)
                return true;
            if (BoxContainsPoint(box, vertex3) == EContainment.Contains)
                return true;
            return false;
        }
        */

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Box"/> and a <see cref="Box"/>.
        /// </summary>
        /// <param name="box1">The first box to test.</param>
        /// <param name="box2">The second box to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool AABBIntersectsAABB(BoundingBox box1, BoundingBox box2)
        {
            if (box1.Min.X > box2.Max.X || box2.Min.X > box1.Max.X)
                return false;

            if (box1.Min.Y > box2.Max.Y || box2.Min.Y > box1.Max.Y)
                return false;

            if (box1.Min.Z > box2.Max.Z || box2.Min.Z > box1.Max.Z)
                return false;

            return true;
        }

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="BoundingBox"/> and a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="sphere">The sphere to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool BoxIntersectsSphere(Vec3 boxHalfExtents, Matrix boxInverseTransform, Vec3 sphereCenter, float sphereRadius)
        {
            sphereCenter = boxInverseTransform.TransformPosition(sphereCenter);
            return AABBIntersectsSphere(-boxHalfExtents, boxHalfExtents, sphereCenter, sphereRadius);
        }

        public static bool AABBIntersectsSphere(Vec3 boxMin, Vec3 boxMax, Vec3 sphereCenter, float sphereRadius)
            => sphereCenter.DistanceToSquared(sphereCenter.Clamped(boxMin, boxMax)) <= sphereRadius * sphereRadius;

        /// <summary>
        /// Determines whether there is an intersection between a <see cref="Sphere"/> and a triangle.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>Whether the two objects intersected.</returns>
        public static bool SphereIntersectsTriangle(Vec3 sphereCenter, float sphereRadius, Vec3 vertex1, Vec3 vertex2, Vec3 vertex3)
        {
            //Source: Real-Time Collision Detection by Christer Ericson
            //Reference: Page 167

            Vec3 point = ClosestPointPointTriangle(sphereCenter, vertex1, vertex2, vertex3);
            Vec3 v = point - sphereCenter;

            return v.LengthSquared <= sphereRadius * sphereRadius;
        }
        public static bool SphereIntersectsSphere(Vec3 sphere1Center, float sphere1Radius, Vec3 sphere2Center, float sphere2Radius)
        {
            float radiisum = sphere1Radius + sphere2Radius;
            return sphere1Center.DistanceToSquared(sphere2Center) <= radiisum * radiisum;
        }
        public static bool BoxContainsPoint(Vec3 boxHalfExtents, Matrix boxInverseTransform, Vec3 point)
        {
            //Transform point into untransformed box space
            point = boxInverseTransform.TransformPosition(point);
            return AABBContainsPoint(-boxHalfExtents, boxHalfExtents, point);
        }
        public static bool AABBContainsPoint(Vec3 boxMin, Vec3 boxMax, Vec3 point)
        {
            if (boxMin.X <= point.X && boxMax.X >= point.X &&
                boxMin.Y <= point.Y && boxMax.Y >= point.Y &&
                boxMin.Z <= point.Z && boxMax.Z >= point.Z)
                return true;

            return false;
        }
        /* This implementation is wrong
        /// <summary>
        /// Determines whether a <see cref="SharpDX.Box"/> contains a triangle.
        /// </summary>
        /// <param name="box">The box to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static EContainment BoxContainsTriangle(Box box, Vec3 vertex1, Vec3 vertex2, Vec3 vertex3)
        {
            EContainment test1 = BoxContainsPoint(box, vertex1);
            EContainment test2 = BoxContainsPoint(box, vertex2);
            EContainment test3 = BoxContainsPoint(box, vertex3);
            if (test1 == EContainment.Contains && test2 == EContainment.Contains && test3 == EContainment.Contains)
                return EContainment.Contains;
            if (test1 == EContainment.Contains || test2 == EContainment.Contains || test3 == EContainment.Contains)
                return EContainment.Intersects;
            return EContainment.Disjoint;
        }
        */
        public static EContainment BoxContainsAABB(
            Vec3 box1HalfExtents,
            Matrix box1Transform,
            Vec3 box2Min,
            Vec3 box2Max)
        {
            return FrustumContainsBox1(
                BoundingBox.GetFrustum(box1HalfExtents, box1Transform), (box2Max - box2Min) / 2.0f,
                Matrix.CreateTranslation((box2Max + box2Min) / 2.0f));
        }
        public static EContainment BoxContainsBox(
            Vec3 box1HalfExtents,
            Matrix box1Transform,
            Vec3 box2HalfExtents,
            Matrix box2Transform)
        {
            return FrustumContainsBox1(
                BoundingBox.GetFrustum(box1HalfExtents, box1Transform), box2HalfExtents, box2Transform);
        }
        public static EContainment AABBContainsAABB(Vec3 box1Min, Vec3 box1Max, Vec3 box2Min, Vec3 box2Max)
        {
            if (box1Max.X < box2Min.X || box1Min.X > box2Max.X)
                return EContainment.Disjoint;

            if (box1Max.Y < box2Min.Y || box1Min.Y > box2Max.Y)
                return EContainment.Disjoint;

            if (box1Max.Z < box2Min.Z || box1Min.Z > box2Max.Z)
                return EContainment.Disjoint;

            if (box1Min.X <= box2Min.X && box2Max.X <= box1Max.X &&
                box1Min.Y <= box2Min.Y && box2Max.Y <= box1Max.Y &&
                box1Min.Z <= box2Min.Z && box2Max.Z <= box1Max.Z)
                return EContainment.Contains;

            return EContainment.Intersects;
        }
        public static EContainment BoxContainsSphere(Vec3 boxHalfExtents, Matrix boxTransform, Vec3 sphereCenter, float sphereRadius)
        {
            IFrustum f = BoundingBox.GetFrustum(boxHalfExtents, boxTransform);
            return FrustumContainsSphere(f, sphereCenter, sphereRadius);

            //Transform sphere into untransformed box space
            //sphereCenter = Vec3.TransformPosition(sphereCenter, boxInverseTransform);
            //return AABBContainsSphere(-boxHalfExtents, boxHalfExtents, sphereCenter, sphereRadius);
        }
        public static EContainment AABBContainsSphere(Vec3 boxMin, Vec3 boxMax, Vec3 sphereCenter, float sphereRadius)
        {
            Vec3 vector = sphereCenter.Clamp(boxMin, boxMax);
            float distance = sphereCenter.DistanceToSquared(vector);

            if (distance > sphereRadius * sphereRadius)
                return EContainment.Disjoint;

            if ((((boxMin.X + sphereRadius <= sphereCenter.X) && (sphereCenter.X <= boxMax.X - sphereRadius)) && ((boxMax.X - boxMin.X > sphereRadius) &&
                (boxMin.Y + sphereRadius <= sphereCenter.Y))) && (((sphereCenter.Y <= boxMax.Y - sphereRadius) && (boxMax.Y - boxMin.Y > sphereRadius)) &&
                (((boxMin.Z + sphereRadius <= sphereCenter.Z) && (sphereCenter.Z <= boxMax.Z - sphereRadius)) && (boxMax.Z - boxMin.Z > sphereRadius))))
            {
                return EContainment.Contains;
            }

            return EContainment.Intersects;
        }

        /// <summary>
        /// Determines whether a <see cref="Sphere"/> contains a point.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static bool SphereContainsPoint(Vec3 center, float radius, Vec3 point)
        {
            float dist2 = point.DistanceToSquared(center);
            return dist2 <= radius * radius;
        }

        /// <summary>
        /// Determines whether a <see cref="Sphere"/> contains a triangle.
        /// </summary>
        /// <param name="sphere">The sphere to test.</param>
        /// <param name="vertex1">The first vertex of the triangle to test.</param>
        /// <param name="vertex2">The second vertex of the triangle to test.</param>
        /// <param name="vertex3">The third vertex of the triangle to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static EContainment SphereContainsTriangle(Sphere sphere, Vec3 vertex1, Vec3 vertex2, Vec3 vertex3)
        {
            //Source: Jorgy343
            //Reference: None

            bool test1 = SphereContainsPoint(sphere.Center, sphere.Radius, vertex1);
            bool test2 = SphereContainsPoint(sphere.Center, sphere.Radius, vertex2);
            bool test3 = SphereContainsPoint(sphere.Center, sphere.Radius, vertex3);

            if (test1 && test2 && test3)
                return EContainment.Contains;

            if (SphereIntersectsTriangle(sphere.Center, sphere.Radius, vertex1, vertex2, vertex3))
                return EContainment.Intersects;

            return EContainment.Disjoint;
        }
        public static EContainment SphereContainsCapsule(Vec3 sphereCenter, float sphereRadius, Vec3 capsuleCenter, float capsuleHalfHeight, float capsuleRadius)
        {
            throw new NotImplementedException();
        }

        public static EContainment SphereContainsBox(
            Vec3 sphereCenter,
            float sphereRadius,
            Vec3 boxHalfExtents,
            Matrix boxInverseTransform)
        {
            if (!BoxIntersectsSphere(boxHalfExtents, boxInverseTransform, sphereCenter, sphereRadius))
                return EContainment.Disjoint;

            sphereCenter = Vec3.TransformPosition(sphereCenter, boxInverseTransform);

            float r2 = sphereRadius * sphereRadius;
            Vec3[] points = BoundingBox.GetCorners(boxHalfExtents, Matrix.Identity);
            foreach (Vec3 point in points)
                if (sphereCenter.DistanceToSquared(point) > r2)
                    return EContainment.Intersects;

            return EContainment.Contains;
        }

        /// <summary>
        /// Determines whether a <see cref="Sphere"/> contains a <see cref="Sphere"/>.
        /// </summary>
        /// <param name="sphere1">The first sphere to test.</param>
        /// <param name="sphere2">The second sphere to test.</param>
        /// <returns>The type of containment the two objects have.</returns>
        public static EContainment SphereContainsSphere(Vec3 sphere1Center, float sphere1Radius, Vec3 sphere2Center, float sphere2Radius)
        {
            float distance = sphere1Center.DistanceToSquared(sphere2Center);

            float value = sphere1Radius + sphere2Radius;
            if (value * value < distance)
                return EContainment.Disjoint;

            value = sphere1Radius - sphere2Radius;
            if (value * value < distance)
                return EContainment.Intersects;

            return EContainment.Contains;
        }
        public static EContainment FrustumContainsSphere(IFrustum frustum, Vec3 center, float radius)
        {
            //if (frustum.UseBoundingSphere)
            //{
            //    EContainment c = SphereContainsSphere(frustum.BoundingSphere.Center, frustum.BoundingSphere.Radius, center, radius);
            //    if (c == EContainment.Disjoint)
            //        return EContainment.Disjoint;
            //    //If the bounding sphere intersects, could intersect the frustum or be disjoint with the frustum, so more checks needed
            //}

            float distance;
            EContainment type = EContainment.Contains;
            foreach (Plane p in frustum)
            {
                distance = DistancePlanePoint(p, center);
                if (distance < -radius)
                    return EContainment.Disjoint;
                else if (distance < radius)
                    type = EContainment.Intersects;
            }
            return type;
        }
        public static bool FrustumContainsPoint(IFrustum frustum, Vec3 point)
        {
            //if (frustum.UseBoundingSphere)
            //{
            //    EContainment c = SphereContainsSphere(frustum.BoundingSphere.Center, frustum.BoundingSphere.Radius, center, radius);
            //    if (c == EContainment.Disjoint)
            //        return EContainment.Disjoint;
            //    //If the bounding sphere intersects, could intersect the frustum or be disjoint with the frustum, so more checks needed
            //}

            foreach (Plane p in frustum)
                if (DistancePlanePoint(p, point) < 0)
                    return false;
            return true;
        }
        public static EContainment FrustumContainsBox1(IFrustum frustum, Vec3 boxHalfExtents, Matrix boxTransform)
        {
            //if (frustum.UseBoundingSphere)
            //{
            //    EContainment c = SphereContainsBox(frustum.BoundingSphere.Center, frustum.BoundingSphere.Radius, boxHalfExtents, boxTransform);
            //    if (c == EContainment.Disjoint)
            //        return EContainment.Disjoint;
            //    //If the bounding sphere intersects, could intersect the frustum or be disjoint with the frustum, so more checks needed
            //}

            EContainment result = EContainment.Contains;
            int numOut, numIn;
            Vec3[] corners = BoundingBox.GetCorners(boxHalfExtents, boxTransform);
            foreach (Plane p in frustum)
            {
                numOut = 0;
                numIn = 0;
                for (int i = 0; i < 8 && (numIn == 0 || numOut == 0); i++)
                    if (DistancePlanePoint(p, corners[i]) < 0)
                        numOut++;
                    else
                        numIn++;
                if (numIn == 0)
                    return EContainment.Disjoint;
                else if (numOut != 0)
                    result = EContainment.Intersects;
            }
            return result;
        }
        public static EContainment AABBContainsFrustum(Vec3 boxMin, Vec3 boxMax, IFrustum frustum)
        {
            //if (frustum.UseBoundingSphere)
            //{
            //    EContainment c = AABBContainsSphere(boxMin, boxMax, frustum.BoundingSphere.Center, frustum.BoundingSphere.Radius);
            //    if (c == EContainment.Disjoint)
            //        return EContainment.Disjoint;
            //    //If the bounding sphere intersects, could intersect the frustum or be disjoint with the frustum, so more checks needed
            //}

            int numIn = 0, numOut = 0;
            foreach (Vec3 v in frustum.Points)
                if (AABBContainsPoint(boxMin, boxMax, v))
                    ++numIn;
                else
                    ++numOut;
            return numOut == 0 ? EContainment.Contains : numIn == 0 ? EContainment.Disjoint : EContainment.Intersects;
        }
        public static EContainment FrustumContainsAABB(IFrustum frustum, Vec3 boxMin, Vec3 boxMax)
        {
            EContainment c = AABBContainsFrustum(boxMin, boxMax, frustum);
            if (c != EContainment.Disjoint)
                return EContainment.Intersects;
            //else if (c == EContainment.Contains)
            //    return EContainment.ContainedWithin;

            EContainment result = EContainment.Contains;
            int numOut, numIn;
            Vec3[] corners = BoundingBox.GetCorners(boxMin, boxMax);
            foreach (Plane p in frustum)
            {
                numOut = 0;
                numIn = 0;
                for (int i = 0; i < 8 && (numIn == 0 || numOut == 0); i++)
                    if (DistancePlanePoint(p, corners[i]) < 0)
                        numOut++;
                    else
                        numIn++;
                if (numIn == 0)
                    return EContainment.Disjoint;
                else if (numOut != 0)
                    result = EContainment.Intersects;
            }
            return result;
        }
        //public static EContainment FrustumContainsBox2(Frustum frustum, BoundingBox box)
        //{
        //    EPlaneIntersection near = PlaneIntersectsBox(frustum.Near, box);
        //    EPlaneIntersection far = PlaneIntersectsBox(frustum.Far, box);
        //    EPlaneIntersection top = PlaneIntersectsBox(frustum.Top, box);
        //    EPlaneIntersection bottom = PlaneIntersectsBox(frustum.Bottom, box);
        //    EPlaneIntersection left = PlaneIntersectsBox(frustum.Left, box);
        //    EPlaneIntersection right = PlaneIntersectsBox(frustum.Right, box);

        //    if (near == EPlaneIntersection.Back ||
        //        far == EPlaneIntersection.Back ||
        //        top == EPlaneIntersection.Back ||
        //        bottom == EPlaneIntersection.Back ||
        //        left == EPlaneIntersection.Back ||
        //        right == EPlaneIntersection.Back)
        //        return EContainment.Disjoint;

        //    if (near == EPlaneIntersection.Front &&
        //        far == EPlaneIntersection.Front &&
        //        top == EPlaneIntersection.Front &&
        //        bottom == EPlaneIntersection.Front &&
        //        left == EPlaneIntersection.Front &&
        //        right == EPlaneIntersection.Front)
        //        return EContainment.Contains;

        //    return EContainment.Intersects;
        //}
    }
    public enum EContainment
    {
        Contains,
        Disjoint,
        Intersects
    }
    public enum EPlaneIntersection
    {
        Front,
        Back,
        Intersecting
    }
}