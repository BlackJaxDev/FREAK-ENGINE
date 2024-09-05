using System.Numerics;

namespace XREngine.Physics.ShapeTracing
{
    public abstract class ShapeTraceSingle(
        XRCollisionShape? shape,
        Matrix4x4 start,
        Matrix4x4 end,
        ushort collisionGroup,
        ushort collidesWith,
        params XRCollisionObject?[] ignored) : ShapeTrace(shape, start, end, collisionGroup, collidesWith, ignored)
    {
        public override bool HasHit => Result != null;

        protected ShapeCollisionResult? Result { get; set; } = null;
        internal override void Reset() => Result = null;

        public XRCollisionObject? CollisionObject => Result?.CollisionObject;
        public float HitFraction => Result is null ? 1.0f : Result.HitFraction;
        public Vector3 HitNormalWorld => Result is null ? Vector3.Zero : Result.HitNormalWorld;
        public Vector3 HitPointWorld => Result is null ? Vector3.Zero : Result.HitPointWorld;
        public int ShapePart => Result is null ? -1 : Result.ShapePart;
        public int TriangleIndex => Result is null ? -1 : Result.TriangleIndex;

        public override void Render()
        {
            Shape.Render(Start, DebugColor, false);
            Shape.Render(End, DebugColor, false);
        }
    }
}
