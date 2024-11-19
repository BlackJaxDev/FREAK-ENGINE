using MagicPhysX;
using System.Numerics;

namespace XREngine.Rendering.Physics.Physx.Joints
{
    public abstract unsafe class PhysxJoint
    {
        public abstract PxJoint* JointBase { get; }

        public PxScene* Scene => JointBase->GetScene();
        public void Release() => JointBase->ReleaseMut();
        public string Name
        {
            get => new((sbyte*)JointBase->GetName());
            set
            {
                fixed (char* v = value)
                    JointBase->SetNameMut((byte*)v);
            }
        }
        public PxConstraint* Constraint => JointBase->GetConstraint();

        public float InvMassScale0
        {
            get => JointBase->GetInvMassScale0();
            set => JointBase->SetInvMassScale0Mut(value);
        }

        public float InvMassScale1
        {
            get => JointBase->GetInvMassScale1();
            set => JointBase->SetInvMassScale1Mut(value);
        }

        public float InvInertiaScale0
        {
            get => JointBase->GetInvInertiaScale0();
            set => JointBase->SetInvInertiaScale0Mut(value);
        }

        public float InvInertiaScale1
        {
            get => JointBase->GetInvInertiaScale1();
            set => JointBase->SetInvInertiaScale1Mut(value);
        }

        public PxConstraintFlags Flags
        {
            get => JointBase->GetConstraintFlags();
            set => JointBase->SetConstraintFlagsMut(value);
        }

        public void SetFlag(PxConstraintFlag flag, bool value)
            => JointBase->SetConstraintFlagMut(flag, value);

        public (float force, float torque) BreakForce
        {
            get
            {
                float force, torque;
                JointBase->GetBreakForce(&force, &torque);
                return (force, torque);
            }
            set => JointBase->SetBreakForceMut(value.force, value.torque);
        }

        public Vector3 RelativeAngularVelocity => JointBase->GetRelativeAngularVelocity();
        public Vector3 RelativeLinearVelocity => JointBase->GetRelativeLinearVelocity();
        public (Vector3 position, Quaternion rotation) RelativeTransform
        {
            get
            {
                PxTransform t = JointBase->GetRelativeTransform();
                return (t.p, t.q);
            }
        }

        public (Vector3 position, Quaternion rotation) LocalPoseActor0
        {
            get
            {
                PxTransform t = JointBase->GetLocalPose(PxJointActorIndex.Actor0);
                return (t.p, t.q);
            }
            set
            {
                PxTransform v = new() { p = value.position, q = value.rotation };
                JointBase->SetLocalPoseMut(PxJointActorIndex.Actor0, &v);
            }
        }
        public (Vector3 position, Quaternion rotation) LocalPoseActor1
        {
            get
            {
                PxTransform t = JointBase->GetLocalPose(PxJointActorIndex.Actor1);
                return (t.p, t.q);
            }
            set
            {
                PxTransform v = new() { p = value.position, q = value.rotation };
                JointBase->SetLocalPoseMut(PxJointActorIndex.Actor1, &v);
            }
        }
        public void GetActors(out PxRigidActor* actor0, out PxRigidActor* actor1)
        {
            PxRigidActor* a0, a1;
            JointBase->GetActors(&a0, &a1);
            actor0 = a0;
            actor1 = a1;
        }
        public void SetActors(PxRigidActor* actor0, PxRigidActor* actor1)
            => JointBase->SetActorsMut(actor0, actor1);
    }
}