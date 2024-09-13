using System.Numerics;

namespace XREngine.Physics.ShapeTracing
{
    public class ShapeTraceClosest(
        XRCollisionShape? shape,
        Matrix4x4 start,
        Matrix4x4 end,
        ushort collisionGroup,
        ushort collidesWith,
        params XRCollisionObject[] ignored) : ShapeTraceSingle(shape, start, end, collisionGroup, collidesWith, ignored)
    {
        internal protected override void AddResult(
            XRCollisionObject obj,
            Vector3 hitNormal,
            bool normalInWorldSpace,
            Vector3 hitPointWorld,
            float hitFraction,
            int shapePart,
            int triangleIndex)
        {
            if (!CanAddCommon(obj))
                return;

            if (!normalInWorldSpace)
                hitNormal = Vector3.TransformNormal(hitNormal, obj.WorldTransform);
            
            if (Result is null)
                Result = new ShapeCollisionResult(obj, hitFraction, hitNormal, hitPointWorld, shapePart, triangleIndex);
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
    }
}
