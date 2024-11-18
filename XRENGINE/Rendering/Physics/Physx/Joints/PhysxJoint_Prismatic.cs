using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx.Joints
{
    public unsafe class PhysxJoint_Prismatic : PhysxJointBase
    {
        public PxPrismaticJoint* _joint;
        public override unsafe PxJoint* JointBase => (PxJoint*)_joint;

        public float Velocity => _joint->GetVelocity();
        public float Position => _joint->GetPosition();
        public (float lower, float upper, float restitution, float bounceThreshold, float stiffness, float damping) Limit
        {
            get 
            {
                PxJointLinearLimitPair pair = _joint->GetLimit();
                return (pair.lower, pair.upper, pair.restitution, pair.bounceThreshold, pair.stiffness, pair.damping);
            }
            set
            {
                PxJointLinearLimitPair pair = new()
                {
                    lower = value.lower,
                    upper = value.upper,
                    restitution = value.restitution,
                    bounceThreshold = value.bounceThreshold,
                    stiffness = value.stiffness,
                    damping = value.damping
                };
                _joint->SetLimitMut(&pair);
            }
        }
        public PxPrismaticJointFlags PrismaticFlags
        {
            get => _joint->GetPrismaticJointFlags();
            set => _joint->SetPrismaticJointFlagsMut(value);
        }

        public void SetPrismaticJointFlag(PxPrismaticJointFlag flag, bool value)
            => _joint->SetPrismaticJointFlagMut(flag, value);
    }
}