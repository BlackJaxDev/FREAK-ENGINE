using System.Numerics;
using XREngine.Data.Colors;

namespace XREngine.Physics
{
    public abstract class XRCollisionBox : XRCollisionShape
    {
        public abstract Vector3 HalfExtents { get; }

        public static XRCollisionBox New(float halfExtentsX, float halfExtentsY, float halfExtentsZ)
            => New(new Vector3(halfExtentsX, halfExtentsY, halfExtentsZ));
        public static XRCollisionBox New(Vector3 halfExtents)
            => Engine.Physics.NewBox(halfExtents);

        public override void Render(Matrix4x4 worldTransform, ColorF4 color, bool solid)
        {
            //Api.RenderBox(HalfExtents, worldTransform, solid, color);
        }
    }
}
