using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;

namespace XREngine.Physics
{
    public abstract class XRCollisionShape : IDisposable
    {
        public abstract float Margin { get; set; }
        public abstract Vector3 LocalScaling { get; set; }
        public bool DebugRender { get; set; }

        public Sphere GetBoundingSphere()
        {
            GetBoundingSphere(out Vector3 center, out float radius);
            return new Sphere(center, radius);
        }
        public abstract void GetBoundingSphere(out Vector3 center, out float radius);
        public AABB GetAabb(Matrix4x4 transform)
        {
            GetAabb(transform, out Vector3 aabbMin, out Vector3 aabbMax);
            return new AABB(aabbMin, aabbMax);
        }
        public abstract void GetAabb(Matrix4x4 transform, out Vector3 aabbMin, out Vector3 aabbMax);
        public abstract Vector3 CalculateLocalInertia(float mass);

        public abstract void Dispose();

        public abstract void Render(Matrix4x4 worldTransform, ColorF4 color, bool solid);
    }
}
