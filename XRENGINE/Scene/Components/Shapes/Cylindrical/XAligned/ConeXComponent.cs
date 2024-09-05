using System.Numerics;
using XREngine.Data.Components;
using XREngine.Data.Geometry;
using XREngine.Physics;

namespace XREngine.Components.Scene.Shapes
{
    public class ConeXComponent : CommonShape3DComponent<ConeX>
    {
        public ConeXComponent()
            : this(1.0f, 1.0f) { }
        public ConeXComponent(float radius, float height)
            : this(new ConeX(Vector3.Zero, height, radius)) { }
        public ConeXComponent(float radius, float height, RigidBodyConstructionInfo info)
            : this(new ConeX(Vector3.Zero, height, radius), info) { }
        public ConeXComponent(Vector3 center)
            : base(new ConeX(center, 1.0f, 1.0f)) { }
        public ConeXComponent(Vector3 center, float radius, float halfHeight)
            : base(new ConeX(center, radius, halfHeight)) { }
        public ConeXComponent(Vector3 center, float radius, float halfHeight, RigidBodyConstructionInfo info)
            : base(new ConeX(center, radius, halfHeight), info) { }
        public ConeXComponent(ConeX cone)
            : base(cone) { }
        public ConeXComponent(ConeX cone, RigidBodyConstructionInfo info)
            : base(cone, info) { }

        public override XRCollisionShape GetCollisionShape()
            => XRCollisionConeX.New(Shape.Radius, Shape.Height);
    }
}
