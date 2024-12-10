using System.Numerics;

namespace XREngine.Physics
{
    public abstract class XRRigidBody : XRCollisionObject
    {
        protected XRRigidBody() : base() { }

        protected Vector3 _previousLinearFactor = Vector3.One;
        protected Vector3 _previousAngularFactor = Vector3.One;

        public new IRigidBodyCollidable Owner
        {
            get => (IRigidBodyCollidable)base.Owner;
            set
            {
                base.Owner = value;
                if (SimulatingPhysics && value != null)
                    WorldTransform = value.CollidableWorldMatrix;
            }
        }

        /// <summary>
        /// Creates a new rigid body using the specified physics library.
        /// </summary>
        /// <param name="info">Construction information.</param>
        /// <returns>A new rigid body.</returns>
        public static XRRigidBody New(RigidBodyConstructionInfo info)
            => Engine.Physics.NewRigidBody(info);

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 TotalForce { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 TotalTorque { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Quaternion Orientation { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 LocalInertia { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract int ConstraintCount { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 LinearFactor { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 LinearVelocity { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float LinearSleepingThreshold { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float LinearDamping { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 AngularFactor { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 AngularVelocity { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float AngularSleepingThreshold { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float AngularDamping { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract bool IsInWorld { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract bool WantsSleeping { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 Gravity { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract float Mass { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Matrix4x4 InvInertiaTensorWorld { get; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 InvInertiaDiagLocal { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract int FrictionSolverType { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract int ContactSolverType { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Matrix4x4 CenterOfMassTransform { get; set; }

        [PhysicsSupport(EPhysicsLibrary.Bullet)]
        public abstract Vector3 CenterOfMassPosition { get; }

        public Vector3 Weight => Mass * Gravity;

        public EventList<XRPhysicsConstraint> Constraints { get; } = [];

        /// <summary>
        /// Applies a force to the center of mass of the body (the origin).
        /// </summary>
        /// <param name="force">The force to apply, in Newtons.</param>
        public abstract void ApplyCentralForce(Vector3 force);
        /// <summary>
        /// Applies an impulse to the center of mass of the body (the origin).
        /// </summary>
        /// <param name="impulse">The impulse (Force * delta sec) to apply, in Newton-seconds.</param>
        public abstract void ApplyCentralImpulse(Vector3 impulse);
        /// <summary>
        /// Applies a force to the body at a position relative to the body's center of mass (the origin).
        /// </summary>
        /// <param name="force">The force to apply, in Newtons.</param>
        /// <param name="relativePosition">An offset relative to the body's origin.</param>
        public abstract void ApplyForce(Vector3 force, Vector3 relativePosition);
        /// <summary>
        /// Applies an impulse to the body at a position relative to the body's center of mass (the origin).
        /// </summary>
        /// <param name="impulse">The impulse (Force * delta sec) to apply, in Newton-seconds.</param>
        /// <param name="relativePosition">An offset relative to the body's origin.</param>
        public abstract void ApplyImpulse(Vector3 impulse, Vector3 relativePosition);
        public abstract void ApplyTorque(Vector3 torque);
        public abstract void ApplyTorqueImpulse(Vector3 torque);
        public abstract void ClearForces();
        
        public abstract void GetAabb(out Vector3 aabbMin, out Vector3 aabbMax);
        public abstract Vector3 GetVelocityInLocalPoint(Vector3 relativePosition);
        public abstract void ProceedToTransform(Matrix4x4 newTrans);
        public abstract void SetDamping(float linearDamping, float angularDamping);
        public abstract void SetMassProps(float mass, Vector3 inertia);
        public abstract void SetSleepingThresholds(float linear, float angular);
        public abstract void Translate(Vector3 v);

        protected override void StopSimulation()
        {
            _previousLinearFactor = LinearFactor;
            _previousAngularFactor = AngularFactor;

            LinearFactor = Vector3.Zero;
            AngularFactor = Vector3.Zero;

            base.StopSimulation();
        }
        protected override void StartSimulation()
        {
            LinearFactor = _previousLinearFactor;
            AngularFactor = _previousAngularFactor;

            base.StartSimulation();
        }
    }
}
