using MagicPhysX;
using System.Numerics;
using XREngine.Scene;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxDynamicRigidBody : PhysxRigidBody, IAbstractDynamicRigidBody
    {
        private readonly unsafe PxRigidDynamic* _obj;
        public PxRigidDynamic* DynamicPtr => _obj;
        public override PxRigidBody* BodyPtr => (PxRigidBody*)_obj;

        public PhysxDynamicRigidBody(
            PhysxScene scene,
            PhysxMaterial material,
            PhysxGeometry geometry,
            float density,
            Vector3? position = null,
            Quaternion? rotation = null,
            Vector3? shapeOffsetTranslation = null,
            Quaternion? shapeOffsetRotation = null) : base(scene)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            var shapeTfm = PhysxScene.MakeTransform(shapeOffsetTranslation, shapeOffsetRotation);
            Scene.PhysicsPtr->PhysPxCreateDynamic(&tfm, geometry.GeometryPtr, material.Material, density, &shapeTfm);
        }
        public PhysxDynamicRigidBody(
            PhysxScene scene,
            PhysxShape shape,
            float density,
            Vector3? position = null,
            Quaternion? rotation = null) : base(scene)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            Scene.PhysicsPtr->PhysPxCreateDynamic1(&tfm, shape.ShapePtr, density);
        }
        public PhysxDynamicRigidBody(
            PhysxScene scene,
            Vector3? position = null,
            Quaternion? rotation = null) : base(scene)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            Scene.PhysicsPtr->CreateRigidDynamicMut(&tfm);
        }

        public void Destroy(bool wakeOnLostTouch = false)
            => Scene.RemoveActor(this, wakeOnLostTouch);
    }
}