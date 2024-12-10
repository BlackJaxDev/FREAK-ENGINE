using System.Numerics;
using XREngine.Rendering;

namespace XREngine.Physics.RayTracing
{
    /// <summary>
    /// Returns the first intersected object that specifies collision with this ray.
    /// </summary>
    public class RayTraceClosest(Vector3 start, Vector3 end, ushort collisionGroupFlags, ushort collidesWithFlags, params XRCollisionObject[] ignored) : RayTraceSingle(start, end, collisionGroupFlags, collidesWithFlags, ignored)
    {
        protected internal override void AddResult(XRCollisionObject obj, Vector3 hitNormal, bool normalInWorldSpace, float hitFraction, int shapePart, int triangleIndex)
        {
            if (!CanAddCommon(obj))
                return;

            Vector3 hitPointWorld = Vector3.Lerp(StartPointWorld, EndPointWorld, hitFraction);
            if (Result is null)
                Result = new RayCollisionResult(obj, hitFraction, hitNormal, hitPointWorld, shapePart, triangleIndex);
            else if (hitFraction < Result.HitFraction)
            {
                Result.CollisionObject = obj;
                Result.HitFraction = hitFraction;
                Result.HitNormalWorld = hitNormal;
                Result.HitPointWorld = hitPointWorld;
                Result.ShapePart = shapePart;
                Result.TriangleIndex = triangleIndex;
            }
        }

        internal bool Trace(XRWorldInstance? world)
        {
            throw new NotImplementedException();
        }
    }
}
