using System.Numerics;
using XREngine.Data.Colors;

namespace XREngine.Physics
{
    public abstract class XRCollisionCapsuleX : XRCollisionShape
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
        /// Creates a new capsule with height aligned to the X axis.
        /// </summary>
        /// <param name="radius">The radius of the upper and lower spheres, and the cylinder.</param>
        /// <param name="height">How tall the capsule is, not including the radius on top and bottom.</param>
        /// <returns>A new capsule with height aligned to the X axis.</returns>
        public static XRCollisionCapsuleX New(float radius, float height)
            => Engine.Physics.NewCapsuleX(radius, height);

        public override void Render(Matrix4x4 worldTransform, ColorF4 color, bool solid)
        {

        }
    }
}
