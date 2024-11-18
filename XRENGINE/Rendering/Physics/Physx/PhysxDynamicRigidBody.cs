using MagicPhysX;
using XREngine.Scene;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxDynamicRigidBody : PhysxRigidBody, IAbstractDynamicRigidBody
    {
        private readonly unsafe PxRigidDynamic* _obj;
        private readonly PhysxScene _scene;

        public PhysxDynamicRigidBody(PhysxScene scene, PhysxMaterial material, PhysxShape shape, float density)
        {
            _scene = scene;

            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            _scene.Physics->PhysPxCreateDynamic(&identity, shape.Geometry, material.Material, density, &identity);
        }

        public PhysxScene Scene => _scene;

        public PxRigidDynamic* Dynamic => _obj;
        public override PxRigidBody* Body => (PxRigidBody*)_obj;

        public void Destroy()
        {
            Scene.RemoveActor(this);
        }
    }
    public unsafe class PhysxStaticRigidBody : PhysxRigidActor, IAbstractStaticRigidBody
    {
        private readonly unsafe PxRigidStatic* _obj;
        private readonly PhysxScene _scene;

        public PhysxStaticRigidBody(PhysxScene scene)
        {
            _scene = scene;
        }

        public PhysxScene Scene => _scene;

        public override unsafe PxRigidActor* RigidActor => (PxRigidActor*)_obj;
        public override unsafe PxActor* Actor => (PxActor*)_obj;
        public override unsafe PxBase* Base => (PxBase*)_obj;

        public void Destroy()
        {
            Scene.RemoveActor(this);
        }
    }
}