using MagicPhysX;
using System.Numerics;
using static MagicPhysX.NativeMethods;
using static XREngine.Engine;

namespace XREngine.Scene
{
    public unsafe class DefaultPhysxScene : PhysicsScene
    {
        private PxPhysics* physics;
        private PxCpuDispatcher* dispatcher;
        private PxScene* scene;

        public override void Initialize()
        {
            physics = physx_create_physics(physx_create_foundation());
            var sceneDesc = PxSceneDesc_new(PxPhysics_getTolerancesScale(physics));
            sceneDesc.gravity = new() { x = 0.0f, y = -9.81f, z = 0.0f };

            dispatcher = (PxCpuDispatcher*)phys_PxDefaultCpuDispatcherCreate(1, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);
            sceneDesc.cpuDispatcher = dispatcher;
            sceneDesc.filterShader = get_default_simulation_filter_shader();
            scene = physics->CreateSceneMut(&sceneDesc);

            var material = physics->CreateMaterialMut(0.5f, 0.5f, 0.6f);

            // create plane and add to scene
            var plane = PxPlane_new_1(0.0f, 1.0f, 0.0f, 0.0f);
            var groundPlane = physics->PhysPxCreatePlane(&plane, material);
            scene->AddActorMut((PxActor*)groundPlane, null);

            // create sphere and add to scene
            var sphereGeo = PxSphereGeometry_new(10.0f);
            var Vector3 = new PxVec3 { x = 0.0f, y = 40.0f, z = 100.0f };
            var transform = PxTransform_new_1(&Vector3);
            var identity = PxTransform_new_2(PxIDENTITY.PxIdentity);
            var sphere = physics->PhysPxCreateDynamic(&transform, (PxGeometry*)&sphereGeo, material, 10.0f, &identity);
            PxRigidBody_setAngularDamping_mut((PxRigidBody*)sphere, 0.5f);
            scene->AddActorMut((PxActor*)sphere, null);
        }

        public override void StepSimulation()
        {
            //scene->SimulateMut(Time.Timer.FixedUpdateDelta, null, null, 0, true);
            //uint error = 0;
            //if (scene->FetchResultsMut(true, &error))
            //{
            //    foreach (var physComp in this)
            //    {
            //        if (physComp.RigidBody is not PhysRigidActor actor)
            //            continue;

            //        var pose = PxRigidActor_getGlobalPose(actor.Actor);
            //        var pos = pose.p;
            //        var rot = pose.q;
            //    }
            //}
        }

        public override void Destroy()
        {
            PxScene_release_mut(scene);
            PxDefaultCpuDispatcher_release_mut((PxDefaultCpuDispatcher*)dispatcher);
            PxPhysics_release_mut(physics);
        }
    }

    public unsafe class PhysRigidActor : AbstractRigidBody
    {
        private unsafe PxRigidActor* _actor;
        public PxRigidActor* Actor
        {
            get => _actor;
            set => _actor = value;
        }

        public override void GetTransform(out Vector3 position, out Quaternion rotation)
        {
            var pose = PxRigidActor_getGlobalPose(_actor);
            position = new Vector3(pose.p.x, pose.p.y, pose.p.z);
            rotation = new Quaternion(pose.q.x, pose.q.y, pose.q.z, pose.q.w);
        }

        public void SetGlobalPose(PxTransform pose)
        {
            PxRigidActor_setGlobalPose_mut(_actor, &pose, true);
        }

        public override void SetTransform(Vector3 position, Quaternion rotation)
        {
            var pose = new PxTransform 
            {
                p = new PxVec3 
                {
                    x = position.X, 
                    y = position.Y, 
                    z = position.Z
                }, 
                q = new PxQuat 
                {
                    x = rotation.X,
                    y = rotation.Y,
                    z = rotation.Z,
                    w = rotation.W
                }
            };
            PxRigidActor_setGlobalPose_mut(_actor, &pose, true);
        }
    }
}