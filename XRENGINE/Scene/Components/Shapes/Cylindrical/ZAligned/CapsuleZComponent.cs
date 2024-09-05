using System.Numerics;
using XREngine.Data.Components;
using XREngine.Data.Geometry;
using XREngine.Physics;

namespace XREngine.Components.Scene.Shapes
{
    public class CapsuleZComponent : CommonShape3DComponent<CapsuleZ>
    {
        public CapsuleZComponent()
            : this(Vector3.Zero) { }
        public CapsuleZComponent(float radius, float halfHeight)
            : this(Vector3.Zero, radius, halfHeight) { }
        public CapsuleZComponent(float radius, float halfHeight, RigidBodyConstructionInfo info)
            : this(Vector3.Zero, radius, halfHeight, info) { }
        public CapsuleZComponent(Vector3 center)
            : base(new CapsuleZ(center, 1.0f, 1.0f)) { }
        public CapsuleZComponent(Vector3 center, float radius, float halfHeight)
            : base(new CapsuleZ(center, radius, halfHeight)) { }
        public CapsuleZComponent(Vector3 center, float radius, float halfHeight, RigidBodyConstructionInfo info)
            : base(new CapsuleZ(center, radius, halfHeight), info) { }
        public CapsuleZComponent(CapsuleZ capsule)
            : base(capsule) { }
        public CapsuleZComponent(CapsuleZ capsule, RigidBodyConstructionInfo info)
            : base(capsule, info) { }

        public override XRCollisionShape GetCollisionShape()
            => XRCollisionCapsuleZ.New(Shape.Radius, Shape.HalfHeight);
    }
}