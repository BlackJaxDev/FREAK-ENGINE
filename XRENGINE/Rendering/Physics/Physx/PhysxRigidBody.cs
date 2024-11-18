using MagicPhysX;
using System.Numerics;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxRigidBody : PhysxRigidActor
    {
        public abstract PxRigidBody* Body { get; }
        public override unsafe PxRigidActor* RigidActor => (PxRigidActor*)Body;

        public float AngularDamping
        {
            get => PxRigidBody_getAngularDamping(Body);
            set => PxRigidBody_setAngularDamping_mut(Body, value);
        }
        public float LinearDamping
        {
            get => PxRigidBody_getLinearDamping(Body);
            set => PxRigidBody_setLinearDamping_mut(Body, value);
        }
        public float MaxAngularVelocity
        {
            get => PxRigidBody_getMaxAngularVelocity(Body);
            set => PxRigidBody_setMaxAngularVelocity_mut(Body, value);
        }
        public float MaxLinearVelocity
        {
            get => PxRigidBody_getMaxLinearVelocity(Body);
            set => PxRigidBody_setMaxLinearVelocity_mut(Body, value);
        }
        public virtual Vector3 LinearVelocity
        {
            get => PxRigidBody_getLinearVelocity(Body);
        }
        public virtual Vector3 AngularVelocity
        {
            get => PxRigidBody_getAngularVelocity(Body);
        }
        public (Quaternion, Vector3) CMassLocalPose
        {
            get
            {
                var tfm = PxRigidBody_getCMassLocalPose(Body);
                return (tfm.q, tfm.p);
            }
            set
            {
                var x = new PxTransform { q = value.Item1, p = value.Item2 };
                PxRigidBody_setCMassLocalPose_mut(Body, &x);
            }
        }
        public float Mass
        {
            get => PxRigidBody_getMass(Body);
            set => PxRigidBody_setMass_mut(Body, value);
        }
        public float InvMass => PxRigidBody_getInvMass(Body);
        public Vector3 MassSpaceInertiaTensor
        {
            get => PxRigidBody_getMassSpaceInertiaTensor(Body);
            set
            {
                PxVec3 v = value;
                PxRigidBody_setMassSpaceInertiaTensor_mut(Body, &v);
            }
        }
        public Vector3 MassSpaceInvInertiaTensor => PxRigidBody_getMassSpaceInvInertiaTensor(Body);
        public void AddForce(Vector3 force, PxForceMode mode, bool wake)
        {
            PxVec3 v = force;
            PxRigidBody_addForce_mut(Body, &v, mode, wake);
        }

        public void AddForceAtPosition(Vector3 force, Vector3 position, PxForceMode mode, bool wake)
        {
            PxVec3 f = force;
            PxVec3 p = position;
            PxRigidBodyExt_addForceAtPos(Body, &f, &p, mode, wake);
        }

        public void AddForceAtLocalPosition(Vector3 force, Vector3 position, PxForceMode mode, bool wake)
        {
            PxVec3 f = force;
            PxVec3 p = position;
            PxRigidBodyExt_addForceAtLocalPos(Body, &f, &p, mode, wake);
        }

        public void AddLocalForceAtPosition(Vector3 force, Vector3 position, PxForceMode mode, bool wake)
        {
            PxVec3 f = force;
            PxVec3 p = position;
            PxRigidBodyExt_addLocalForceAtPos(Body, &f, &p, mode, wake);
        }

        public void AddLocalForceAtLocalPosition(Vector3 force, Vector3 position, PxForceMode mode, bool wake)
        {
            PxVec3 f = force;
            PxVec3 p = position;
            PxRigidBodyExt_addLocalForceAtLocalPos(Body, &f, &p, mode, wake);
        }

        public void AddTorque(Vector3 torque, PxForceMode mode, bool wake)
        {
            PxVec3 v = torque;
            PxRigidBody_addTorque_mut(Body, &v, mode, wake);
        }

        public void ClearTorque(PxForceMode mode)
            => PxRigidBody_clearTorque_mut(Body, mode);

        public void ClearForce(PxForceMode mode)
            => PxRigidBody_clearForce_mut(Body, mode);

        public void SetForceAndTorque(Vector3 force, Vector3 torque, PxForceMode mode)
        {
            PxVec3 f = force;
            PxVec3 t = torque;
            PxRigidBody_setForceAndTorque_mut(Body, &f, &t, mode);
        }

        public PxRigidBodyFlags Flags
        {
            get => PxRigidBody_getRigidBodyFlags(Body);
            set => PxRigidBody_setRigidBodyFlags_mut(Body, value);
        }

        public float MinCCDAdvanceCoefficient
        {
            get => PxRigidBody_getMinCCDAdvanceCoefficient(Body);
            set => PxRigidBody_setMinCCDAdvanceCoefficient_mut(Body, value);
        }

        public float MaxDepenetrationVelocity
        {
            get => PxRigidBody_getMaxDepenetrationVelocity(Body);
            set => PxRigidBody_setMaxDepenetrationVelocity_mut(Body, value);
        }

        public float MaxContactImpulse
        {
            get => PxRigidBody_getMaxContactImpulse(Body);
            set => PxRigidBody_setMaxContactImpulse_mut(Body, value);
        }

        public float ContactSlopCoefficient
        {
            get => PxRigidBody_getContactSlopCoefficient(Body);
            set => PxRigidBody_setContactSlopCoefficient_mut(Body, value);
        }

        public PxNodeIndex NodeIndex => PxRigidBody_getInternalIslandNodeIndex(Body);
    }
}