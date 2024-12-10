using System.Numerics;
using XREngine.Data.Colors;

namespace XREngine.Physics
{
    public abstract class XRCollisionSphere : XRCollisionShape
    {
        public abstract float Radius { get; set; }

        public static XRCollisionSphere New(float radius)
            => Engine.Physics.NewSphere(radius);

        public override void Render(Matrix4x4 worldTransform, ColorF4 color, bool solid)
        {
            //TODO: callback to the API wrapper
            //Api.RenderSphere(worldTransform.Translation, Radius, solid, color);
        }
    }
}
