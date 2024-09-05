using System.Numerics;

namespace XREngine.Physics.ShapeTracing
{
    public class ShapeTraceMulti(XRCollisionShape shape, Matrix4x4 start, Matrix4x4 end, ushort collisionGroup, ushort collidesWith, params XRCollisionObject[] ignored) : ShapeTrace(shape, start, end, collisionGroup, collidesWith, ignored)
    {
        public override bool HasHit => Results.Count > 0;

        public List<ShapeCollisionResult> Results { get; } = [];
        internal override void Reset() => Results.Clear();

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
            
            Results.Add(new ShapeCollisionResult(obj, hitFraction, hitNormal, hitPointWorld, shapePart, triangleIndex));
        }

        public override void Render()
        {
            Shape.Render(Start, DebugColor, false);
            Shape.Render(End, DebugColor, false);
        }
    }
}
