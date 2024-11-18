using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx.Joints
{
    public unsafe class PhysxJoint_RackAndPinion : PhysxJointBase
    {
        public PxRackAndPinionJoint* _joint;
        public override unsafe PxJoint* JointBase => (PxJoint*)_joint;
    }
}