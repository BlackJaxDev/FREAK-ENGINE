using System.Numerics;

namespace XREngine.Physics
{
    public abstract class XRPointPointConstraint : XRPhysicsConstraint
    {
        public static XRPointPointConstraint New(XRRigidBody rigidBodyA, Vector3 pivotInA)
            => Engine.Physics.NewPointPointConstraint(rigidBodyA, pivotInA);
        public static XRPointPointConstraint New(XRRigidBody rigidBodyA, XRRigidBody rigidBodyB, Vector3 pivotInA, Vector3 pivotInB)
            => Engine.Physics.NewPointPointConstraint(rigidBodyA, rigidBodyB, pivotInA, pivotInB);

        public override EConstraintType ConstraintType
            => EConstraintType.PointPoint;

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 PivotInB { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 PivotInA { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float Tau { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float ImpulseClamp { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float Damping { get; set; }

        /// <summary>
        /// There is a mechanism to reduce joint error: during each simulation step each joint applies a special force to bring its bodies back into correct alignment. 
        /// This force is controlled by the error reduction parameter (ERP), which has a value between 0 and 1.
        ///The ERP specifies what proportion of the joint error will be fixed during the next simulation step.
        ///If ERP = 0 then no correcting force is applied and the bodies will eventually drift apart as the simulation proceeds.
        ///If ERP = 1 then the simulation will attempt to fix all joint error during the next time step.
        ///However, setting ERP = 1 is not recommended, as the joint error will not be completely fixed due to various internal approximations.
        ///A value of ERP = 0.1 to 0.8 is recommended(0.2 is the default).
        /// </summary>
        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract bool HasErrorReductionParameter { get; }

        /// <summary>
        /// <para>If CFM is set to zero, the constraint will be hard. 
        /// If CFM is set to a positive value, it will be possible to violate the constraint by "pushing on it" 
        /// (for example, for contact constraints by forcing the two contacting objects together).
        /// In other words the constraint will be soft, and the softness will increase as CFM increases.</para>
        ///  <para>What is actually happening here is that the constraint is allowed to be violated by an amount proportional to CFM times the restoring force that is needed to enforce the constraint. 
        /// Note that setting CFM to a negative value can have undesirable bad effects, such as instability. Don't do it.</para>
        /// </summary>
        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract bool HasConstraintForceMixing { get; }
    }
}
