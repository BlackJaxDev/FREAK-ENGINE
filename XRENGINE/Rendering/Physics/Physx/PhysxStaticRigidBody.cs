using MagicPhysX;
using System.Numerics;
using XREngine.Scene;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxStaticRigidBody : PhysxRigidActor, IAbstractStaticRigidBody
    {
        private readonly unsafe PxRigidStatic* _obj;

        public PhysxStaticRigidBody(
            PhysxScene scene,
            PxRigidStatic* obj) : base(scene)
        {
            _obj = obj;
        }
        public PhysxStaticRigidBody(
            PhysxScene scene,
            Vector3? position = null,
            Quaternion? rotation = null) : base(scene)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            _obj = Scene.PhysicsPtr->CreateRigidStaticMut(&tfm);
        }
        public PhysxStaticRigidBody(
            PhysxScene scene,
            PhysxShape shape,
            Vector3? position = null,
            Quaternion? rotation = null) : base(scene)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            _obj = Scene.PhysicsPtr->PhysPxCreateStatic1(&tfm, shape.ShapePtr);
        }
        public PhysxStaticRigidBody(
            PhysxScene scene,
            PhysxMaterial material,
            PhysxGeometry geometry,
            Vector3? position = null,
            Quaternion? rotation = null,
            Vector3? shapeOffsetTranslation = null,
            Quaternion? shapeOffsetRotation = null) : base(scene)
        {
            var tfm = PhysxScene.MakeTransform(position, rotation);
            var shapeTfm = PhysxScene.MakeTransform(shapeOffsetTranslation, shapeOffsetRotation);
            _obj = Scene.PhysicsPtr->PhysPxCreateStatic(&tfm, geometry.GeometryPtr, material.Material, &shapeTfm);
        }

        public static PhysxStaticRigidBody CreatePlane(PhysxScene scene, PxPlane plane, PhysxMaterial material)
        {
            var stat = scene.PhysicsPtr->PhysPxCreatePlane(&plane, material.Material);
            return new PhysxStaticRigidBody(scene, stat);
        }
        public static PhysxStaticRigidBody CreatePlane(PhysxScene scene, Vector3 normal, float distance, PhysxMaterial material)
            => CreatePlane(scene, PxPlane_new_1(normal.X, normal.Y, normal.Z, distance), material);
        public static PhysxStaticRigidBody CreatePlane(PhysxScene scene, PhysxPlane plane, PhysxMaterial material)
            => CreatePlane(scene, plane.InternalPlane.n, plane.InternalPlane.d, material);
        public static PhysxStaticRigidBody CreatePlane(PhysxScene scene, Plane plane, PhysxMaterial material, Vector3 position, Quaternion rotation)
            => CreatePlane(scene, plane.Normal, plane.D, material);

        public override unsafe PxRigidActor* RigidActorPtr => (PxRigidActor*)_obj;
        public override unsafe PxActor* ActorPtr => (PxActor*)_obj;
        public override unsafe PxBase* BasePtr => (PxBase*)_obj;

        public void Destroy()
        {
            Scene.RemoveActor(this);
        }
    }
}