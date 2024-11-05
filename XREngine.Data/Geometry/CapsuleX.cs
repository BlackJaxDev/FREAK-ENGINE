using System.Numerics;

namespace XREngine.Data.Geometry
{
    public struct CapsuleX : IShape
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

        public CapsuleX() { }
        public CapsuleX(Vector3 center, float radius, float halfHeight)
        {
            Center = center;
            Radius = radius;
            HalfHeight = halfHeight;
        }

        public EContainment Contains(AABB box)
        {
            throw new NotImplementedException();
        }

        public EContainment Contains(Sphere sphere)
        {
            throw new NotImplementedException();
        }

        public EContainment Contains(Cone cone)
        {
            throw new NotImplementedException();
        }

        public EContainment Contains(Capsule shape)
        {
            throw new NotImplementedException();
        }

        public Vector3 ClosestPoint(Vector3 point, bool clampToEdge)
        {
            throw new NotImplementedException();
        }

        public bool Contains(Vector3 point)
        {
            throw new NotImplementedException();
        }

        public AABB GetAABB()
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
    }
}
