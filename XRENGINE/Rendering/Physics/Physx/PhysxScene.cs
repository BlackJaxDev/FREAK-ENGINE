using MagicPhysX;
using System.Numerics;
using XREngine.Scene;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxScene : AbstractPhysicsScene
    {
        public static readonly PxVec3 DefaultGravity = new() { x = 0.0f, y = -9.81f, z = 0.0f };

        private PxPhysics* _physics;
        private PxCpuDispatcher* _dispatcher;
        private PxScene* _scene;

        public PxPhysics* Physics => _physics;
        public PxScene* Scene => _scene;
        public PxCpuDispatcher* Dispatcher => _dispatcher;

        public Vector3 Gravity
        {
            get => _scene->GetGravity();
            set
            {
                PxVec3 g = value;
                _scene->SetGravityMut(&g);
            }
        }

        public override void Initialize()
        {
            _physics = physx_create_physics(physx_create_foundation());
            var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(_physics));
            sceneDesc.gravity = DefaultGravity;

            _dispatcher = (PxCpuDispatcher*)phys_PxDefaultCpuDispatcherCreate(4, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
            sceneDesc.cpuDispatcher = _dispatcher;
            sceneDesc.filterShader = get_default_simulation_filter_shader();
            sceneDesc.flags |= PxSceneFlags.EnableCcd | PxSceneFlags.EnableGpuDynamics;
            sceneDesc.broadPhaseType = PxBroadPhaseType.Gpu;
            sceneDesc.gpuDynamicsConfig = new PxgDynamicsMemoryConfig()
            {

            };
            _scene = _physics->CreateSceneMut(&sceneDesc);

            var material = _physics->CreateMaterialMut(0.5f, 0.5f, 0.6f);

            // create plane and add to scene
            var plane = PxPlane_new_1(0.0f, 1.0f, 0.0f, 0.0f);
            var groundPlane = _physics->PhysPxCreatePlane(&plane, material);
            _scene->AddActorMut((PxActor*)groundPlane, null);

            // create sphere and add to scene
            var sphereGeo = PxSphereGeometry_new(10.0f);
            var Vector3 = new PxVec3 { x = 0.0f, y = 40.0f, z = 100.0f };
            var transform = PxTransform_new_1(&Vector3);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            var sphere = _physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&sphereGeo, material, 10.0f, &identity);
            PxRigidBody_setAngularDamping_mut((PxRigidBody*)sphere, 0.5f);
            _scene->AddActorMut((PxActor*)sphere, null);
        }

        public override void StepSimulation()
        {
            _scene->SimulateMut(Engine.Time.Timer.FixedUpdateDelta, null, null, 0, true);

            uint error = 0;
            if (!_scene->FetchResultsMut(true, &error))
                return;

            NotifySimulationStepped();
        }

        public override void Destroy()
        {
            PxScene_release_mut(_scene);
            PxDefaultCpuDispatcher_release_mut((PxDefaultCpuDispatcher*)_dispatcher);
            PxPhysics_release_mut(_physics);
        }

        public override IAbstractDynamicRigidBody? NewDynamicRigidBody()
            => new PhysxDynamicRigidBody(this, null, null, 0);

        public override IAbstractStaticRigidBody? NewStaticRigidBody()
            => new PhysxStaticRigidBody(this);

        public void RemoveActor(PhysxActor actor, bool wakeOnLostTouch = false)
        {
            _scene->RemoveActorMut(actor.Actor, wakeOnLostTouch);
        }
    }
}