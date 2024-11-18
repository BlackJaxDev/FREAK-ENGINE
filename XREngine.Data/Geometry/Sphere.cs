using System.Numerics;

namespace XREngine.Data.Geometry
{
    public struct Sphere(Vector3 center, float radius) : IShape
    {
        public Vector3 Center = center;
        public float Radius = radius;

        public float Diameter
        {
            readonly get => Radius * 2.0f;
            set => Radius = value / 2.0f;
        }

        public readonly Vector3 ClosestPoint(Vector3 point, bool clampToEdge)
        {
            Vector3 vec = point - Center;
            float length = vec.Length();
            return Center + vec / length * Radius;
        }

        public readonly bool ContainedWithin(AABB boundingBox)
        {
            Vector3 min = boundingBox.Min;
            Vector3 max = boundingBox.Max;
            Vector3 closestPoint = ClosestPoint(min, false);
            if (closestPoint.X < min.X || closestPoint.X > max.X)
                return false;
            if (closestPoint.Y < min.Y || closestPoint.Y > max.Y)
                return false;
            if (closestPoint.Z < min.Z || closestPoint.Z > max.Z)
                return false;
            return true;
        }

        public readonly EContainment ContainsAABB(AABB box, float tolerance = float.Epsilon)
        {
            Vector3 min = box.Min;
            Vector3 max = box.Max;
            Vector3 closestPoint = ClosestPoint(min, false);
            if (closestPoint.X < min.X || closestPoint.X > max.X)
                return EContainment.Disjoint;
            if (closestPoint.Y < min.Y || closestPoint.Y > max.Y)
                return EContainment.Disjoint;
            if (closestPoint.Z < min.Z || closestPoint.Z > max.Z)
                return EContainment.Disjoint;
            return EContainment.Contains;
        }

        public readonly EContainment ContainsSphere(Sphere sphere)
        {
            float distance = Vector3.Distance(Center, sphere.Center);
            if (distance > Radius + sphere.Radius)
                return EContainment.Disjoint;
            if (distance + sphere.Radius < Radius)
                return EContainment.Contains;
            return EContainment.Intersects;
        }

        public EContainment ContainsCone(Cone cone)
        {
            throw new NotImplementedException();
        }

        public EContainment ContainsCapsule(Capsule shape)
        {
            throw new NotImplementedException();
        }

        public bool ContainsPoint(Vector3 point, float tolerance = float.Epsilon)
        {
            throw new NotImplementedException();
        }

        public readonly AABB GetAABB(bool transformed) 
            => new(Center - new Vector3(Radius), Center + new Vector3(Radius));

        public bool IntersectsSegment(Segment segment, out Vector3[] points)
        {
            throw new NotImplementedException();
        }

        public bool IntersectsSegment(Segment segment)
        {
            throw new NotImplementedException();
        }

        public override readonly string ToString()
            => $"Sphere (Center: {Center}, Radius: {Radius})";

        public EContainment ContainsBox(Box box)
        {
            throw new NotImplementedException();
        }
    }
}
