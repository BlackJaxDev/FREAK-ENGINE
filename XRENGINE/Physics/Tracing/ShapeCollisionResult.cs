using System.Numerics;
using XREngine.Data.Core;

namespace XREngine.Physics.ShapeTracing
{
    /// <summary>
    /// Provides information about a ray intersection.
    /// </summary>
    public class ShapeCollisionResult : XRBase
    {
        public XRCollisionObject CollisionObject { get; internal set; }
        public float HitFraction { get; internal set; }
        public Vector3 HitNormalWorld { get; internal set; }
        public Vector3 HitPointWorld { get; internal set; }
        public int ShapePart { get; internal set; }
        public int TriangleIndex { get; internal set; }
        
        internal ShapeCollisionResult() { }
        internal ShapeCollisionResult(
            XRCollisionObject collisionObject, 
            float hitFraction,
            Vector3 hitNormalWorld,
            Vector3 hitPointWorld, 
            int shapePart,
            int triangleIndex)
        {
            CollisionObject = collisionObject;
            HitFraction = hitFraction;
            HitNormalWorld = hitNormalWorld;
            HitPointWorld = hitPointWorld;
            ShapePart = shapePart;
            TriangleIndex = triangleIndex;
        }
    }
}
