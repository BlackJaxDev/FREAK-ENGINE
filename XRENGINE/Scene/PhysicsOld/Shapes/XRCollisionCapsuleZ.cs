using System.Numerics;
using XREngine.Data.Colors;

namespace XREngine.Physics
{
    public abstract class XRCollisionCapsuleZ : XRCollisionShape
    {
        /// <summary>
        /// The radius of the upper and lower spheres, and the cylinder.
        /// </summary>
        public abstract float Radius { get; }
        /// <summary>
        /// How tall the capsule is, not including the radius on top and bottom.
        /// </summary>
        public abstract float Height { get; }

        /// <summary>
        /// Creates a new capsule with height aligned to the Z axis.
        /// </summary>
        /// <param name="radius">The radius of the upper and lower spheres, and the cylinder.</param>
        /// <param name="height">How tall the capsule is, not including the radius on top and bottom.</param>
        /// <returns>A new capsule with height aligned to the Z axis.</returns>
        public static XRCollisionCapsuleZ New(float radius, float height)
            => Engine.Physics.NewCapsuleZ(radius, height);

        public override void Render(Matrix4x4 worldTransform, ColorF4 color, bool solid)
        {

        }
    }
}
