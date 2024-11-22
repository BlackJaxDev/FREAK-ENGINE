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
            PhysxMaterial material,
            IAbstractPhysicsGeometry geometry,
            float density,
            Vector3? position = null,
            Quaternion? rotation = null,
            Vector3? shapeOffsetTranslation = null,
            Quaternion? shapeOffsetRotation = null)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            var shapeTfm = PhysxScene.MakeTransform(shapeOffsetTranslation, shapeOffsetRotation);
            PhysxScene.PhysicsPtr->PhysPxCreateDynamic(&tfm, geometry.Geometry, material.MaterialPtr, density, &shapeTfm);
        }
        public PhysxDynamicRigidBody(
            PhysxShape shape,
            float density,
            Vector3? position = null,
            Quaternion? rotation = null)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            PhysxScene.PhysicsPtr->PhysPxCreateDynamic1(&tfm, shape.ShapePtr, density);
        }
        public PhysxDynamicRigidBody(
            Vector3? position = null,
            Quaternion? rotation = null)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            PhysxScene.PhysicsPtr->CreateRigidDynamicMut(&tfm);
        }
    }
}