using MagicPhysX;
using System.Numerics;
using XREngine.Scene;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxStaticRigidBody : PhysxRigidActor, IAbstractStaticRigidBody
    {
        private readonly unsafe PxRigidStatic* _obj;

        public PhysxStaticRigidBody(PxRigidStatic* obj) => _obj = obj;

        public PhysxStaticRigidBody(
            Vector3? position = null,
            Quaternion? rotation = null)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            _obj = PhysxScene.PhysicsPtr->CreateRigidStaticMut(&tfm);
        }
        public PhysxStaticRigidBody(
            PhysxShape shape,
            Vector3? position = null,
            Quaternion? rotation = null)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            _obj = PhysxScene.PhysicsPtr->PhysPxCreateStatic1(&tfm, shape.ShapePtr);
        }
        public PhysxStaticRigidBody(
            PhysxMaterial material,
            PhysxGeometry geometry,
            Vector3? position = null,
            Quaternion? rotation = null,
            Vector3? shapeOffsetTranslation = null,
            Quaternion? shapeOffsetRotation = null)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            var shapeTfm = PhysxScene.MakeTransform(shapeOffsetTranslation, shapeOffsetRotation);
            _obj = PhysxScene.PhysicsPtr->PhysPxCreateStatic(&tfm, geometry.Geometry, material.MaterialPtr, &shapeTfm);
        }

        public static PhysxStaticRigidBody CreatePlane(PxPlane plane, PhysxMaterial material)
        {
            var stat = PhysxScene.PhysicsPtr->PhysPxCreatePlane(&plane, material.MaterialPtr);
            return new PhysxStaticRigidBody(stat);
        }
        public static PhysxStaticRigidBody CreatePlane(Vector3 normal, float distance, PhysxMaterial material)
            => CreatePlane(PxPlane_new_1(normal.X, normal.Y, normal.Z, distance), material);
        public static PhysxStaticRigidBody CreatePlane(PhysxPlane plane, PhysxMaterial material)
            => CreatePlane(plane.InternalPlane.n, plane.InternalPlane.d, material);
        public static PhysxStaticRigidBody CreatePlane(Plane plane, PhysxMaterial material, Vector3 position, Quaternion rotation)
            => CreatePlane(plane.Normal, plane.D, material);

        public override unsafe PxRigidActor* RigidActorPtr => (PxRigidActor*)_obj;
        public override unsafe PxActor* ActorPtr => (PxActor*)_obj;
        public override unsafe PxBase* BasePtr => (PxBase*)_obj;
    }
}