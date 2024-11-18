using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx.Joints
{
    public unsafe class PhysxJoint_D6 : PhysxJointBase
    {
        public PxD6Joint* _joint;
        public override unsafe PxJoint* JointBase => (PxJoint*)_joint;
    }
}