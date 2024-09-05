using System.Drawing;
using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Rendering;

namespace XREngine.Physics.ShapeTracing
{
    public abstract class ShapeTrace(
        XRCollisionShape? shape,
        Matrix4x4 start,
        Matrix4x4 end,
        ushort collisionGroup,
        ushort collidesWith,
        params XRCollisionObject?[] ignored) : XRBase
    {
        public XRCollisionShape? Shape { get; set; } = shape;
        public Matrix4x4 Start { get; set; } = start;
        public Matrix4x4 End { get; set; } = end;
        public ushort CollisionGroup { get; set; } = collisionGroup;
        public ushort CollidesWith { get; set; } = collidesWith;
        public XRCollisionObject?[] Ignored { get; set; } = ignored;
        public float AllowedCcdPenetration { get; set; } = -1.0f;
        public abstract bool HasHit { get; }
        public bool DebugDraw { get; set; } = true;
        public ColorF4 DebugColor { get; set; } = Color.Magenta;

        protected bool CanAddCommon(XRCollisionObject obj)
            => obj.HasContactResponse && !Ignored.Any(x => x == obj);
        
        internal protected abstract void AddResult(XRCollisionObject obj, Vector3 hitNormalLocal, bool normalInWorldSpace, Vector3 hitPointLocal, float hitFraction, int shapePart, int triangleIndex);
        internal virtual bool TestApproxCollision(int uniqueID, ushort collisionGroup, ushort collidesWith, Vector3 aabbMin, Vector3 aabbMax, object clientObject)
        {
            bool rayIntersectsOther = (CollisionGroup & collidesWith) == CollisionGroup;
            bool otherIntersectsRay = (collisionGroup & CollidesWith) == collisionGroup;
            return rayIntersectsOther && otherIntersectsRay;
        }

        /// <summary>
        /// Performs the trace in the world and returns true if there are any collision results.
        /// </summary>
        public bool Trace(XRWorldInstance world)
            => Engine.Physics.ShapeTrace(this, world);
        
        internal abstract void Reset();
        public abstract void Render();
    }
}
