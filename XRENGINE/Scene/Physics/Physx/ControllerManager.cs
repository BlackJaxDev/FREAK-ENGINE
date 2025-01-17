﻿using MagicPhysX;
using System.Numerics;
using System.Runtime.InteropServices;
using XREngine.Data;
using XREngine.Data.Core;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class Obstacle(PxObstacle* obstaclePtr) : XRBase
    {
        public PxObstacle* ObstaclePtr { get; } = obstaclePtr;
    }
    public unsafe class ControllerManager : XRBase
    {
        public PxControllerFilterCallback* ControllerFilterCallback => _controllerFilterCallbackSource.ToStructPtr<PxControllerFilterCallback>();
        public PxQueryFilterCallback* QueryFilterCallback => _queryFilterCallbackSource.ToStructPtr<PxQueryFilterCallback>();
        public PxControllerFilters* ControllerFilters => _controllerFiltersSource.ToStructPtr<PxControllerFilters>();
        public PxFilterData* FilterData => _filterDataSource.ToStructPtr<PxFilterData>();

        private readonly DataSource _controllerFilterCallbackSource;
        private readonly DataSource _queryFilterCallbackSource;
        private readonly DataSource _controllerFiltersSource;
        private readonly DataSource _filterDataSource;

        private void Destructor() { }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DelDestructor();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate PxQueryHitType DelPreFilterCallback(PxFilterData* filterData, PxShape* shape, PxRigidActor* actor, PxHitFlags* queryFlags);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate PxQueryHitType DelPostFilterCallback(PxFilterData* filterData, PxQueryHit* hit, PxShape* shape, PxRigidActor* actor);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate bool DelFilterControllerCollision(PxController* a, PxController* b);

        private readonly DelDestructor? DestructorInstance = null;
        private readonly DelPreFilterCallback? PreFilterCallbackInstance = null;
        private readonly DelPostFilterCallback? PostFilterCallbackInstance = null;
        private readonly DelFilterControllerCollision? FilterControllerCollisionInstance = null;

        public PxQueryFlags QueryFlags
        {
            get => ControllerFilters->mFilterFlags;
            set => ControllerFilters->mFilterFlags = value;
        }

        public ControllerManager(PxControllerManager* manager)
        {
            ControllerManagerPtr = manager;

            DestructorInstance = Destructor;
            PreFilterCallbackInstance = PreFilterCallback;
            PostFilterCallbackInstance = PostFilterCallback;
            FilterControllerCollisionInstance = FilterControllerCollision;

            _controllerFilterCallbackSource = DataSource.FromStruct(new PxControllerFilterCallback()
            {
                vtable_ = PhysxScene.Native.CreateVTable(DestructorInstance, FilterControllerCollisionInstance)
            });
            _queryFilterCallbackSource = DataSource.FromStruct(new PxQueryFilterCallback()
            {
                vtable_ = PhysxScene.Native.CreateVTable(PreFilterCallbackInstance, PostFilterCallbackInstance, DestructorInstance)
            });

            CreateObstacleContext();

            PxFilterData filterData = PxFilterData_new_2(0, 0, 0, 0);
            _filterDataSource = DataSource.FromStruct(filterData);

            var filter = PxControllerFilters_new(FilterData, QueryFilterCallback, ControllerFilterCallback);
            filter.mFilterFlags = PxQueryFlags.Static | PxQueryFlags.Dynamic | PxQueryFlags.Prefilter | PxQueryFlags.Postfilter;
            _controllerFiltersSource = DataSource.FromStruct(filter);

            //SetTessellation(true, 1.0f);
        }

        public PxControllerManager* ControllerManagerPtr { get; }

        public PhysxScene Scene => PhysxScene.Scenes[(nint)ControllerManagerPtr->GetScene()];

        public Dictionary<nint, Controller> Controllers { get; } = [];

        public uint ControllerCount => ControllerManagerPtr->GetNbControllers();
        public Controller[] GetControllers()
        {
            uint count = ControllerCount;
            Controller[] controllers = new Controller[count];
            for (uint i = 0; i < count; i++)
                controllers[i] = Controllers[(nint)ControllerManagerPtr->GetControllerMut(i)];
            return controllers;
        }
        public BoxController CreateAABBController(
            Vector3 position,
            Vector3 upDirection,
            float slopeLimit,
            float invisibleWallHeight,
            float maxJumpHeight,
            float contactOffset,
            float stepOffset,
            float density,
            float scaleCoeff,
            float volumeGrowth,
            PxControllerNonWalkableMode nonWalkableMode,
            PhysxMaterial material,
            byte clientID,
            void* userData,
            float halfHeight,
            float halfSideExtent,
            float halfForwardExtent)
        {
            PxBoxControllerDesc* desc = PxBoxControllerDesc_new_alloc();
            //desc->SetToDefaultMut();
            desc->halfHeight = halfHeight;
            desc->halfSideExtent = halfSideExtent;
            desc->halfForwardExtent = halfForwardExtent;
            desc->halfHeight = halfHeight;

            var genericDesc = (PxControllerDesc*)desc;
            var controller = new BoxController();
            SetGenericControllerParams(
                position,
                upDirection,
                slopeLimit,
                invisibleWallHeight,
                maxJumpHeight,
                contactOffset,
                stepOffset,
                density,
                scaleCoeff,
                volumeGrowth,
                controller.UserControllerHitReport,
                null,//controller.ControllerBehaviorCallback, //TODO: doesn't work right now, access violation
                nonWalkableMode,
                material,
                clientID,
                userData,
                genericDesc);

            bool valid = desc->IsValid();
            if (!valid)
                throw new Exception("Invalid box controller description");

            controller.BoxControllerPtr = (PxBoxController*)ControllerManagerPtr->CreateControllerMut(genericDesc);
            Controllers.Add((nint)controller.ControllerPtr, controller);
            return controller;
        }

        public CapsuleController CreateCapsuleController(
            Vector3 position,
            Vector3 upDirection,
            float slopeLimit,
            float invisibleWallHeight,
            float maxJumpHeight,
            float contactOffset,
            float stepOffset,
            float density,
            float scaleCoeff,
            float volumeGrowth,
            PxControllerNonWalkableMode nonWalkableMode,
            PhysxMaterial material,
            byte clientID,
            void* userData,
            float radius,
            float height,
            PxCapsuleClimbingMode climbingMode)
        {
            var controller = new CapsuleController();

            PxCapsuleControllerDesc* desc = PxCapsuleControllerDesc_new_alloc();
            desc->SetToDefaultMut();
            desc->radius = radius;
            desc->height = height;
            desc->climbingMode = climbingMode;
            
            var genericDesc = (PxControllerDesc*)desc;
            SetGenericControllerParams(
                position,
                upDirection,
                slopeLimit,
                invisibleWallHeight,
                maxJumpHeight,
                contactOffset,
                stepOffset,
                density,
                scaleCoeff,
                volumeGrowth,
                controller.UserControllerHitReport,
                null,//controller.ControllerBehaviorCallback, //TODO: doesn't work right now, access violation
                nonWalkableMode,
                material,
                clientID,
                userData,
                genericDesc);

            bool valid = desc->IsValid();
            if (!valid)
                throw new Exception("Invalid capsule controller description");

            controller.CapsuleControllerPtr = (PxCapsuleController*)ControllerManagerPtr->CreateControllerMut(genericDesc);
            Controllers.Add((nint)controller.ControllerPtr, controller);
            return controller;
        }

        public delegate void DelOnControllerHit(PxControllersHit* hit);
        public delegate void DelOnShapeHit(PxControllerShapeHit* hit);
        public delegate void DelOnObstacleHit(PxControllerObstacleHit* hit);

        public delegate PxControllerBehaviorFlags DelGetBehaviorFlagsShape(PxShape* shape, PxActor* actor);
        public delegate PxControllerBehaviorFlags DelGetBehaviorFlagsObstacle(PxObstacle* obstacle);
        public delegate PxControllerBehaviorFlags DelGetBehaviorFlagsController(PxController* controller);

        private void SetGenericControllerParams(
            Vector3 position,
            Vector3 upDirection,
            float slopeLimit,
            float invisibleWallHeight,
            float maxJumpHeight,
            float contactOffset,
            float stepOffset,
            float density,
            float scaleCoeff,
            float volumeGrowth,
            PxUserControllerHitReport* reportCallbackHit,
            PxControllerBehaviorCallback* behaviorCallback,
            PxControllerNonWalkableMode nonWalkableMode,
            PhysxMaterial material,
            byte clientID,
            void* userData,
            PxControllerDesc* genericDesc)
        {
            PxExtendedVec3 pos = PxExtendedVec3_new_1(position.X, position.Y, position.Z);
            genericDesc->position = pos;
            genericDesc->upDirection = upDirection;
            genericDesc->slopeLimit = slopeLimit;
            genericDesc->invisibleWallHeight = invisibleWallHeight;
            genericDesc->maxJumpHeight = maxJumpHeight;
            genericDesc->contactOffset = contactOffset;
            genericDesc->stepOffset = stepOffset;
            genericDesc->density = density;
            genericDesc->scaleCoeff = scaleCoeff;
            genericDesc->volumeGrowth = volumeGrowth;
            genericDesc->reportCallback = reportCallbackHit;
            genericDesc->behaviorCallback = behaviorCallback;
            genericDesc->nonWalkableMode = nonWalkableMode;
            genericDesc->material = material.MaterialPtr;
            genericDesc->clientID = clientID;
            genericDesc->userData = userData;
        }

        public void DestroyAllControllers()
        {
            foreach (var controller in Controllers.Values)
                controller.Release();
            ControllerManagerPtr->PurgeControllersMut();
            Controllers.Clear();
        }

        public PxRenderBuffer* RenderBuffer
            => ControllerManagerPtr->GetRenderBufferMut();

        public void SetDebugRenderingFlags(PxControllerDebugRenderFlags flags)
            => ControllerManagerPtr->SetDebugRenderingFlagsMut(flags);

        public Dictionary<nint, ObstacleContext> ObstacleContexts { get; } = [];
        public uint ObstacleContextCount => ControllerManagerPtr->GetNbObstacleContexts();

        public ObstacleContext[] GetObstacleContexts()
        {
            uint count = ObstacleContextCount;
            ObstacleContext[] contexts = new ObstacleContext[count];
            for (uint i = 0; i < count; i++)
                contexts[i] = ObstacleContexts[(nint)ControllerManagerPtr->GetObstacleContextMut(i)];
            return contexts;
        }
        public ObstacleContext GetObstacleContext(uint index)
            => ObstacleContexts[(nint)ControllerManagerPtr->GetObstacleContextMut(index)];
        public ObstacleContext CreateObstacleContext()
        {
            PxObstacleContext* context = ControllerManagerPtr->CreateObstacleContextMut();
            var obstacleContext = new ObstacleContext(context);
            ObstacleContexts.Add((nint)context, obstacleContext);
            return obstacleContext;
        }
        public void ReleaseObstacleContext(ObstacleContext context)
        {
            context.Release();
            ObstacleContexts.Remove((nint)context.ContextPtr);
        }
        public void DestroyAllObstacleContexts()
        {
            foreach (var context in ObstacleContexts.Values)
                context.Release();
            ObstacleContexts.Clear();
        }

        public void ComputeInteractions(float elapsedTime)
            => ControllerManagerPtr->ComputeInteractionsMut(elapsedTime, ControllerFilterCallback);
        public void SetTessellation(bool enabled, float maxEdgeLength)
            => ControllerManagerPtr->SetTessellationMut(enabled, maxEdgeLength);
        public void SetOverlapRecoveryModule(bool enabled)
            => ControllerManagerPtr->SetOverlapRecoveryModuleMut(enabled);
        public void SetPreciseSweeps(bool enabled)
            => ControllerManagerPtr->SetPreciseSweepsMut(enabled);
        public void SetPreventVerticalSlidingAgainstCeiling(bool enabled)
            => ControllerManagerPtr->SetPreventVerticalSlidingAgainstCeilingMut(enabled);

        /// <summary>
        /// Shift the origin of the character controllers and obstacle objects by the specified vector.
        /// The positions of all character controllers, obstacle objects and the corresponding data structures
        /// will get adjusted to reflect the shifted origin location
        /// (the shift vector will get subtracted from all character controller and obstacle object positions).
        /// 
        /// It is the user’s responsibility to keep track of the summed total origin shift 
        /// and adjust all input/output to/from PhysXCharacterKinematic accordingly.
        /// 
        /// This call will not automatically shift the PhysX scene and its objects.
        /// You need to call PxScene::shiftOrigin() seperately to keep the systems in sync.
        /// </summary>
        /// <param name="shift"></param>
        public void ShiftOrigin(Vector3 shift)
        {
            PxVec3 s = shift;
            ControllerManagerPtr->ShiftOriginMut(&s);
        }

        internal PxQueryHitType PreFilterCallback(PxFilterData* filterData, PxShape* shape, PxRigidActor* actor, PxHitFlags* queryFlags)
        {
            //var a = PhysxRigidActor.Get(actor);
            //var s = PhysxShape.Get(shape);
            //if (a is null && s is null)
            //    return PxQueryHitType.Block;
            return PxQueryHitType.Block;
        }
        internal PxQueryHitType PostFilterCallback(PxFilterData* filterData, PxQueryHit* hit, PxShape* shape, PxRigidActor* actor)
        {
            return PxQueryHitType.Block;
        }
        /// <summary>
        /// Dedicated filtering callback for CCT vs CCT.
        /// This controls collisions between CCTs (one CCT vs anoter CCT).
        /// To make each CCT collide against all other CCTs, just return true - or simply avoid defining a callback.
        /// To make each CCT freely go through all other CCTs, just return false.
        /// Otherwise create a custom filtering logic in this callback.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        internal bool FilterControllerCollision(PxController* a, PxController* b)
        {
            return false;
        }
    }
}