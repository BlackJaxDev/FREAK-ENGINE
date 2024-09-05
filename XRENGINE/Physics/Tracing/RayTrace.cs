using System.Drawing;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Geometry;

namespace XREngine.Physics.RayTracing
{
    /// <summary>
    /// Contains properties and methods for projecting a ray in the world and testing for intersections with collision objects.
    /// </summary>
    public abstract class RayTrace(
        Vector3 startPointWorld,
        Vector3 endPointWorld,
        ushort collisionGroup,
        ushort collidesWith,
        params XRCollisionObject[] ignored) : XRBase
    {
        public Vector3 StartPointWorld { get; set; } = startPointWorld;
        public Vector3 EndPointWorld { get; set; } = endPointWorld;
        public ushort CollisionGroup { get; set; } = collisionGroup;
        public ushort CollidesWith { get; set; } = collidesWith;
        public XRCollisionObject[] Ignored { get; set; } = ignored;
        public abstract bool HasHit { get; }
        public bool DebugDraw { get; set; } = true;
        public ColorF4 DebugColor { get; set; } = Color.Magenta;

        protected bool CanAddCommon(XRCollisionObject obj)
            => obj.HasContactResponse && !Ignored.Any(x => x == obj);

        internal protected abstract void AddResult(XRCollisionObject obj, Vector3 hitNormal, bool normalInWorldSpace, float hitFraction, int shapePart, int triangleIndex);
        internal protected virtual bool TestApproxCollision(int uniqueID, ushort collisionGroup, ushort collidesWith, Vector3 aabbMin, Vector3 aabbMax, object clientObject)
        {
            //if (Ignored.Any(x => x.UniqueID == uniqueID))
            //    return false;

            Vector3 dir = EndPointWorld - StartPointWorld;

            //I believe this algorithm is faster.
            if (GeoUtil.RayIntersectsAABBDistance(StartPointWorld, dir, aabbMin, aabbMax, out float distance) && distance * distance < dir.LengthSquared())
            //if (Collision.SegmentIntersectsAABB(Start, End, aabbMin, aabbMax, out Vector3 enterPoint, out Vector3 exitPoint))
            {
                bool rayIntersectsOther = (CollisionGroup & collidesWith) == CollisionGroup;
                bool otherIntersectsRay = (collisionGroup & CollidesWith) == collisionGroup;
                if (rayIntersectsOther && otherIntersectsRay)
                    return true;
            }
            return false;
        }

        internal abstract void Reset();
        public abstract void Render();
    }
}
