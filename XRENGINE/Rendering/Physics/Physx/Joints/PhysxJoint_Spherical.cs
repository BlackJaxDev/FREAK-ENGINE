using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx.Joints
{
    public unsafe class PhysxJoint_Spherical : PhysxJointBase
    {
        public PxSphericalJoint* _joint;
        public override unsafe PxJoint* JointBase => (PxJoint*)_joint;

        public (float zAngle, float yAngle, float restitution, float bounceThreshold, float stiffness, float damping) LimitCone
        {
            get
            {
                var cone = _joint->GetLimitCone();
                return (cone.zAngle, cone.yAngle, cone.restitution, cone.bounceThreshold, cone.stiffness, cone.damping);
            }
            set
            {
                var cone = new PxJointLimitCone() { zAngle = value.zAngle, yAngle = value.yAngle, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetLimitConeMut(&cone);
            }
        }

        public float SwingZAngle => _joint->GetSwingZAngle();
        public float SwingYAngle => _joint->GetSwingYAngle();

        public PxSphericalJointFlags SphericalFlags
        {
            get => _joint->GetSphericalJointFlags();
            set => _joint->SetSphericalJointFlagsMut(value);
        }

        public void SetSphericalJointFlag(PxSphericalJointFlag flag, bool value)
            => _joint->SetSphericalJointFlagMut(flag, value);
    }
}