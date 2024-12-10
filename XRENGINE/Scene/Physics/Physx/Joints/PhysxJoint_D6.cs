using MagicPhysX;
using System.Numerics;

namespace XREngine.Rendering.Physics.Physx.Joints
{
    public unsafe class PhysxJoint_D6(PxD6Joint* joint) : PhysxJoint, IHingeJoint, IPrismaticJoint
    {
        public PxD6Joint* _joint = joint;

        public override unsafe PxJoint* JointBase => (PxJoint*)_joint;

        public float TwistAngle => _joint->GetTwistAngle();
        public float SwingYAngle => _joint->GetSwingYAngle();
        public float SwingZAngle => _joint->GetSwingZAngle();

        public (float value, float restitution, float bounceThreshold, float stiffness, float damping) DistanceLimit
        {
            get
            {
                PxJointLinearLimit limit = _joint->GetDistanceLimit();
                return (limit.value, limit.restitution, limit.bounceThreshold, limit.stiffness, limit.damping);
            }
            set
            {
                PxJointLinearLimit limit = new() { value = value.@value, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetDistanceLimitMut(&limit);
            }
        }

        public (float lower, float upper, float restitution, float bounceThreshold, float stiffness, float damping) LinearLimitX
        {
            get
            {
                PxJointLinearLimitPair limit = _joint->GetLinearLimit(PxD6Axis.X);
                return (limit.lower, limit.upper, limit.restitution, limit.bounceThreshold, limit.stiffness, limit.damping);
            }
            set
            {
                PxJointLinearLimitPair limit = new() { lower = value.lower, upper = value.upper, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetLinearLimitMut(PxD6Axis.X, &limit);
            }
        }
        public (float lower, float upper, float restitution, float bounceThreshold, float stiffness, float damping) LinearLimitY
        {
            get
            {
                PxJointLinearLimitPair limit = _joint->GetLinearLimit(PxD6Axis.Y);
                return (limit.lower, limit.upper, limit.restitution, limit.bounceThreshold, limit.stiffness, limit.damping);
            }
            set
            {
                PxJointLinearLimitPair limit = new() { lower = value.lower, upper = value.upper, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetLinearLimitMut(PxD6Axis.Y, &limit);
            }
        }
        public (float lower, float upper, float restitution, float bounceThreshold, float stiffness, float damping) LinearLimitZ
        {
            get
            {
                PxJointLinearLimitPair limit = _joint->GetLinearLimit(PxD6Axis.Z);
                return (limit.lower, limit.upper, limit.restitution, limit.bounceThreshold, limit.stiffness, limit.damping);
            }
            set
            {
                PxJointLinearLimitPair limit = new() { lower = value.lower, upper = value.upper, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetLinearLimitMut(PxD6Axis.Z, &limit);
            }
        }
        public (float lower, float upper, float restitution, float bounceThreshold, float stiffness, float damping) TwistLimit
        {
            get
            {
                PxJointAngularLimitPair limit = _joint->GetTwistLimit();
                return (limit.lower, limit.upper, limit.restitution, limit.bounceThreshold, limit.stiffness, limit.damping);
            }
            set
            {
                PxJointAngularLimitPair limit = new() { lower = value.lower, upper = value.upper, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetTwistLimitMut(&limit);
            }
        }
        public (float lower, float upper, float restitution, float bounceThreshold, float stiffness, float damping) Swing1Limit
        {
            get
            {
                PxJointLinearLimitPair limit = _joint->GetLinearLimit(PxD6Axis.Swing1);
                return (limit.lower, limit.upper, limit.restitution, limit.bounceThreshold, limit.stiffness, limit.damping);
            }
            set
            {
                PxJointLinearLimitPair limit = new() { lower = value.lower, upper = value.upper, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetLinearLimitMut(PxD6Axis.Swing1, &limit);
            }
        }
        public (float lower, float upper, float restitution, float bounceThreshold, float stiffness, float damping) Swing2Limit
        {
            get
            {
                PxJointLinearLimitPair limit = _joint->GetLinearLimit(PxD6Axis.Swing2);
                return (limit.lower, limit.upper, limit.restitution, limit.bounceThreshold, limit.stiffness, limit.damping);
            }
            set
            {
                PxJointLinearLimitPair limit = new() { lower = value.lower, upper = value.upper, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetLinearLimitMut(PxD6Axis.Swing2, &limit);
            }
        }

        public (float stiffness, float damping, float forceLimit, PxD6JointDriveFlags flags) DriveX
        {
            get
            {
                PxD6JointDrive drive = _joint->GetDrive(PxD6Drive.X);
                return (drive.stiffness, drive.damping, drive.forceLimit, drive.flags);
            }
            set
            {
                PxD6JointDrive drive = new() { stiffness = value.stiffness, damping = value.damping, forceLimit = value.forceLimit, flags = value.flags };
                _joint->SetDriveMut(PxD6Drive.X, &drive);
            }
        }
        public (float stiffness, float damping, float forceLimit, PxD6JointDriveFlags flags) DriveY
        {
            get
            {
                PxD6JointDrive drive = _joint->GetDrive(PxD6Drive.Y);
                return (drive.stiffness, drive.damping, drive.forceLimit, drive.flags);
            }
            set
            {
                PxD6JointDrive drive = new() { stiffness = value.stiffness, damping = value.damping, forceLimit = value.forceLimit, flags = value.flags };
                _joint->SetDriveMut(PxD6Drive.Y, &drive);
            }
        }
        public (float stiffness, float damping, float forceLimit, PxD6JointDriveFlags flags) DriveZ
        {
            get
            {
                PxD6JointDrive drive = _joint->GetDrive(PxD6Drive.Z);
                return (drive.stiffness, drive.damping, drive.forceLimit, drive.flags);
            }
            set
            {
                PxD6JointDrive drive = new() { stiffness = value.stiffness, damping = value.damping, forceLimit = value.forceLimit, flags = value.flags };
                _joint->SetDriveMut(PxD6Drive.Z, &drive);
            }
        }
        public (float stiffness, float damping, float forceLimit, PxD6JointDriveFlags flags) DriveSwing
        {
            get
            {
                PxD6JointDrive drive = _joint->GetDrive(PxD6Drive.Swing);
                return (drive.stiffness, drive.damping, drive.forceLimit, drive.flags);
            }
            set
            {
                PxD6JointDrive drive = new() { stiffness = value.stiffness, damping = value.damping, forceLimit = value.forceLimit, flags = value.flags };
                _joint->SetDriveMut(PxD6Drive.Swing, &drive);
            }
        }
        public (float stiffness, float damping, float forceLimit, PxD6JointDriveFlags flags) DriveTwist
        {
            get
            {
                PxD6JointDrive drive = _joint->GetDrive(PxD6Drive.Twist);
                return (drive.stiffness, drive.damping, drive.forceLimit, drive.flags);
            }
            set
            {
                PxD6JointDrive drive = new() { stiffness = value.stiffness, damping = value.damping, forceLimit = value.forceLimit, flags = value.flags };
                _joint->SetDriveMut(PxD6Drive.Twist, &drive);
            }
        }
        public (float stiffness, float damping, float forceLimit, PxD6JointDriveFlags flags) DriveSlerp
        {
            get
            {
                PxD6JointDrive drive = _joint->GetDrive(PxD6Drive.Slerp);
                return (drive.stiffness, drive.damping, drive.forceLimit, drive.flags);
            }
            set
            {
                PxD6JointDrive drive = new() { stiffness = value.stiffness, damping = value.damping, forceLimit = value.forceLimit, flags = value.flags };
                _joint->SetDriveMut(PxD6Drive.Slerp, &drive);
            }
        }

        public (Vector3 position, Quaternion Quaternion) DrivePosition
        {
            get
            {
                PxTransform t = _joint->GetDrivePosition();
                return (t.p, t.q);
            }
            set
            {
                PxTransform t = new() { p = value.position, q = value.Quaternion };
                _joint->SetDrivePositionMut(&t, true);
            }
        }

        public void SetDrivePosition(Vector3 position, Quaternion Quaternion, bool wake)
        {
            PxTransform t = new() { p = position, q = Quaternion };
            _joint->SetDrivePositionMut(&t, wake);
        }

        public (Vector3 linear, Vector3 angular) DriveVelocity
        {
            get
            {

                PxVec3 linear, angular;
                _joint->GetDriveVelocity(&linear, &angular);
                return (linear, angular);
            }
            set
            {
                PxVec3 linear = value.linear;
                PxVec3 angular = value.angular;
                _joint->SetDriveVelocityMut(&linear, &angular, true);
            }
        }

        public void SetDriveVelocity(Vector3 linear, Vector3 angular, bool wake)
        {
            PxVec3 l = linear;
            PxVec3 a = angular;
            _joint->SetDriveVelocityMut(&l, &a, wake);
        }

        public float ProjectionLinearTolerance
        {
            get => _joint->GetProjectionLinearTolerance();
            set => _joint->SetProjectionLinearToleranceMut(value);
        }
        public float ProjectionAngularTolerance
        {
            get => _joint->GetProjectionAngularTolerance();
            set => _joint->SetProjectionAngularToleranceMut(value);
        }

        (float yAngleMin, float yAngleMax, float zAngleMin, float zAngleMax, float restitution, float bounceThreshold, float stiffness, float damping) PyramidSwingLimit
        {
            get
            {
                PxJointLimitPyramid limit = _joint->GetPyramidSwingLimit();
                return (limit.yAngleMin, limit.yAngleMax, limit.zAngleMin, limit.zAngleMax, limit.restitution, limit.bounceThreshold, limit.stiffness, limit.damping);
            }
            set
            {
                PxJointLimitPyramid limit = new() { yAngleMin = value.yAngleMin, yAngleMax = value.yAngleMax, zAngleMin = value.zAngleMin, zAngleMax = value.zAngleMax, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetPyramidSwingLimitMut(&limit);
            }
        }

        public (float yAngle, float zAngle, float restitution, float bounceThreshold, float stiffness, float damping) SwingLimit
        {
            get
            {
                PxJointLimitCone limit = _joint->GetSwingLimit();
                return (limit.yAngle, limit.zAngle, limit.restitution, limit.bounceThreshold, limit.stiffness, limit.damping);
            }
            set
            {
                PxJointLimitCone limit = new() { yAngle = value.yAngle, zAngle = value.zAngle, restitution = value.restitution, bounceThreshold = value.bounceThreshold, stiffness = value.stiffness, damping = value.damping };
                _joint->SetSwingLimitMut(&limit);
            }
        }

        public PxD6Motion MotionX
        {
            get => _joint->GetMotion(PxD6Axis.X);
            set => _joint->SetMotionMut(PxD6Axis.X, value);
        }
        public PxD6Motion MotionY
        {
            get => _joint->GetMotion(PxD6Axis.Y);
            set => _joint->SetMotionMut(PxD6Axis.Y, value);
        }
        public PxD6Motion MotionZ
        {
            get => _joint->GetMotion(PxD6Axis.Z);
            set => _joint->SetMotionMut(PxD6Axis.Z, value);
        }
        public PxD6Motion MotionTwist
        {
            get => _joint->GetMotion(PxD6Axis.Twist);
            set => _joint->SetMotionMut(PxD6Axis.Twist, value);
        }
        public PxD6Motion MotionSwing1
        {
            get => _joint->GetMotion(PxD6Axis.Swing1);
            set => _joint->SetMotionMut(PxD6Axis.Swing1, value);
        }
        public PxD6Motion MotionSwing2
        {
            get => _joint->GetMotion(PxD6Axis.Swing2);
            set => _joint->SetMotionMut(PxD6Axis.Swing2, value);
        }
    }
}