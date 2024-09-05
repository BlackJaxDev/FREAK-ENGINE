using System.Numerics;
using XREngine.Data.Components;
using XREngine.Data.Geometry;
using XREngine.Physics;

namespace XREngine.Components.Scene.Shapes
{
    public class ConeYComponent : CommonShape3DComponent<ConeY>
    {
        public ConeYComponent()
            : this(1.0f, 1.0f) { }
        public ConeYComponent(float radius, float height)
            : this(new ConeY(Vector3.Zero, height, radius)) { }
        public ConeYComponent(float radius, float height, RigidBodyConstructionInfo info)
            : this(new ConeY(Vector3.Zero, height, radius), info) { }
        public ConeYComponent(Vector3 center)
            : base(new ConeY(center, 1.0f, 1.0f)) { }
        public ConeYComponent(Vector3 center, float radius, float halfHeight)
            : base(new ConeY(center, radius, halfHeight)) { }
        public ConeYComponent(Vector3 center, float radius, float halfHeight, RigidBodyConstructionInfo info)
            : base(new ConeY(center, radius, halfHeight), info) { }
        public ConeYComponent(ConeY cone)
            : base(cone) { }
        public ConeYComponent(ConeY cone, RigidBodyConstructionInfo info)
            : base(cone, info) { }

        public override XRCollisionShape GetCollisionShape()
            => XRCollisionConeY.New(Shape.Radius, Shape.Height);
    }
}
