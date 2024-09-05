using System.Numerics;
using XREngine.Data.Core;

namespace XREngine.Physics
{
    public abstract class XRPhysicsConstraint : XRBase
    {
        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract int UniqueID { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract bool IsEnabled { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract XRRigidBody? RigidBodyB { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract XRRigidBody? RigidBodyA { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract int OverrideNumSolverIterations { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract bool NeedsFeedback { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 AppliedTorqueBodyB { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 AppliedTorqueBodyA { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 AppliedForceBodyB { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 AppliedForceBodyA { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float DebugDrawSize { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract EConstraintType ConstraintType { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float BreakingImpulseThreshold { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float AppliedImpulse { get; }
        
        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract void EnableFeedback(bool needsFeedback);

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract void GetInfo1(XRConstraintInfo1 info);

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract void GetInfo2(XRConstraintInfo2 info);

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float GetParam(EConstraintParam num, int axis);

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float GetParam(EConstraintParam num);

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract void SetParam(EConstraintParam num, float value, int axis);

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract void SetParam(EConstraintParam num, float value);
    }
}
