using XREngine.Rendering;

namespace XREngine.Physics.ContactTesting
{
    /// <summary>
    /// Contains properties and methods for projecting a ray in the world and testing for intersections with collision objects.
    /// </summary>
    public abstract class ContactTest(
        XRCollisionObject obj,
        ushort collisionGroup,
        ushort collidesWith,
        params XRCollisionObject[] ignored)
    {
        public XRCollisionObject Object { get; set; } = obj;
        public ushort CollisionGroup { get; set; } = collisionGroup;
        public ushort CollidesWith { get; set; } = collidesWith;
        public XRCollisionObject[] Ignored { get; set; } = ignored;

        public abstract bool HasContact { get; }

        protected bool CanAddCommon(XRCollisionObject obj)
            => !Ignored.Contains(obj);

        internal protected abstract void AddResult(XRContactInfo contact, XRCollisionObject otherObject, bool isOtherB);
        //internal protected virtual bool TestApproxCollision(int uniqueID, ushort collisionGroup, ushort collidesWith, Vector3 aabbMin, Vector3 aabbMax, object clientObject)
        //{
        //    //if (Ignored.Any(x => x.UniqueID == uniqueID))
        //    //    return false;
            
        //    //I believe this algorithm is faster.
        //    if (Collision.RayIntersectsAABBDistance(StartPointWorld, dir, aabbMin, aabbMax, out float distance) && distance * distance < dir.LengthSquared)
        //    //if (Collision.SegmentIntersectsAABB(Start, End, aabbMin, aabbMax, out Vector3 enterPoint, out Vector3 exitPoint))
        //    {
        //        bool rayIntersectsOther = (CollisionGroup & collidesWith) == CollisionGroup;
        //        bool otherIntersectsRay = (collisionGroup & CollidesWith) == collisionGroup;
        //        if (rayIntersectsOther && otherIntersectsRay)
        //            return true;
        //    }
        //    return false;
        //}

        /// <summary>
        /// Performs the test in the world and returns true if there are any collision results.
        /// </summary>
        public bool Test(XRWorldInstance world)
            => Engine.Physics.ContactTest(this, world);

        internal abstract void Reset();
    }
}
