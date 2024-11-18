using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx.Joints
{
    public unsafe class PhysxJoint_Gear : PhysxJointBase
    {
        public PxGearJoint* _joint;
        public override unsafe PxJoint* JointBase => (PxJoint*)_joint;
    }
}