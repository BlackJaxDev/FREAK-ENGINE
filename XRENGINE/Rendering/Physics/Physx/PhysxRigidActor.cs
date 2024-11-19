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
            => PxRigidActor_setGlobalPose_mut(RigidActorPtr, &pose, wake);

        public void SetTransform(Vector3 position, Quaternion rotation, bool wake)
            => SetTransform(new() { p = position, q = rotation }, wake);

        public uint ConstraintCount
            => PxRigidActor_getNbConstraints(RigidActorPtr);

        public uint ShapeCount
            => PxRigidActor_getNbShapes(RigidActorPtr);

        public uint GetConstraints(PxConstraint** constraints, uint maxConstraints, uint startIndex)
            => PxRigidActor_getConstraints(RigidActorPtr, constraints, maxConstraints, startIndex);

        public uint GetShapes(PxShape** shapes, uint maxShapes, uint startIndex)
            => PxRigidActor_getShapes(RigidActorPtr, shapes, maxShapes, startIndex);

        public void AttachShape(PxShape shape)
            => PxRigidActor_attachShape_mut(RigidActorPtr, &shape);

        public void DetachShape(PxShape shape, bool wakeOnLostTouch)
            => PxRigidActor_detachShape_mut(RigidActorPtr, &shape, wakeOnLostTouch);
    }
}