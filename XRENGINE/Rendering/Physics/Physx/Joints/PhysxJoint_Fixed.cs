using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx.Joints
{
    public unsafe class PhysxJoint_Fixed : PhysxJointBase
    {
        public PxFixedJoint* _joint;
        public override unsafe PxJoint* JointBase => (PxJoint*)_joint;
    }
}