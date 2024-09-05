using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Physics;

namespace XREngine.Data.Components
{
    public class CapsuleYComponent : CommonShape3DComponent<CapsuleY>
    {
        public CapsuleYComponent()
            : this(Vector3.Zero) { }
        public CapsuleYComponent(float radius, float halfHeight)
            : this(Vector3.Zero, radius, halfHeight) { }
        public CapsuleYComponent(float radius, float halfHeight, RigidBodyConstructionInfo info)
            : this(Vector3.Zero, radius, halfHeight, info) { }
        public CapsuleYComponent(Vector3 center)
            : this(new CapsuleY(center, 1.0f, 1.0f)) { }
        public CapsuleYComponent(Vector3 center, float radius, float halfHeight)
            : this(new CapsuleY(center, radius, halfHeight)) { }
        public CapsuleYComponent(Vector3 center, float radius, float halfHeight, RigidBodyConstructionInfo info)
            : this(new CapsuleY(center, radius, halfHeight), info) { }
        public CapsuleYComponent(CapsuleY capsule)
            : base(capsule) { }
        public CapsuleYComponent(CapsuleY capsule, RigidBodyConstructionInfo info)
            : base(capsule, info) { }

        public override XRCollisionShape GetCollisionShape()
            => Engine.Physics.NewCapsuleY(Shape.Radius, Shape.HalfHeight);
    }
}