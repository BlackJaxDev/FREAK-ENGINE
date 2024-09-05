using System.Numerics;
using XREngine.Data.Core;

namespace XREngine.Data.Geometry
{
    public struct Ray(Vector3 startPoint, Vector3 direction)
    {
        public Vector3 StartPoint
        {
            readonly get => startPoint;
            set => startPoint = value;
        }
        public Vector3 Direction
        {
            readonly get => _direction;
            set => _direction = value;
        }

        private Vector3 _direction = Vector3.Normalize(direction);

        public readonly Ray TransformedBy(Matrix4x4 transform)
        {
            Vector3 newStart = Vector3.Transform(StartPoint, transform);
            Vector3 newEnd = Vector3.Transform(StartPoint + Direction, transform);
            return new Ray(newStart, newEnd - newStart);
        }

        public readonly float DistanceToPoint(Vector3 point)
            => Vector3.Cross(Direction, (point - StartPoint)).Length();

        public static Vector3 GetClosestColinearPoint(Vector3 startPoint, Vector3 direction, Vector3 point)
        {
            direction = Vector3.Normalize(direction);
            return startPoint + Vector3.Dot(point - startPoint, direction) * direction;
        }

        /// <summary>
        /// Returns a vector that starts at the given point and perpendicularly intersects with this ray.
        /// </summary>
        public readonly Vector3 PerpendicularVectorFromPoint(Vector3 point)
            => GetPerpendicularVectorFromPoint(StartPoint, Direction, point);
        /// <summary>
        /// Returns a vector that starts at the given point and perpendicularly intersects with a ray formed by the given start and end points.
        /// </summary>
        public static Vector3 GetPerpendicularVectorFromPoint(Vector3 startPoint, Vector3 direction, Vector3 point)
            => GetClosestColinearPoint(startPoint, direction, point) - point;

        public readonly Segment PerpendicularSegmentFromPoint(Vector3 point)
            => new(point, point + PerpendicularVectorFromPoint(point));

        public readonly bool LineSphereIntersect(Vector3 center, float radius, out Vector3 result)
        {
            Vector3 diff = Direction;
            float a = diff.LengthSquared();
            if (a > 0.0f)
            {
                float b = 2.0f * Vector3.Dot(diff, StartPoint - center);
                float c = center.LengthSquared() + StartPoint.LengthSquared() - 2.0f * Vector3.Dot(center, StartPoint) - radius * radius;

                if (XRMath.QuadraticRealRoots(a, b, c, out float val1, out float val2))
                {
                    if (val2 < val1)
                        val1 = val2;

                    result = StartPoint + (diff * val1);
                    return true;
                }
            }

            result = new Vector3();
            return false;
        }

        public readonly bool LinePlaneIntersect(Plane p, out Vector3 result)
            => LinePlaneIntersect(XRMath.GetPlanePoint(p), p.Normal, out result);

        public readonly bool LinePlaneIntersect(Vector3 point, Vector3 normal, out Vector3 result)
        {
            Vector3 diff = Direction;
            float scale = -Vector3.Dot(normal, StartPoint - point) / Vector3.Dot(normal, diff);

            if (float.IsNaN(scale) || scale < 0.0f || scale > 1.0f)
            {
                result = new Vector3();
                return false;
            }
            result = StartPoint + (diff * scale);
            return true;
        }

        public readonly Vector3 PointAtNormalizedLineDistance(float distance)
            => StartPoint + Direction * distance;

        public readonly Vector3 PointAtLineDistance(float distance)
        {
            Vector3 diff = Direction;
            return StartPoint + (diff * (distance / diff.Length()));
        }

        public readonly Vector3 PointLineIntersect(Vector3 point)
        {
            Vector3 diff = Direction;
            return StartPoint + (diff * (Vector3.Dot(diff, point - StartPoint) / diff.LengthSquared()));
        }

        public static Vector3 PointAtLineDistance(Vector3 start, Vector3 end, float distance)
        {
            Vector3 diff = end - start;
            return start + (diff * (distance / diff.Length()));
        }

        public static Vector3 PointAtNormalizedLineDistance(Vector3 start, Vector3 end, float time)
        {
            Vector3 diff = end - start;
            return start + diff * time;
        }
    }
}
