using System.Numerics;
using XREngine.Data.Components;
using XREngine.Data.Geometry;
using XREngine.Physics;

namespace XREngine.Components.Scene.Shapes
{
    public class SphereComponent(float radius, RigidBodyConstructionInfo? info) : CommonShape3DComponent<Sphere>(new Sphere(Vector3.Zero, radius), info)
    {
        public SphereComponent()
            : this(1.0f) { }

        public SphereComponent(float radius) 
            : this(radius, null) { }

        public override XRCollisionShape GetCollisionShape()
            => XRCollisionSphere.New(Shape.Radius);
    }
}
