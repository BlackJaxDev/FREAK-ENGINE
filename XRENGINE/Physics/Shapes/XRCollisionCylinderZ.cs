using System.Numerics;
using XREngine.Data.Colors;

namespace XREngine.Physics
{
    public abstract class XRCollisionCylinderZ : XRCollisionShape
    {
        public abstract float Radius { get; }
        public abstract float Height { get; }
        
        public static XRCollisionCylinderZ New(float radius, float height)
            => Engine.Physics.NewCylinderZ(radius, height);

        public override void Render(Matrix4x4 worldTransform, ColorF4 color, bool solid)
        {

        }
    }
}
