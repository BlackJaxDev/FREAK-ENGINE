using System.Numerics;

namespace XREngine.Physics
{
    public class XRContactInfo(XRCollisionObject bodyA, XRCollisionObject bodyB)
    {
        public float Distance { get; set; }
        public bool LateralFrictionInitialized { get; set; }
        public bool HasContactConstraintForceMixing { get; set; }
        public bool HasContactErrorReductionParameter { get; set; }
        public float ContactMotion2 { get; set; }
        public float ContactMotion1 { get; set; }
        public float ContactErp { get; set; }
        public float ContactCfm { get; set; }
        public float CombinedRollingFriction { get; set; }
        public float CombinedRestitution { get; set; }
        public float CombinedFriction { get; set; }
        public float AppliedImpulseLateral2 { get; set; }
        public float AppliedImpulseLateral1 { get; set; }
        public float FrictionCfm { get; set; }
        public int Index0 { get; set; }
        public int Index1 { get; set; }
        public Vector3 LateralFrictionDir1 { get; set; }
        public Vector3 LateralFrictionDir2 { get; set; }
        public int LifeTime { get; set; }
        public Vector3 LocalPointA { get; set; }
        public Vector3 LocalPointB { get; set; }
        public Vector3 NormalWorldOnB { get; set; }
        public int PartId0 { get; set; }
        public int PartId1 { get; set; }
        public Vector3 PositionWorldOnA { get; set; }
        public Vector3 PositionWorldOnB { get; set; }
        public object? UserPersistentData { get; set; }
        public float AppliedImpulse { get; set; }
        public XRCollisionObject BodyA { get; set; } = bodyA;
        public XRCollisionObject BodyB { get; set; } = bodyB;
    }
}
