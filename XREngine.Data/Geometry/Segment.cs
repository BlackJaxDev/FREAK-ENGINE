using System.Numerics;

namespace XREngine.Data.Geometry
{
    public struct Segment
    {
        private Vector3 start;
        private Vector3 end;

        public Segment(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
        }

        public Vector3 Start
        {
            readonly get => start;
            set => start = value;
        }

        public Vector3 End
        {
            readonly get => end;
            set => end = value;
        }

        public float Length
        {
            readonly get => (end - start).Length();
            set => end = start + Vector3.Normalize(end - start) * value;
        }

        public readonly Vector3 ClosestPoint(Vector3 point)
        {
            Vector3 direction = end - start;
            float t = Vector3.Dot(point - start, direction) / Vector3.Dot(direction, direction);
            return start + t * direction;
        }

        public readonly float DistanceToPoint(Vector3 point)
            => Vector3.Cross(end - start, (point - start)).Length() / (end - start).Length();

        public readonly float DistanceToSegment(Segment segment)
            => DistanceToSegment(segment.Start, segment.End);

        public readonly float DistanceToSegment(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            Vector3 diff = start - this.start;
            float a = Vector3.Dot(direction, direction);
            float b = Vector3.Dot(diff, direction);
            float c = Vector3.Dot(diff, diff);
            float d = a * c - b * b;
            if (d > 0.0f)
            {
                float t = Math.Max(0.0f, Math.Min(1.0f, (b / a)));
                Vector3 closest = start + t * direction;
                return (closest - this.start).Length();
            }
            return (start - this.start).Length();
        }

        /// <summary>
        /// Returns a colinear point the given distance from the start point of this segment.
        /// </summary>
        public readonly Vector3 PointAtLineDistance(float distance)
            => PointAtLineDistance(Start, End, distance);
        /// <summary>
        /// Returns a 
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Vector3 PointAtLineDistance(Vector3 startPoint, Vector3 endPoint, float distance)
        {
            Vector3 diff = endPoint - startPoint;
            return startPoint + (diff * (distance / diff.Length()));
        }
    }
}