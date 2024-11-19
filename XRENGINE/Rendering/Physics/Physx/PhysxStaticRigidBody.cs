using MagicPhysX;
using System.Numerics;
using XREngine.Scene;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxStaticRigidBody : PhysxRigidActor, IAbstractStaticRigidBody
    {
        private readonly unsafe PxRigidStatic* _obj;

        public PhysxStaticRigidBody(
            PhysxScene scene,
            Vector3? position = null,
            Quaternion? rotation = null) : base(scene)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            Scene.PhysicsPtr->CreateRigidStaticMut(&tfm);
        }
        public PhysxStaticRigidBody(
            PhysxScene scene,
            PhysxShape shape,
            Vector3? position = null,
            Quaternion? rotation = null) : base(scene)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            Scene.PhysicsPtr->PhysPxCreateStatic1(&tfm, shape.ShapePtr);
        }
        public PhysxStaticRigidBody(
            PhysxScene scene,
            PhysxMaterial material,
            PhysxShape shape,
            Vector3? position = null,
            Quaternion? rotation = null,
            Vector3? shapeOffsetTranslation = null,
            Quaternion? shapeOffsetRotation = null) : base(scene)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            var shapeTfm = PhysxScene.MakeTransform(shapeOffsetTranslation, shapeOffsetRotation);
            Scene.PhysicsPtr->PhysPxCreateStatic(&tfm, shape.GeometryPtr, material.Material, &shapeTfm);
        }

        public override unsafe PxRigidActor* RigidActorPtr => (PxRigidActor*)_obj;
        public override unsafe PxActor* ActorPtr => (PxActor*)_obj;
        public override unsafe PxBase* BasePtr => (PxBase*)_obj;

        public void Destroy()
        {
            Scene.RemoveActor(this);
        }
    }
}