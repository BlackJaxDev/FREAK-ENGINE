using System.Numerics;
using XREngine.Data.Components;
using XREngine.Data.Geometry;
using XREngine.Physics;

namespace XREngine.Components.Scene.Shapes
{
    public class ConeZComponent : CommonShape3DComponent<ConeZ>
    {
        public ConeZComponent()
            : this(1.0f, 1.0f) { }
        public ConeZComponent(float radius, float height)
            : this(new ConeZ(Vector3.Zero, height, radius)) { }
        public ConeZComponent(float radius, float height, RigidBodyConstructionInfo info)
            : this(new ConeZ(Vector3.Zero, height, radius), info) { }
        public ConeZComponent(Vector3 center)
            : base(new ConeZ(center, 1.0f, 1.0f)) { }
        public ConeZComponent(Vector3 center, float radius, float halfHeight)
            : base(new ConeZ(center, radius, halfHeight)) { }
        public ConeZComponent(Vector3 center, float radius, float halfHeight, RigidBodyConstructionInfo info)
            : base(new ConeZ(center, radius, halfHeight), info) { }
        public ConeZComponent(ConeZ cone)
            : base(cone) { }
        public ConeZComponent(ConeZ cone, RigidBodyConstructionInfo info)
            : base(cone, info) { }

        public override XRCollisionShape GetCollisionShape()
            => XRCollisionConeZ.New(_shape.Height, _shape.Radius);
    }
}
