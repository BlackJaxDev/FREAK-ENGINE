using System.Numerics;
using XREngine.Data.Components;
using XREngine.Data.Geometry;
using XREngine.Physics;

namespace XREngine.Components.Scene.Shapes
{
    public class CapsuleXComponent : CommonShape3DComponent<CapsuleX>
    {
        public CapsuleXComponent()
            : this(Vector3.Zero) { }
        public CapsuleXComponent(float radius, float halfHeight)
            : this(Vector3.Zero, radius, halfHeight) { }
        public CapsuleXComponent(float radius, float halfHeight, RigidBodyConstructionInfo info)
            : this(Vector3.Zero, radius, halfHeight, info) { }
        public CapsuleXComponent(Vector3 center)
            : base(new CapsuleX(center, 1.0f, 1.0f)) { }
        public CapsuleXComponent(Vector3 center, float radius, float halfHeight)
            : base(new CapsuleX(center, radius, halfHeight)) { }
        public CapsuleXComponent(Vector3 center, float radius, float halfHeight, RigidBodyConstructionInfo info)
            : base(new CapsuleX(center, radius, halfHeight), info) { }
        public CapsuleXComponent(CapsuleX capsule)
            : base(capsule) { }
        public CapsuleXComponent(CapsuleX capsule, RigidBodyConstructionInfo info)
            : base(capsule, info) { }

        public override XRCollisionShape GetCollisionShape()
            => XRCollisionCapsuleX.New(Shape.Radius, Shape.HalfHeight);
    }
}