using System.Numerics;

namespace XREngine.Data.Geometry
{
    public struct Capsule : IShape
    {
        private Vector3 _center;
        private Vector3 _upAxis;
        private float _radius;
        private float _halfHeight;

        public Vector3 Center
        {
            readonly get => _center;
            set => _center = value;
        }
        public Vector3 UpAxis
        {
            readonly get => _upAxis;
            set => _upAxis = Vector3.Normalize(value);
        }
        public float Radius
        {
            readonly get => _radius;
            set => _radius = value;
        }
        public float HalfHeight
        {
            readonly get => _halfHeight;
            set => _halfHeight = value;
        }

        public Capsule(Vector3 upAxis, float radius, float halfHeight)
        {
            UpAxis = upAxis;
            Radius = radius;
            HalfHeight = halfHeight;
        }
        public Capsule(Vector3 center, Vector3 upAxis, float radius, float halfHeight)
        {
            Center = center;
            UpAxis = upAxis;
            Radius = radius;
            HalfHeight = halfHeight;
        }

        public readonly Sphere GetTopSphere()
            => new(GetTopCenterPoint(), Radius);
        public readonly Sphere GetBottomSphere()
            => new(GetBottomCenterPoint(), Radius);

        public readonly Vector3 GetTopCenterPoint()
            => Center + UpAxis * HalfHeight;
        public readonly Vector3 GetBottomCenterPoint()
            => Center - UpAxis * HalfHeight;

        /// <summary>
        /// Returns the closest point on this shape to the given point.
        /// </summary>
        /// <param name="point">The point determine closeness with.</param>
        public readonly Vector3 ClosestPoint(Vector3 point)
            => ClosestPoint(point, false);

        /// <summary>
        /// Returns the closest point on this shape to the given point.
        /// </summary>
        /// <param name="point">The point determine closeness with.</param>
        /// <param name="clampIfInside">If true, finds closest edge point even if the given point is inside the capsule. Otherwise, just returns the given point if it is inside.</param>
        public readonly Vector3 ClosestPoint(Vector3 point, bool clampIfInside)
        {
            Vector3 colinearPoint = GeoUtil.SegmentClosestColinearPointToPoint(GetBottomCenterPoint(), GetTopCenterPoint(), point);
            if (!clampIfInside && Vector3.Distance(colinearPoint, point) < _radius)
                return point;
            return Ray.PointAtLineDistance(colinearPoint, point, _radius);
        }

        public readonly AABB GetAABB()
        {
            Vector3 top = GetTopCenterPoint();
            Vector3 bot = GetBottomCenterPoint();
            float radius = Radius;
            return new(
                Vector3.Min(top, bot) - new Vector3(radius),
                Vector3.Max(top, bot) + new Vector3(radius));
        }

        #region Containment

        public readonly bool Contains(Vector3 point)
            => GeoUtil.SegmentShortestDistanceToPoint(GetBottomCenterPoint(), GetTopCenterPoint(), point) <= _radius;

        public enum ESegmentPart
        {
            Start,
            End,
            Middle,
        }

        public readonly Vector3 ClosestPointTo(Vector3 point)
        {
            Vector3 startPoint = GetBottomCenterPoint();
            Vector3 endPoint = GetTopCenterPoint();
            ESegmentPart part = GeoUtil.GetDistancePointToSegmentPart(startPoint, endPoint, point, out _);
            return part switch
            {
                ESegmentPart.Start => Ray.PointAtLineDistance(startPoint, point, _radius),
                ESegmentPart.End => Ray.PointAtLineDistance(endPoint, point, _radius),
                ESegmentPart.Middle => Ray.GetPerpendicularVectorFromPoint(startPoint, endPoint - startPoint, point),
                _ => throw new Exception(),
            };
        }

        public readonly Vector3 ClosestPointTo(Sphere sphere)
            => ClosestPointTo(sphere.Center);

        public readonly EContainment Contains(Sphere sphere)
        {
            Vector3 startPoint = GetBottomCenterPoint();
            Vector3 endPoint = GetTopCenterPoint();
            float pointToSegment = GeoUtil.SegmentShortestDistanceToPoint(startPoint, endPoint, sphere.Center);
            float maxDist = sphere.Radius + Radius;
            if (pointToSegment > maxDist)
                return EContainment.Disjoint;
            else if (pointToSegment + sphere.Radius < Radius)
                return EContainment.Contains;
            else
                return EContainment.Intersects;
        }

        public readonly EContainment Contains(Capsule capsule)
        {
            //TODO
            return EContainment.Contains;
        }

        public readonly EContainment Contains(Cone cone)
        {
            //TODO
            return EContainment.Contains;
        }

        public readonly EContainment Contains(Box box)
        {
            //TODO
            return EContainment.Contains;
        }

        public readonly EContainment Contains(AABB box)
        {
            //TODO
            return EContainment.Contains;
        }

        public bool ContainedWithin(AABB boundingBox)
        {
            throw new NotImplementedException();
        }

        public bool Intersects(Segment segment, out Vector3[] points)
        {
            throw new NotImplementedException();
        }

        public bool Intersects(Segment segment)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
