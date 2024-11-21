using MagicPhysX;
using System.Numerics;
using XREngine.Scene.Transforms;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxRigidActor(PhysxScene scene) : PhysxActor(scene)
    {
        public abstract PxRigidActor* RigidActorPtr { get; }
        public override unsafe PxActor* ActorPtr => (PxActor*)RigidActorPtr;

        public uint InternalActorIndex => RigidActorPtr->GetInternalActorIndex();

        public void ApplyTransformTo(RigidBodyTransform transform)
        {
            GetTransform(out var position, out var rotation);
            transform.Position = position;
            transform.Rotation = rotation;
        }

        public void ApplyTransformTo(Transform transform)
        {
            GetTransform(out var position, out var rotation);
            transform.DeriveWorldMatrix(Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position));
        }

        public (Vector3 position, Quaternion rotation) Transform
        {
            get
            {
                GetTransform(out var position, out var rotation);
                return (position, rotation);
            }
            set => SetTransform(new() { p = value.position, q = value.rotation }, true);
        }

        private void GetTransform(out Vector3 position, out Quaternion rotation)
        {
            var pose = PxRigidActor_getGlobalPose(RigidActorPtr);
            position = new Vector3(pose.p.x, pose.p.y, pose.p.z);
            rotation = new Quaternion(pose.q.x, pose.q.y, pose.q.z, pose.q.w);
        }

        private void SetTransform(PxTransform pose, bool wake)
            => RigidActorPtr->SetGlobalPoseMut(&pose, wake);

        public void SetTransform(Vector3 position, Quaternion rotation, bool wake)
            => SetTransform(new() { p = position, q = rotation }, wake);

        public uint ConstraintCount
            => RigidActorPtr->GetNbConstraints();

        public uint ShapeCount
            => RigidActorPtr->GetNbShapes();

        public PxConstraint*[] GetConstraints()
        {
            var constraints = new PxConstraint*[ConstraintCount];
            fixed (PxConstraint** constraintsPtr = constraints)
                RigidActorPtr->GetConstraints(constraintsPtr, ConstraintCount, 0);
            return constraints;
        }

        public PxShape*[] GetShapes()
        {
            var shapes = new PxShape*[ShapeCount];
            fixed (PxShape** shapesPtr = shapes)
                RigidActorPtr->GetShapes(shapesPtr, ShapeCount, 0);
            return shapes;
        }

        public void AttachShape(PxShape shape)
            => RigidActorPtr->AttachShapeMut(&shape);

        public void DetachShape(PxShape shape, bool wakeOnLostTouch)
            => RigidActorPtr->DetachShapeMut(&shape, wakeOnLostTouch);

        public override void Release()
            => RigidActorPtr->ReleaseMut();

        public PxQueryFilterCallback* CreateRaycastFilterCallback()
            => RigidActorPtr->CreateRaycastFilterCallback();
    }
}