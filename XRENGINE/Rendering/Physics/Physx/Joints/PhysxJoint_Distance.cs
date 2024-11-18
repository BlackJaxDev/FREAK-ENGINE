using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx.Joints
{

    public unsafe class PhysxJoint_Distance(PxDistanceJoint* joint) : PhysxJointBase
    {
        public PxDistanceJoint* _joint = joint;
        public override unsafe PxJoint* JointBase => (PxJoint*)_joint;

        public float Distance => _joint->GetDistance();

        public float MinDistance
        {
            get => _joint->GetMinDistance();
            set => _joint->SetMinDistanceMut(value);
        }

        public float MaxDistance
        {
            get => _joint->GetMaxDistance();
            set => _joint->SetMaxDistanceMut(value);
        }

        public float Stiffness
        {
            get => _joint->GetStiffness();
            set => _joint->SetStiffnessMut(value);
        }

        public float Damping
        {
            get => _joint->GetDamping();
            set => _joint->SetDampingMut(value);
        }

        public float Tolerance
        {
            get => _joint->GetTolerance();
            set => _joint->SetToleranceMut(value);
        }

        public float ContactDistance
        {
            get => _joint->GetContactDistance();
            set => _joint->SetContactDistanceMut(value);
        }

        public PxDistanceJointFlags Flags
        {
            get => _joint->GetDistanceJointFlags();
            set => _joint->SetDistanceJointFlagsMut(value);
        }

        public void SetFlag(PxDistanceJointFlag flag, bool value)
            => _joint->SetDistanceJointFlagMut(flag, value);
    }
}