using System.Numerics;

namespace XREngine.Data.Geometry
{
    public struct CapsuleY : IShape
    {
        private Vector3 _center;
        private float _radius;
        private float _halfHeight;

        public Vector3 Center
        {
            readonly get => _center;
            set => _center = value;
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

        public CapsuleY() { }
        public CapsuleY(Vector3 center, float radius, float halfHeight)
        {
            Center = center;
            Radius = radius;
            HalfHeight = halfHeight;
        }

        public EContainment ContainsAABB(AABB box, float tolerance = float.Epsilon)
        {
            throw new NotImplementedException();
        }

        public EContainment ContainsSphere(Sphere sphere)
        {
            throw new NotImplementedException();
        }

        public EContainment ContainsCone(Cone cone)
        {
            throw new NotImplementedException();
        }

        public EContainment ContainsCapsule(Capsule shape)
        {
            throw new NotImplementedException();
        }

        public Vector3 ClosestPoint(Vector3 point, bool clampToEdge)
        {
            throw new NotImplementedException();
        }

        public bool ContainsPoint(Vector3 point, float tolerance = float.Epsilon)
        {
            throw new NotImplementedException();
        }

        public AABB GetAABB(bool transformed)
        {
            throw new NotImplementedException();
        }

        public bool IntersectsSegment(Segment segment, out Vector3[] points)
        {
            throw new NotImplementedException();
        }

        public bool IntersectsSegment(Segment segment)
        {
            throw new NotImplementedException();
        }

        public EContainment ContainsBox(Box box)
        {
            throw new NotImplementedException();
        }
    }
}
