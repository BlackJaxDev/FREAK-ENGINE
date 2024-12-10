using System.Numerics;
using XREngine.Data.Colors;

namespace XREngine.Physics
{
    public abstract class XRCollisionConvexHull : XRCollisionShape
    {
        public static XRCollisionConvexHull New(IEnumerable<Vector3> points)
            => Engine.Physics.NewConvexHull(points);

        public override void Render(Matrix4x4 worldTransform, ColorF4 color, bool solid)
        {

        }
    }
}
