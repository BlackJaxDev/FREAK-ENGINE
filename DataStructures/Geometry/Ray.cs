using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Data.Geometry
{
    public struct Ray
    {
        public Ray(Vec3 startPoint, Vec3 direction, ENormalizeOption normalization = ENormalizeOption.FastSafe)
        {
            _startPoint = startPoint;
            _direction = direction.Normalized(normalization);
        }
        public Vec3 StartPoint
        {
            get => _startPoint;
            set => _startPoint = value;
        }
        public Vec3 Direction
        {
            get => _direction;
            set => _direction = value;
        }

        private Vec3 _startPoint;
        private Vec3 _direction;

        public Ray TransformedBy(Matrix transform)
        {
            Vec3 newStart = StartPoint * transform;
            Vec3 newEnd = (StartPoint + Direction) * transform;
            return new Ray(newStart, newEnd - newStart);
        }

        public float DistanceToPoint(Vec3 point)
            => (Direction ^ (point - StartPoint)).Length;

        //public float DistanceToRay(Ray ray)
        //{
        //    throw new NotImplementedException();
        //}
        //public float DistanceToSegment(Segment segment)
        //{
        //    throw new NotImplementedException();
        //}

        public static Vec3 GetClosestColinearPoint(Vec3 startPoint, Vec3 direction, Vec3 point)
        {
            direction.Normalize();
            return startPoint + ((point - startPoint).Dot(direction)) * direction;
        }

        /// <summary>
        /// Returns a vector that starts at the given point and perpendicularly intersects with this ray.
        /// </summary>
        public Vec3 PerpendicularVectorFromPoint(Vec3 point)
            => GetPerpendicularVectorFromPoint(StartPoint, Direction, point);
        /// <summary>
        /// Returns a vector that starts at the given point and perpendicularly intersects with a ray formed by the given start and end points.
        /// </summary>
        public static Vec3 GetPerpendicularVectorFromPoint(Vec3 startPoint, Vec3 direction, Vec3 point)
            => GetClosestColinearPoint(startPoint, direction, point) - point;

        public Segment PerpendicularSegmentFromPoint(Vec3 point)
            => new Segment(point, point + PerpendicularVectorFromPoint(point));

        public bool LineSphereIntersect(Vec3 center, float radius, out Vec3 result)
        {
            Vec3 diff = Direction;
            float a = diff.LengthSquared;
            if (a > 0.0f)
            {
                float b = 2.0f * diff.Dot(StartPoint - center);
                float c = center.LengthSquared + StartPoint.LengthSquared - 2.0f * center.Dot(StartPoint) - radius * radius;

                if (XMath.QuadraticRealRoots(a, b, c, out float val1, out float val2))
                {
                    if (val2 < val1)
                        val1 = val2;

                    result = StartPoint + (diff * val1);
                    return true;
                }
            }

            result = new Vec3();
            return false;
        }
        public bool LinePlaneIntersect(Plane p, out Vec3 result)
            => LinePlaneIntersect(p.IntersectionPoint, p.Normal, out result);

        public bool LinePlaneIntersect(Vec3 point, Vec3 normal, out Vec3 result)
        {
            Vec3 diff = Direction;
            float scale = -normal.Dot(StartPoint - point) / normal.Dot(diff);

            if (float.IsNaN(scale) || scale < 0.0f || scale > 1.0f)
            {
                result = new Vec3();
                return false;
            }
            result = StartPoint + (diff * scale);
            return true;
        }
        public Vec3 PointAtNormalizedLineDistance(float distance)
            => StartPoint + Direction * distance;
        public Vec3 PointAtLineDistance(float distance)
        {
            Vec3 diff = Direction;
            return StartPoint + (diff * (distance / diff.Length));
        }
        public Vec3 PointLineIntersect(Vec3 point)
        {
            Vec3 diff = Direction;
            return StartPoint + (diff * (diff.Dot(point - StartPoint) / diff.LengthSquared));
        }
        public static Vec3 PointAtLineDistance(Vec3 start, Vec3 end, float distance)
        {
            Vec3 diff = end - start;
            return start + (diff * (distance / diff.Length));
        }
        public static Vec3 PointAtLerpTime(Vec3 start, Vec3 end, float time)
        {
            Vec3 diff = end - start;
            return start + diff * time;
        }
    }
}
