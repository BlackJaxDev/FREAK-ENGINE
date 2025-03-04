﻿using Extensions;
using MagicPhysX;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XREngine.Components;
using XREngine.Data;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Rendering.Physics.Physx.Joints;
using XREngine.Scene;
using static MagicPhysX.NativeMethods;
using Quaternion = System.Numerics.Quaternion;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe partial class PhysxScene : AbstractPhysicsScene
    {
        private static PxFoundation* _foundationPtr;
        public static PxFoundation* FoundationPtr => _foundationPtr;

        private static PxPhysics* _physicsPtr;
        public static PxPhysics* PhysicsPtr => _physicsPtr;

        public static Dictionary<nint, PhysxScene> Scenes { get; } = [];

        static PhysxScene()
        {
            Init();
        }

        public static void Init()
        {
            _foundationPtr = physx_create_foundation();
            _physicsPtr = physx_create_physics(_foundationPtr);
        }
        public static void Release()
        {
            _physicsPtr->ReleaseMut();
        }

        public static readonly PxVec3 DefaultGravity = new() { x = 0.0f, y = -9.81f, z = 0.0f };

        private PxCpuDispatcher* _dispatcher;
        private PxScene* _scene;

        //public PxPhysics* PhysicsPtr => _scene->GetPhysicsMut();

        public PxScene* ScenePtr => _scene;
        public PxCpuDispatcher* DispatcherPtr => _dispatcher;

        public override Vector3 Gravity
        {
            get => _scene->GetGravity();
            set
            {
                PxVec3 g = value;
                _scene->SetGravityMut(&g);
            }
        }

        public override void Destroy()
        {
            if (_scene is not null)
            {
                Scenes.Remove((nint)_scene);
                _scene->ReleaseMut();
            }

            if (_dispatcher is not null)
                ((PxDefaultCpuDispatcher*)_dispatcher)->ReleaseMut();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CustomFilterShaderDelegate(FilterShaderCallbackInfo* callbackInfo, PxFilterFlags filterFlags);

        public CustomFilterShaderDelegate CustomFilterShaderInstance = CustomFilterShader;
        static void CustomFilterShader(FilterShaderCallbackInfo* callbackInfo, PxFilterFlags filterFlags)
        {
            callbackInfo->pairFlags[0] = 
                PxPairFlags.ContactDefault |
                PxPairFlags.NotifyTouchFound |
                PxPairFlags.SolveContact |
                PxPairFlags.DetectCcdContact |
                PxPairFlags.DetectDiscreteContact;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CollisionCallback(IntPtr userData, PxContactPairHeader pairHeader, PxContactPair contacts, uint flags);

        public CollisionCallback OnContactDelegateInstance = OnContact;
        static void OnContact(IntPtr userData, PxContactPairHeader pairHeader, PxContactPair contacts, uint flags)
        {
            //Debug.Out($"Contact: {pairHeader.nbPairs}");
        }

        public override void Initialize()
        {
            //PxPvd pvd;
            //if (_physics->PhysPxInitExtensions(&pvd))
            //{

            //}
            var scale = _physicsPtr->GetTolerancesScale();
            //scale->length = 100;
            //scale->speed = 980;
            var sceneDesc = PxSceneDesc_new(scale);
            sceneDesc.gravity = DefaultGravity;
            sceneDesc.cpuDispatcher = _dispatcher = (PxCpuDispatcher*)phys_PxDefaultCpuDispatcherCreate(4, null, PxDefaultCpuDispatcherWaitForWorkMode.WaitForWork, 0);

            var simEventCallback = new SimulationEventCallbackInfo
            {
                collision_callback = (delegate* unmanaged[Cdecl]<void*, PxContactPairHeader*, PxContactPair*, uint, void>)Marshal.GetFunctionPointerForDelegate(OnContactDelegateInstance).ToPointer()
            };
            sceneDesc.simulationEventCallback = create_simulation_event_callbacks(&simEventCallback);

            //sceneDesc.filterShader = get_default_simulation_filter_shader();
            var filterShaderCallback = (delegate* unmanaged[Cdecl]<FilterShaderCallbackInfo*, PxFilterFlags>)Marshal.GetFunctionPointerForDelegate(CustomFilterShaderInstance).ToPointer();
            enable_custom_filter_shader(&sceneDesc, filterShaderCallback, 1u);

            sceneDesc.flags |= PxSceneFlags.EnableCcd | PxSceneFlags.EnableGpuDynamics;
            sceneDesc.broadPhaseType = PxBroadPhaseType.Gpu;
            //sceneDesc.gpuDynamicsConfig = new PxgDynamicsMemoryConfig()
            //{
                
            //};
            _scene = _physicsPtr->CreateSceneMut(&sceneDesc);
            Scenes.Add((nint)_scene, this);

            VisualizeEnabled = true;
            VisualizeWorldAxes = true;
            VisualizeBodyAxes = true;
            VisualizeBodyMassAxes = false;
            VisualizeBodyLinearVelocity = true;
            VisualizeBodyAngularVelocity = true;
            VisualizeContactPoint = true;
            VisualizeContactNormal = true;
            VisualizeContactError = false;
            VisualizeContactForce = false;
            VisualizeActorAxes = true;
            VisualizeCollisionAabbs = true;
            VisualizeCollisionShapes = true;
            VisualizeCollisionAxes = true;
            VisualizeCollisionCompounds = false;
            VisualizeCollisionFaceNormals = false;
            VisualizeCollisionEdges = false;
            VisualizeCollisionStatic = false;
            VisualizeCollisionDynamic = false;
            VisualizeJointLocalFrames = false;
            VisualizeJointLimits = false;
            VisualizeCullBox = false;
            VisualizeMbpRegions = false;
            VisualizeSimulationMesh = false;
            VisualizeSdf = false;
        }

        public DataSource? _scratchBlock = new(32000, true);

        public override void StepSimulation()
        {
            Simulate(Engine.Time.Timer.FixedUpdateDelta, null, true);
            if (!FetchResults(true, out uint error))
            {
                Debug.Out($"PhysX FetchResults error: {error}");
                return;
            }
            if (VisualizeEnabled)
                PopulateDebugBuffers();
            NotifySimulationStepped();
        }

        private XRMeshRenderer? _debugPointsRenderer = null;
        private XRMeshRenderer? _debugLinesRenderer = null;
        private XRMeshRenderer? _debugTrianglesRenderer = null;

        private void PopulateDebugBuffers()
        {
            var rb = RenderBuffer;

            _debugPointCount = rb->GetNbPoints();
            _debugLineCount = rb->GetNbLines();
            _debugTriangleCount = rb->GetNbTriangles();

            _debugPoints = rb->GetPoints();
            _debugLines = rb->GetLines();
            _debugTriangles = rb->GetTriangles();
            CreateOrResizeDebugBuffers();
            PopulateBuffers();
        }

        private void PopulateBuffers()
        {
            var destPoints = (float*)_debugPointsBuffer!.Address;
            var destLines = (float*)_debugLinesBuffer!.Address;
            var destTriangles = (float*)_debugTrianglesBuffer!.Address;
            void SetPointsAt(int i)
            {
                if (i >= _debugPointCount)
                    return;
                
                var point = _debugPoints[i];
                var pos = point.pos;
                var color = ToColorF4(point.color);
                var x = i * 8;
                destPoints[x + 0] = pos.x;
                destPoints[x + 1] = pos.y;
                destPoints[x + 2] = pos.z;
                destPoints[x + 3] = 0.0f;
                destPoints[x + 4] = color.R;
                destPoints[x + 5] = color.G;
                destPoints[x + 6] = color.B;
                destPoints[x + 7] = color.A;
            }
            void SetLinesAt(int i)
            {
                var line = _debugLines[i];
                var pos0 = line.pos0;
                var pos1 = line.pos1;
                var color = ToColorF4(line.color0);
                var x = i * 12;
                destLines[x + 0] = pos0.x;
                destLines[x + 1] = pos0.y;
                destLines[x + 2] = pos0.z;
                destLines[x + 3] = 0.0f;
                destLines[x + 4] = pos1.x;
                destLines[x + 5] = pos1.y;
                destLines[x + 6] = pos1.z;
                destLines[x + 7] = 0.0f;
                destLines[x + 8] = color.R;
                destLines[x + 9] = color.G;
                destLines[x + 10] = color.B;
                destLines[x + 11] = color.A;
            }
            void SetTrianglesAt(int i)
            {
                if (i >= _debugTriangleCount)
                    return;
                
                var triangle = _debugTriangles[i];
                var pos0 = triangle.pos0;
                var pos1 = triangle.pos1;
                var pos2 = triangle.pos2;
                var color = ToColorF4(triangle.color0);
                var x = i * 16;
                destTriangles[x + 0] = pos0.x;
                destTriangles[x + 1] = pos0.y;
                destTriangles[x + 2] = pos0.z;
                destTriangles[x + 3] = 0.0f;
                destTriangles[x + 4] = pos1.x;
                destTriangles[x + 5] = pos1.y;
                destTriangles[x + 6] = pos1.z;
                destTriangles[x + 7] = 0.0f;
                destTriangles[x + 8] = pos2.x;
                destTriangles[x + 9] = pos2.y;
                destTriangles[x + 10] = pos2.z;
                destTriangles[x + 11] = 0.0f;
                destTriangles[x + 12] = color.R;
                destTriangles[x + 13] = color.G;
                destTriangles[x + 14] = color.B;
                destTriangles[x + 15] = color.A;
            }

            ////Set sequential
            //for (int i = 0; i < _debugPointCount; i++)
            //    SetPointsAt(i);
            //for (int i = 0; i < _debugLineCount; i++)
            //    SetLinesAt(i);
            //for (int i = 0; i < _debugTriangleCount; i++)
            //    SetTrianglesAt(i);

            Task t0 = Task.Run(() => Parallel.For(0, (int)_debugPointCount, SetPointsAt));
            Task t1 = Task.Run(() => Parallel.For(0, (int)_debugLineCount, SetLinesAt));
            Task t2 = Task.Run(() => Parallel.For(0, (int)_debugTriangleCount, SetTrianglesAt));
            Task.WaitAll(t0, t1, t2);
        }

        private void CreateOrResizeDebugBuffers()
        {
            if (_debugPointsBuffer is null)
                UpdatePoints(_debugPointCount);

            if (_debugLinesBuffer is null)
                UpdateLines(_debugLineCount);

            if (_debugTrianglesBuffer is null)
                UpdateTriangles(_debugTriangleCount);

            _debugPointsRenderer?.Material?.SetFloat(0, PointSize);
            _debugLinesRenderer?.Material?.SetFloat(0, LineWidth);
        }

        private void UpdateTriangles(uint count)
        {
            _debugTrianglesRenderer ??= new XRMeshRenderer(CreateDebugMesh(), CreateDebugTriangleMaterial());
            //16 floats: 3 for position0, 1 for padding, 3 for position1, 1 for padding, 3 for position2, 1 for padding, 4 for color
            if (_debugTrianglesBuffer is not null)
                _debugTrianglesBuffer.Resize(count, true, true);
            else
            {
                _debugTrianglesBuffer = new XRDataBuffer(
                    "TrianglesBuffer",
                    EBufferTarget.ShaderStorageBuffer,
                    count,
                    EComponentType.Float,
                    16,
                    false,
                    false,
                    true)
                {
                    BindingIndexOverride = 0,
                    Usage = EBufferUsage.StreamDraw,
                };
                _debugTrianglesRenderer.Buffers?.Add(_debugTrianglesBuffer.BindingName, _debugTrianglesBuffer);
                _debugTrianglesRenderer.SettingUniforms += _debugTrianglesRenderer_SettingUniforms;
                _debugTrianglesRenderer.GenerateAsync = false;
                //_debugTrianglesBuffer.PushData();
            }
        }

        private void _debugTrianglesRenderer_SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            _debugTrianglesBuffer?.PushSubData();
        }

        private void UpdateLines(uint count)
        {
            _debugLinesRenderer ??= new XRMeshRenderer(CreateDebugMesh(), CreateDebugLineMaterial());
            //12 floats: 3 for position0, 1 for padding, 3 for position1, 1 for padding, 4 for color
            if (_debugLinesBuffer is not null)
                _debugLinesBuffer.Resize(count * 3, true, true);
            else
            {
                _debugLinesBuffer = new XRDataBuffer(
                    "LinesBuffer",
                    EBufferTarget.ShaderStorageBuffer,
                    count * 3,
                    EComponentType.Float,
                    4,
                    false,
                    false,
                    true)
                {
                    BindingIndexOverride = 0,
                    Usage = EBufferUsage.StreamDraw,
                };
                _debugLinesRenderer.Buffers?.Add(_debugLinesBuffer.BindingName, _debugLinesBuffer);
                _debugLinesRenderer.SettingUniforms += _debugLinesRenderer_SettingUniforms;
                _debugLinesRenderer.GenerateAsync = false;
                //_debugLinesBuffer.PushData();
            }
        }

        private void _debugLinesRenderer_SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            _debugLinesBuffer?.PushSubData();
        }

        private void UpdatePoints(uint count)
        {
            _debugPointsRenderer ??= new XRMeshRenderer(CreateDebugMesh(), CreateDebugPointMaterial());
            //8 floats: 3 for position, 1 for padding, 4 for color
            if (_debugPointsBuffer is not null)
                _debugPointsBuffer.Resize(count, true, true);
            else
            {
                _debugPointsBuffer = new XRDataBuffer(
                    "PointsBuffer",
                    EBufferTarget.ShaderStorageBuffer,
                    count,
                    EComponentType.Float,
                    8,
                    false,
                    false,
                    true)
                {
                    BindingIndexOverride = 0,
                    Usage = EBufferUsage.StreamDraw,
                };
                _debugPointsRenderer.Buffers.Add(_debugPointsBuffer.BindingName, _debugPointsBuffer);
                _debugPointsRenderer.SettingUniforms += _debugPointsRenderer_SettingUniforms;
                _debugPointsRenderer.GenerateAsync = false;
                //_debugPointsBuffer.PushData();
            }
        }

        private void _debugPointsRenderer_SettingUniforms(XRRenderProgram vertexProgram, XRRenderProgram materialProgram)
        {
            _debugPointsBuffer?.PushSubData();
        }

        private float _pointSize = 10f;
        public float PointSize
        {
            get => _pointSize;
            set => SetField(ref _pointSize, value);
        }

        private float _lineWidth = 10f;
        public float LineWidth
        {
            get => _lineWidth;
            set => SetField(ref _lineWidth, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                //case nameof(VisualizeEnabled):
                //    break;
                case nameof(PointSize):
                    _debugPointsRenderer?.Material?.SetFloat(0, PointSize);
                    break;
                case nameof(LineWidth):
                    _debugLinesRenderer?.Material?.SetFloat(0, LineWidth);
                    break;
            }
        }

        private static XRMaterial? CreateDebugPointMaterial()
        {
            XRShader vertShader = ShaderHelper.LoadEngineShader(Path.Combine("Common", "Debug", "InstancedDebugPrimitive.vs"), EShaderType.Vertex);
            XRShader geomShader = ShaderHelper.LoadEngineShader(Path.Combine("Common", "Debug", "PointInstance.gs"), EShaderType.Geometry);
            XRShader fragShader = ShaderHelper.LoadEngineShader(Path.Combine("Common", "Debug", "InstancedDebugPrimitivePoint.fs"), EShaderType.Fragment);
            ShaderVar[] vars = [new ShaderFloat(1f, "PointSize")];
            var mat = new XRMaterial(vars, vertShader, geomShader, fragShader);
            mat.RenderOptions.RequiredEngineUniforms = EUniformRequirements.Camera;
            mat.RenderOptions.CullMode = ECullMode.None;
            mat.EnableTransparency();
            return mat;
        }
        private static XRMaterial? CreateDebugLineMaterial()
        {
            XRShader vertShader = ShaderHelper.LoadEngineShader(Path.Combine("Common", "Debug", "InstancedDebugPrimitive.vs"), EShaderType.Vertex);
            XRShader geomShader = ShaderHelper.LoadEngineShader(Path.Combine("Common", "Debug", "LineInstance.gs"), EShaderType.Geometry);
            XRShader fragShader = ShaderHelper.LoadEngineShader(Path.Combine("Common", "Debug", "InstancedDebugPrimitive.fs"), EShaderType.Fragment);
            ShaderVar[] vars = [new ShaderFloat(10f, "LineWidth")];
            var mat = new XRMaterial(vars, vertShader, geomShader, fragShader);
            mat.RenderOptions.RequiredEngineUniforms = EUniformRequirements.Camera;
            mat.RenderOptions.CullMode = ECullMode.None;
            mat.EnableTransparency();
            return mat;
        }
        private static XRMaterial? CreateDebugTriangleMaterial()
        {
            XRShader vertShader = ShaderHelper.LoadEngineShader(Path.Combine("Common", "Debug", "InstancedDebugPrimitive.vs"), EShaderType.Vertex);
            XRShader geomShader = ShaderHelper.LoadEngineShader(Path.Combine("Common", "Debug", "TriangleInstance.gs"), EShaderType.Geometry);
            XRShader fragShader = ShaderHelper.LoadEngineShader(Path.Combine("Common", "Debug", "InstancedDebugPrimitive.fs"), EShaderType.Fragment);
            ShaderVar[] vars = [];
            var mat = new XRMaterial(vars, vertShader, geomShader, fragShader);
            mat.RenderOptions.RequiredEngineUniforms = EUniformRequirements.Camera;
            mat.RenderOptions.CullMode = ECullMode.None;
            mat.EnableTransparency();
            return mat;
        }

        private static XRMesh? CreateDebugMesh()
            => new(new Vertex[] { new Vertex(Vector3.Zero) });

        private XRDataBuffer? _debugPointsBuffer = null;
        private XRDataBuffer? _debugLinesBuffer = null;
        private XRDataBuffer? _debugTrianglesBuffer = null;

        public override void DebugRender()
        {
            if (_debugPointCount > 0)
                _debugPointsRenderer?.Render(null, _debugPointCount);
            
            if (_debugLineCount > 0)
                _debugLinesRenderer?.Render(null, _debugLineCount);
            
            if (_debugTriangleCount > 0)
                _debugTrianglesRenderer?.Render(null, _debugTriangleCount);
        }

        private PxDebugPoint* _debugPoints;
        private PxDebugLine* _debugLines;
        private PxDebugTriangle* _debugTriangles;

        private uint _debugPointCount = 0;
        private uint _debugLineCount = 0;
        private uint _debugTriangleCount = 0;

        private static ColorF4 ToColorF4(uint c) => new(
            ((c >> 00) & 0xFF) / 255.0f,
            ((c >> 08) & 0xFF) / 255.0f,
            ((c >> 16) & 0xFF) / 255.0f,
            ((c >> 24) & 0xFF) / 255.0f);

        public void Simulate(float elapsedTime, PxBaseTask* completionTask, bool controlSimulation)
            => _scene->SimulateMut(elapsedTime, completionTask, _scratchBlock is null ? null : _scratchBlock.Address.Pointer, _scratchBlock?.Length ?? 0, controlSimulation);
        public void Collide(float elapsedTime, PxBaseTask* completionTask, bool controlSimulation)
            => _scene->CollideMut(elapsedTime, completionTask, _scratchBlock is null ? null : _scratchBlock.Address.Pointer, _scratchBlock?.Length ?? 0, controlSimulation);
        public void FlushSimulation(bool sendPendingReports)
            => _scene->FlushSimulationMut(sendPendingReports);
        public void Advance(PxBaseTask* completionTask)
            => _scene->AdvanceMut(completionTask);
        public void FetchCollision(bool block)
            => _scene->FetchCollisionMut(block);
        public bool FetchResults(bool block, out uint errorState)
        {
            uint es = 0;
            bool result = _scene->FetchResultsMut(block, &es);
            errorState = es;
            return result;
        }
        public bool FetchResultsStart(out PxContactPairHeader[] contactPairs, bool block)
        {
            PxContactPairHeader* ptr;
            uint numPairs;
            bool result = _scene->FetchResultsStartMut(&ptr, &numPairs, block);
            contactPairs = new PxContactPairHeader[numPairs];
            for (int i = 0; i < numPairs; i++)
                contactPairs[i] = *ptr++;
            return result;
        }
        public void ProcessCallbacks(PxBaseTask* continuation)
            => _scene->ProcessCallbacksMut(continuation);
        public void FetchResultsFinish(out uint errorState)
        {
            uint es = 0;
            _scene->FetchResultsFinishMut(&es);
            errorState = es;
        }

        public bool CheckResults(bool block)
            => _scene->CheckResultsMut(block);

        public void FetchResultsParticleSystem()
            => _scene->FetchResultsParticleSystemMut();

        public uint Timestamp
            => _scene->GetTimestamp();

        public PxBroadPhaseCallback* BroadPhaseCallbackPtr
        {
            get => _scene->GetBroadPhaseCallback();
            set => _scene->SetBroadPhaseCallbackMut(value);
        }
        public PxCCDContactModifyCallback* CcdContactModifyCallbackPtr
        {
            get => _scene->GetCCDContactModifyCallback();
            set => _scene->SetCCDContactModifyCallbackMut(value);
        }
        public PxContactModifyCallback* ContactModifyCallbackPtr
        {
            get => _scene->GetContactModifyCallback();
            set => _scene->SetContactModifyCallbackMut(value);
        }
        public PxSimulationEventCallback* SimulationEventCallbackPtr
        {
            get => _scene->GetSimulationEventCallback();
            set => _scene->SetSimulationEventCallbackMut(value);
        }

        public byte CreateClient()
            => _scene->CreateClientMut();

        public struct FilterShader
        {
            public void* data;
            public uint dataSize;
        }
        public FilterShader FilterShaderData
        {
            get
            {
                void* data = _scene->GetFilterShaderData();
                uint dataSize = _scene->GetFilterShaderDataSize();
                return new FilterShader { data = data, dataSize = dataSize };
            }
            set
            {
                _scene->SetFilterShaderDataMut(value.data, value.dataSize);
            }
        }

        public void AddActor(PhysxActor actor)
        {
            _scene->AddActorMut(actor.ActorPtr, null);
            actor.OnAddedToScene(this);
        }
        public void AddActors(PhysxActor[] actors)
        {
            PxActor** ptrs = stackalloc PxActor*[actors.Length];
            for (int i = 0; i < actors.Length; i++)
                ptrs[i] = actors[i].ActorPtr;
            _scene->AddActorsMut(ptrs, (uint)actors.Length);
            foreach (var actor in actors)
                actor.OnAddedToScene(this);
        }
        public void RemoveActor(PhysxActor actor, bool wakeOnLostTouch = false)
        {
            _scene->RemoveActorMut(actor.ActorPtr, wakeOnLostTouch);
            actor.OnRemovedFromScene(this);
        }
        public void RemoveActors(PhysxActor[] actors, bool wakeOnLostTouch = false)
        {
            PxActor** ptrs = stackalloc PxActor*[actors.Length];
            for (int i = 0; i < actors.Length; i++)
                ptrs[i] = actors[i].ActorPtr;
            _scene->RemoveActorsMut(ptrs, (uint)actors.Length, wakeOnLostTouch);
            foreach (var actor in actors)
                actor.OnRemovedFromScene(this);
        }

        public Dictionary<nint, PhysxShape> Shapes { get; } = [];
        public PhysxShape? GetShape(PxShape* ptr)
            => Shapes.TryGetValue((nint)ptr, out var shape) ? shape : null;

        #region Joints

        public Dictionary<nint, PhysxJoint> Joints { get; } = [];
        public PhysxJoint? GetJoint(PxJoint* ptr)
            => Joints.TryGetValue((nint)ptr, out var joint) ? joint : null;

        public Dictionary<nint, PhysxJoint_Contact> ContactJoints { get; } = [];
        public PhysxJoint_Contact? GetContactJoint(PxContactJoint* ptr)
            => ContactJoints.TryGetValue((nint)ptr, out var joint) ? joint : null;
        public PhysxJoint_Contact NewContactJoint(PhysxRigidActor actor0, (Vector3 position, Quaternion rotation) localFrame0, PhysxRigidActor actor1, (Vector3 position, Quaternion rotation) localFrame1)
        {
            PxTransform pxlocalFrame0 = new() { p = localFrame0.position, q = localFrame0.rotation };
            PxTransform pxlocalFrame1 = new() { p = localFrame1.position, q = localFrame1.rotation };
            var joint = PhysicsPtr->PhysPxContactJointCreate(actor0.RigidActorPtr, &pxlocalFrame0, actor1.RigidActorPtr, &pxlocalFrame1);
            var jointObj = new PhysxJoint_Contact(joint);
            Joints.Add((nint)joint, jointObj);
            ContactJoints.Add((nint)joint, jointObj);
            return jointObj;
        }

        public Dictionary<nint, PhysxJoint_Distance> DistanceJoints { get; } = [];
        public PhysxJoint_Distance? GetDistanceJoint(PxDistanceJoint* ptr)
            => DistanceJoints.TryGetValue((nint)ptr, out var joint) ? joint : null;
        public PhysxJoint_Distance NewDistanceJoint(PhysxRigidActor actor0, (Vector3 position, Quaternion rotation) localFrame0, PhysxRigidActor actor1, (Vector3 position, Quaternion rotation) localFrame1)
        {
            PxTransform pxlocalFrame0 = new() { p = localFrame0.position, q = localFrame0.rotation };
            PxTransform pxlocalFrame1 = new() { p = localFrame1.position, q = localFrame1.rotation };
            var joint = PhysicsPtr->PhysPxDistanceJointCreate(actor0.RigidActorPtr, &pxlocalFrame0, actor1.RigidActorPtr, &pxlocalFrame1);
            var jointObj = new PhysxJoint_Distance(joint);
            Joints.Add((nint)joint, jointObj);
            DistanceJoints.Add((nint)joint, jointObj);
            return jointObj;
        }

        public Dictionary<nint, PhysxJoint_D6> D6Joints { get; } = [];
        public PhysxJoint_D6? GetD6Joint(PxD6Joint* ptr)
            => D6Joints.TryGetValue((nint)ptr, out var joint) ? joint : null;
        public PhysxJoint_D6 NewD6Joint(PhysxRigidActor actor0, (Vector3 position, Quaternion rotation) localFrame0, PhysxRigidActor actor1, (Vector3 position, Quaternion rotation) localFrame1)
        {
            PxTransform pxlocalFrame0 = new() { p = localFrame0.position, q = localFrame0.rotation };
            PxTransform pxlocalFrame1 = new() { p = localFrame1.position, q = localFrame1.rotation };
            var joint = PhysicsPtr->PhysPxD6JointCreate(actor0.RigidActorPtr, &pxlocalFrame0, actor1.RigidActorPtr, &pxlocalFrame1);
            var jointObj = new PhysxJoint_D6(joint);
            Joints.Add((nint)joint, jointObj);
            D6Joints.Add((nint)joint, jointObj);
            return jointObj;
        }

        public Dictionary<nint, PhysxJoint_Fixed> FixedJoints { get; } = [];
        public PhysxJoint_Fixed? GetFixedJoint(PxFixedJoint* ptr)
            => FixedJoints.TryGetValue((nint)ptr, out var joint) ? joint : null;
        public PhysxJoint_Fixed NewFixedJoint(PhysxRigidActor actor0, (Vector3 position, Quaternion rotation) localFrame0, PhysxRigidActor actor1, (Vector3 position, Quaternion rotation) localFrame1)
        {
            PxTransform pxlocalFrame0 = new() { p = localFrame0.position, q = localFrame0.rotation };
            PxTransform pxlocalFrame1 = new() { p = localFrame1.position, q = localFrame1.rotation };
            var joint = PhysicsPtr->PhysPxFixedJointCreate(actor0.RigidActorPtr, &pxlocalFrame0, actor1.RigidActorPtr, &pxlocalFrame1);
            var jointObj = new PhysxJoint_Fixed(joint);
            Joints.Add((nint)joint, jointObj);
            FixedJoints.Add((nint)joint, jointObj);
            return jointObj;
        }

        public Dictionary<nint, PhysxJoint_Prismatic> PrismaticJoints { get; } = [];
        public PhysxJoint_Prismatic? GetPrismaticJoint(PxPrismaticJoint* ptr)
            => PrismaticJoints.TryGetValue((nint)ptr, out var joint) ? joint : null;
        public PhysxJoint_Prismatic NewPrismaticJoint(PhysxRigidActor actor0, (Vector3 position, Quaternion rotation) localFrame0, PhysxRigidActor actor1, (Vector3 position, Quaternion rotation) localFrame1)
        {
            PxTransform pxlocalFrame0 = new() { p = localFrame0.position, q = localFrame0.rotation };
            PxTransform pxlocalFrame1 = new() { p = localFrame1.position, q = localFrame1.rotation };
            var joint = PhysicsPtr->PhysPxPrismaticJointCreate(actor0.RigidActorPtr, &pxlocalFrame0, actor1.RigidActorPtr, &pxlocalFrame1);
            var jointObj = new PhysxJoint_Prismatic(joint);
            Joints.Add((nint)joint, jointObj);
            PrismaticJoints.Add((nint)joint, jointObj);
            return jointObj;
        }

        public Dictionary<nint, PhysxJoint_Revolute> RevoluteJoints { get; } = [];
        public PhysxJoint_Revolute? GetRevoluteJoint(PxRevoluteJoint* ptr)
            => RevoluteJoints.TryGetValue((nint)ptr, out var joint) ? joint : null;
        public PhysxJoint_Revolute NewRevoluteJoint(PhysxRigidActor actor0, (Vector3 position, Quaternion rotation) localFrame0, PhysxRigidActor actor1, (Vector3 position, Quaternion rotation) localFrame1)
        {
            PxTransform pxlocalFrame0 = new() { p = localFrame0.position, q = localFrame0.rotation };
            PxTransform pxlocalFrame1 = new() { p = localFrame1.position, q = localFrame1.rotation };
            var joint = PhysicsPtr->PhysPxRevoluteJointCreate(actor0.RigidActorPtr, &pxlocalFrame0, actor1.RigidActorPtr, &pxlocalFrame1);
            var jointObj = new PhysxJoint_Revolute(joint);
            Joints.Add((nint)joint, jointObj);
            RevoluteJoints.Add((nint)joint, jointObj);
            return jointObj;
        }

        public Dictionary<nint, PhysxJoint_Spherical> SphericalJoints { get; } = [];
        public PhysxJoint_Spherical? GetSphericalJoint(PxSphericalJoint* ptr)
            => SphericalJoints.TryGetValue((nint)ptr, out var joint) ? joint : null;
        public PhysxJoint_Spherical NewSphericalJoint(PhysxRigidActor actor0, (Vector3 position, Quaternion rotation) localFrame0, PhysxRigidActor actor1, (Vector3 position, Quaternion rotation) localFrame1)
        {
            PxTransform pxlocalFrame0 = new() { p = localFrame0.position, q = localFrame0.rotation };
            PxTransform pxlocalFrame1 = new() { p = localFrame1.position, q = localFrame1.rotation };
            var joint = PhysicsPtr->PhysPxSphericalJointCreate(actor0.RigidActorPtr, &pxlocalFrame0, actor1.RigidActorPtr, &pxlocalFrame1);
            var jointObj = new PhysxJoint_Spherical(joint);
            Joints.Add((nint)joint, jointObj);
            SphericalJoints.Add((nint)joint, jointObj);
            return jointObj;
        }

        #endregion

        public static PxTransform MakeTransform(Vector3? position, Quaternion? rotation)
        {
            Quaternion q = rotation ?? Quaternion.Identity;
            Vector3 p = position ?? Vector3.Zero;
            PxVec3 pos = new() { x = p.X, y = p.Y, z = p.Z };
            PxQuat rot = new() { x = q.X, y = q.Y, z = q.Z, w = q.W };
            return PxTransform_new_5(&pos, &rot);
        }

        public PxSceneFlags Flags => _scene->GetFlags();
        public void SetFlag(PxSceneFlag flag, bool value)
            => _scene->SetFlagMut(flag, value);

        public PxSceneLimits Limits
        {
            get => _scene->GetLimits();
            set => _scene->SetLimitsMut(&value);
        }

        public void AddArticulation(PxArticulationReducedCoordinate* articulation)
            => _scene->AddArticulationMut(articulation);
        public void RemoveArticulation(PxArticulationReducedCoordinate* articulation, bool wakeOnLostTouch)
            => _scene->RemoveArticulationMut(articulation, wakeOnLostTouch);

        public void AddAggregate(PxAggregate* aggregate)
            => _scene->AddAggregateMut(aggregate);
        public void RemoveAggregate(PxAggregate* aggregate, bool wakeOnLostTouch)
            => _scene->RemoveAggregateMut(aggregate, wakeOnLostTouch);

        public void AddCollection(PxCollection* collection)
            => _scene->AddCollectionMut(collection);
        public uint GetActorCount(PxActorTypeFlags types)
            => _scene->GetNbActors(types);

        public PhysxActor[] GetActors(PxActorTypeFlags types)
        {
            uint count = GetActorCount(types);
            PxActor** ptrs = stackalloc PxActor*[(int)count];
            uint numWritten = _scene->GetActors(types, ptrs, count, 0);
            PhysxActor[] actors = new PhysxActor[count];
            for (int i = 0; i < count; i++)
                actors[i] = PhysxActor.Get(ptrs[i])!;
            return actors;
        }

        /// <summary>
        /// Requires PxSceneFlag::eENABLE_ACTIVE_ACTORS to be set.
        /// </summary>
        /// <returns></returns>
        public PhysxActor[] GetActiveActors()
        {
            uint count;
            PxActor** ptrs = _scene->GetActiveActorsMut(&count);
            PhysxActor[] actors = new PhysxActor[count];
            for (int i = 0; i < count; i++)
                actors[i] = PhysxActor.Get(ptrs[i])!;
            return actors;
        }

        public uint ArticulationCount => _scene->GetNbArticulations();

        public PxArticulationReducedCoordinate*[] GetArticulations()
        {
            uint count = ArticulationCount;
            PxArticulationReducedCoordinate** ptrs = stackalloc PxArticulationReducedCoordinate*[(int)count];
            uint numWritten = _scene->GetArticulations(ptrs, count, 0);
            PxArticulationReducedCoordinate*[] articulations = new PxArticulationReducedCoordinate*[count];
            for (int i = 0; i < count; i++)
                articulations[i] = ptrs[i];
            return articulations;
        }

        public uint ConstraintCount => _scene->GetNbConstraints();

        public PxConstraint*[] GetConstraints()
        {
            uint count = ConstraintCount;
            PxConstraint** ptrs = stackalloc PxConstraint*[(int)count];
            uint numWritten = _scene->GetConstraints(ptrs, count, 0);
            PxConstraint*[] constraints = new PxConstraint*[count];
            for (int i = 0; i < count; i++)
                constraints[i] = ptrs[i];
            return constraints;
        }

        public uint AggregateCount => _scene->GetNbAggregates();

        public PxAggregate*[] GetAggregates()
        {
            uint count = AggregateCount;
            PxAggregate** ptrs = stackalloc PxAggregate*[(int)count];
            uint numWritten = _scene->GetAggregates(ptrs, count, 0);
            PxAggregate*[] aggregates = new PxAggregate*[count];
            for (int i = 0; i < count; i++)
                aggregates[i] = ptrs[i];
            return aggregates;
        }

        public void SetDominanceGroupPair(byte group1, byte group2, PxDominanceGroupPair dominance)
            => _scene->SetDominanceGroupPairMut(group1, group2, &dominance);

        public PxDominanceGroupPair GetDominanceGroupPair(byte group1, byte group2)
            => _scene->GetDominanceGroupPair(group1, group2);

        public bool ResetFiltering(PhysxActor actor)
            => _scene->ResetFilteringMut(actor.ActorPtr);

        public bool ResetFiltering(PhysxRigidActor actor, PhysxShape[] shapes)
        {
            PxShape** shapes_ = stackalloc PxShape*[shapes.Length];
            for (int i = 0; i < shapes.Length; i++)
                shapes_[i] = shapes[i].ShapePtr;
            return _scene->ResetFilteringMut1(actor.RigidActorPtr, shapes_, (uint)shapes.Length);
        }

        public PxPairFilteringMode KinematicKinematicFilteringMode
            => _scene->GetKinematicKinematicFilteringMode();

        public PxPairFilteringMode StaticKinematicFilteringMode
            => _scene->GetStaticKinematicFilteringMode();

        public float BounceThresholdVelocity
        {
            get => _scene->GetBounceThresholdVelocity();
            set => _scene->SetBounceThresholdVelocityMut(value);
        }

        public uint CCDMaxPasses
        {
            get => _scene->GetCCDMaxPasses();
            set => _scene->SetCCDMaxPassesMut(value);
        }

        public float CCDMaxSeparation
        {
            get => _scene->GetCCDMaxSeparation();
            set => _scene->SetCCDMaxSeparationMut(value);
        }

        public float CCDThreshold
        {
            get => _scene->GetCCDThreshold();
            set => _scene->SetCCDThresholdMut(value);
        }

        public float MaxBiasCoefficient
        {
            get => _scene->GetMaxBiasCoefficient();
            set => _scene->SetMaxBiasCoefficientMut(value);
        }

        public float FrictionOffsetThreshold
        {
            get => _scene->GetFrictionOffsetThreshold();
            set => _scene->SetFrictionOffsetThresholdMut(value);
        }

        public float FrictionCorrelationDistance
        {
            get => _scene->GetFrictionCorrelationDistance();
            set => _scene->SetFrictionCorrelationDistanceMut(value);
        }

        public PxFrictionType FrictionType
            => _scene->GetFrictionType();

        public PxSolverType SolverType
            => _scene->GetSolverType();
        
        public bool SetVisualizationParameter(PxVisualizationParameter param, float value)
            => _scene->SetVisualizationParameterMut(param, value);

        public float GetVisualizationParameter(PxVisualizationParameter param)
            => _scene->GetVisualizationParameter(param);

        public AABB VisualizationCullingBox
        {
            get
            {
                PxBounds3 b = _scene->GetVisualizationCullingBox();
                return new AABB { Min = b.minimum, Max = b.maximum };
            }
            set
            {
                PxBounds3 b = new() { minimum = value.Min, maximum = value.Max };
                _scene->SetVisualizationCullingBoxMut(&b);
            }
        }

        public PxRenderBuffer* RenderBuffer
            => _scene->GetRenderBufferMut();

        public PxSimulationStatistics SimulationStatistics
        {
            get
            {
                PxSimulationStatistics stats;
                _scene->GetSimulationStatistics(&stats);
                return stats;
            }
        }

        public PxBroadPhaseType BroadPhaseType
            => _scene->GetBroadPhaseType();

        public PxBroadPhaseCaps BroadPhaseCaps
        {
            get
            {
                PxBroadPhaseCaps caps;
                _scene->GetBroadPhaseCaps(&caps);
                return caps;
            }
        }

        public uint BroadPhaseRegionsCount
            => _scene->GetNbBroadPhaseRegions();
        public PxBroadPhaseRegionInfo[] GetBroadPhaseRegions(uint startIndex)
        {
            uint count = BroadPhaseRegionsCount;
            PxBroadPhaseRegionInfo* buffer = stackalloc PxBroadPhaseRegionInfo[(int)count];
            uint numWritten = _scene->GetBroadPhaseRegions(buffer, count, startIndex);
            PxBroadPhaseRegionInfo[] regions = new PxBroadPhaseRegionInfo[count];
            for (int i = 0; i < count; i++)
                regions[i] = buffer[i];
            return regions;
        }
        public uint AddBroadPhaseRegion(PxBroadPhaseRegion region, bool populateRegion)
            => _scene->AddBroadPhaseRegionMut(&region, populateRegion);
        public bool RemoveBroadPhaseRegion(uint handle)
            => _scene->RemoveBroadPhaseRegionMut(handle);

        public PxTaskManager* TaskManager
            => _scene->GetTaskManager();

        public void LockRead(byte* file, uint line)
            => _scene->LockReadMut(file, line);
        public void UnlockRead()
            => _scene->UnlockReadMut();
        public void LockWrite(byte* file, uint line)
            => _scene->LockWriteMut(file, line);
        public void UnlockWrite()
            => _scene->UnlockWriteMut();

        public void SetContactDataBlockCount(uint numBlocks)
            => _scene->SetNbContactDataBlocksMut(numBlocks);

        public uint ContactDataBlocksUsed
            => _scene->GetNbContactDataBlocksUsed();

        public uint MaxContactDataBlocksUsed
            => _scene->GetMaxNbContactDataBlocksUsed();

        public uint ContactReportStreamBufferSize
            => _scene->GetContactReportStreamBufferSize();

        public uint SolverBatchSize
        {
            get => _scene->GetSolverBatchSize();
            set => _scene->SetSolverBatchSizeMut(value);
        }

        public uint SolverArticulationBatchSize
        {
            get => _scene->GetSolverArticulationBatchSize();
            set => _scene->SetSolverArticulationBatchSizeMut(value);
        }

        public float WakeCounterResetValue
            => _scene->GetWakeCounterResetValue();

        public void ShiftOrigin(Vector3 shift)
        {
            PxVec3 s = shift;
            _scene->ShiftOriginMut(&s);
        }

        public PxPvdSceneClient* ScenePvdClient
            => _scene->GetScenePvdClientMut();

        public void CopyArticulationData(void* data, void* index, PxArticulationGpuDataType dataType, uint nbCopyArticulations, void* copyEvent)
            => _scene->CopyArticulationDataMut(data, index, dataType, nbCopyArticulations, copyEvent);

        public void ApplyArticulationData(void* data, void* index, PxArticulationGpuDataType dataType, uint nbUpdatedArticulations, void* waitEvent, void* signalEvent)
            => _scene->ApplyArticulationDataMut(data, index, dataType, nbUpdatedArticulations, waitEvent, signalEvent);
        
        public void CopySoftBodyData(void** data, void* dataSizes, void* softBodyIndices, PxSoftBodyDataFlag flag, uint nbCopySoftBodies, uint maxSize, void* copyEvent)
                => _scene->CopySoftBodyDataMut(data, dataSizes, softBodyIndices, flag, nbCopySoftBodies, maxSize, copyEvent);
        public void CopyContactData(void* data, uint maxContactPairs, void* numContactPairs, void* copyEvent)
            => _scene->CopyContactDataMut(data, maxContactPairs, numContactPairs, copyEvent);
        public void CopyBodyData(PxGpuBodyData* data, PxGpuActorPair* index, uint nbCopyActors, void* copyEvent)
            => _scene->CopyBodyDataMut(data, index, nbCopyActors, copyEvent);

        public void ApplySoftBodyData(void** data, void* dataSizes, void* softBodyIndices, PxSoftBodyDataFlag flag, uint nbUpdatedSoftBodies, uint maxSize, void* applyEvent)
            => _scene->ApplySoftBodyDataMut(data, dataSizes, softBodyIndices, flag, nbUpdatedSoftBodies, maxSize, applyEvent);
        public void ApplyActorData(void* data, PxGpuActorPair* index, PxActorCacheFlag flag, uint nbUpdatedActors, void* waitEvent, void* signalEvent)
            => _scene->ApplyActorDataMut(data, index, flag, nbUpdatedActors, waitEvent, signalEvent);

        public void ComputeDenseJacobians(PxIndexDataPair* indices, uint nbIndices, void* computeEvent)
            => _scene->ComputeDenseJacobiansMut(indices, nbIndices, computeEvent);

        public void ComputeGeneralizedMassMatrices(PxIndexDataPair* indices, uint nbIndices, void* computeEvent)
            => _scene->ComputeGeneralizedMassMatricesMut(indices, nbIndices, computeEvent);
        public void ComputeGeneralizedGravityForces(PxIndexDataPair* indices, uint nbIndices, void* computeEvent)
            => _scene->ComputeGeneralizedGravityForcesMut(indices, nbIndices, computeEvent);
        public void ComputeCoriolisAndCentrifugalForces(PxIndexDataPair* indices, uint nbIndices, void* computeEvent)
            => _scene->ComputeCoriolisAndCentrifugalForcesMut(indices, nbIndices, computeEvent);

        public PxgDynamicsMemoryConfig GetGpuDynamicsConfig()
            => _scene->GetGpuDynamicsConfig();

        public void ApplyParticleBufferData(uint* indices, PxGpuParticleBufferIndexPair* bufferIndexPair, PxParticleBufferFlags* flags, uint nbUpdatedBuffers, void* waitEvent, void* signalEvent)
            => _scene->ApplyParticleBufferDataMut(indices, bufferIndexPair, flags, nbUpdatedBuffers, waitEvent, signalEvent);

        public PxSceneReadLock* ReadLockNewAlloc(byte* file, uint line)
            => _scene->ReadLockNewAlloc(file, line);
        public PxSceneWriteLock* WriteLockNewAlloc(byte* file, uint line)
            => _scene->WriteLockNewAlloc(file, line);

        private ControllerManager? _controllerManager;
        public ControllerManager CreateOrCreateControllerManager(bool lockingEnabled = false)
            => _controllerManager ??= new ControllerManager(_scene->PhysPxCreateControllerManager(lockingEnabled));

        public void ReleaseControllerManager()
        {
            if (_controllerManager == null)
                return;
            
            _controllerManager.ControllerManagerPtr->ReleaseMut();
            _controllerManager = null;
        }

        public bool RaycastAny(
            Vector3 origin,
            Vector3 unitDir,
            float distance,
            out uint hitFaceIndex,
            PxQueryFlags queryFlags,
            PxFilterData* filterMask = null,
            PxQueryFilterCallback* filterCallback = null,
            PxQueryCache* cache = null)
        {
            var filterData = filterMask != null ? PxQueryFilterData_new_1(filterMask, queryFlags) : PxQueryFilterData_new_2(queryFlags);
            PxVec3 o = origin;
            PxVec3 d = unitDir;
            PxQueryHit hit_;
            bool hasHit = _scene->QueryExtRaycastAny(
                (PxVec3*)Unsafe.AsPointer(ref o),
                (PxVec3*)Unsafe.AsPointer(ref d),
                distance,
                &hit_,
                &filterData,
                filterCallback,
                cache);
            hitFaceIndex = hit_.faceIndex;
            return hasHit;
        }

        /// <summary>
        /// Raycast returning a single result.
        /// Returns the first rigid actor that is hit along the ray.
        /// Data for a blocking hit will be returned as specified by the outputFlags field.
        /// Touching hits will be ignored.
        /// </summary>
        /// <param name="origin">Origin of the ray.</param>
        /// <param name="unitDir">Normalized direction of the ray.</param>
        /// <param name="distance">Length of the ray. Needs to be larger than 0.</param>
        /// <param name="outputFlags">Specifies which properties should be written to the hit information.</param>
        /// <param name="hit">Raycast hit information.</param>
        /// <param name="filterData">Filtering data and simple logic.</param>
        /// <param name="filterCallback">Custom filtering logic (optional). 
        /// Only used if the corresponding PxHitFlag flags are set. If NULL, all hits are assumed to be blocking.</param>
        /// <param name="cache">Cached hit shape (optional).
        /// Ray is tested against cached shape first then against the scene.
        /// Note: Filtering is not executed for a cached shape if supplied; instead, if a hit is found, it is assumed to be a blocking hit. 
        /// Note: Using past touching hits as cache will produce incorrect behavior since the cached hit will always be treated as blocking.</param>
        /// <returns></returns>
        public bool RaycastSingle(
            Vector3 origin,
            Vector3 unitDir,
            float distance,
            PxHitFlags outputFlags,
            out PxRaycastHit hit,
            PxQueryFlags queryFlags,
            PxFilterData* filterMask = null,
            PxQueryFilterCallback* filterCallback = null,
            PxQueryCache* cache = null)
        {
            var filterData = filterMask != null ? PxQueryFilterData_new_1(filterMask, queryFlags) : PxQueryFilterData_new_2(queryFlags);
            PxVec3 o = origin;
            PxVec3 d = unitDir;
            PxRaycastHit hit_;
            bool hasHit = _scene->QueryExtRaycastSingle(
                &o,
                &d,
                distance,
                outputFlags,
                &hit_,
                &filterData,
                filterCallback,
                cache);
            hit = hit_;
            return hasHit;
        }

        public PxRaycastHit[] RaycastMultiple(
            Vector3 origin,
            Vector3 unitDir,
            float distance,
            PxHitFlags outputFlags,
            out bool blockingHit,
            PxQueryFlags queryFlags,
            PxFilterData* filterMask = null,
            PxQueryFilterCallback* filterCallback = null,
            PxQueryCache* cache = null,
            int maxHitCapacity = 32)
        {
            var filterData = filterMask != null ? PxQueryFilterData_new_1(filterMask, queryFlags) : PxQueryFilterData_new_2(queryFlags);
            PxVec3 o = origin;
            PxVec3 d = unitDir;
            PxRaycastHit* hitBuffer = stackalloc PxRaycastHit[maxHitCapacity];
            bool blockingHit_;
            int hitCount = _scene->QueryExtRaycastMultiple(
                &o,
                &d,
                distance,
                outputFlags,
                hitBuffer,
                (uint)maxHitCapacity,
                &blockingHit_,
                &filterData,
                filterCallback,
                cache);
            blockingHit = blockingHit_;
            PxRaycastHit[] hits = new PxRaycastHit[hitCount];
            for (int i = 0; i < hitCount; i++)
                hits[i] = hitBuffer[i];
            return hits;
        }

        public bool SweepAny(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            Vector3 unitDir,
            float distance,
            PxHitFlags hitFlags,
            out PxQueryHit hit,
            PxQueryFlags queryFlags,
            PxFilterData* filterMask = null,
            float inflation = 0.0f,
            PxQueryFilterCallback* filterCallback = null,
            PxQueryCache* cache = null)
        {
            var filterData = filterMask != null ? PxQueryFilterData_new_1(filterMask, queryFlags) : PxQueryFilterData_new_2(queryFlags);
            PxVec3 d = unitDir;
            var t = MakeTransform(pose.position, pose.rotation);
            PxQueryHit hit_;
            using var structObj = geometry.GetPhysxStruct();
            bool hasHit = _scene->QueryExtSweepAny(
                structObj.Address.As<PxGeometry>(),
                &t,
                &d,
                distance,
                hitFlags,
                &hit_,
                &filterData,
                filterCallback,
                cache,
                inflation);
            hit = hit_;
            return hasHit;
        }

        //Note that the scene-level sweep query returns PxSweepHit structures,
        //while the object-level sweep query returns PxGeomSweepHit hits.
        //The difference is simply that PxSweepHit is augmented with PxRigidActor and PxShape pointers.
        public bool SweepSingle(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            Vector3 unitDir,
            float distance,
            PxHitFlags outputFlags,
            out PxSweepHit hit,
            PxQueryFlags queryFlags,
            PxFilterData* filterMask = null,
            float inflation = 0.0f,
            PxQueryFilterCallback* filterCallback = null,
            PxQueryCache* cache = null)
        {
            var filterData = filterMask != null ? PxQueryFilterData_new_1(filterMask, queryFlags) : PxQueryFilterData_new_2(queryFlags);
            PxVec3 d = unitDir;
            var t = MakeTransform(pose.position, pose.rotation);
            PxSweepHit hit_;
            using var structObj = geometry.GetPhysxStruct();
            bool hasHit = _scene->QueryExtSweepSingle(
                structObj.Address.As<PxGeometry>(),
                &t,
                &d,
                distance,
                outputFlags,
                &hit_,
                &filterData,
                filterCallback,
                cache,
                inflation);
            hit = hit_;
            return hasHit;
        }

        //Note that the scene-level sweep query returns PxSweepHit structures,
        //while the object-level sweep query returns PxGeomSweepHit hits.
        //The difference is simply that PxSweepHit is augmented with PxRigidActor and PxShape pointers.
        public PxSweepHit[] SweepMultiple(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            Vector3 unitDir,
            float distance,
            PxHitFlags outputFlags,
            out bool blockingHit,
            PxQueryFlags queryFlags,
            PxFilterData* filterMask = null,
            float inflation = 0.0f,
            PxQueryFilterCallback* filterCallback = null,
            PxQueryCache* cache = null,
            int maxHitCapacity = 32)
        {
            var filterData = filterMask != null ? PxQueryFilterData_new_1(filterMask, queryFlags) : PxQueryFilterData_new_2(queryFlags);
            PxVec3 d = unitDir;
            var t = MakeTransform(pose.position, pose.rotation);
            bool blockingHit_;
            PxSweepHit* hitBuffer_ = stackalloc PxSweepHit[maxHitCapacity];
            using var structObj = geometry.GetPhysxStruct();
            int hitCount = _scene->QueryExtSweepMultiple(
                structObj.Address.As<PxGeometry>(),
                &t,
                &d,
                distance,
                outputFlags,
                hitBuffer_,
                (uint)maxHitCapacity,
                &blockingHit_,
                &filterData,
                filterCallback,
                cache,
                inflation);
            blockingHit = blockingHit_;
            PxSweepHit[] hits = new PxSweepHit[hitCount];
            for (int i = 0; i < hitCount; i++)
                hits[i] = hitBuffer_[i];
            return hits;
        }

        public PxOverlapHit[] OverlapMultiple(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            PxQueryFlags queryFlags,
            PxFilterData* filterMask = null,
            PxQueryFilterCallback* filterCallback = null,
            int maxHitCapacity = 32)
        {
            var filterData = filterMask != null ? PxQueryFilterData_new_1(filterMask, queryFlags) : PxQueryFilterData_new_2(queryFlags);
            var t = MakeTransform(pose.position, pose.rotation);
            PxOverlapHit* hitBuffer = stackalloc PxOverlapHit[maxHitCapacity];
            using var structObj = geometry.GetPhysxStruct();
            int hitCount = _scene->QueryExtOverlapMultiple(
                structObj.Address.As<PxGeometry>(),
                &t,
                hitBuffer,
                (uint)maxHitCapacity,
                &filterData,
                filterCallback);
            PxOverlapHit[] hits = new PxOverlapHit[hitCount];
            for (int i = 0; i < hitCount; i++)
                hits[i] = hitBuffer[i];
            return hits;
        }

        public bool OverlapAny(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            out PxOverlapHit hit,
            PxQueryFlags queryFlags,
            PxFilterData* filterMask = null,
            PxQueryFilterCallback* filterCallback = null)
        {
            var filterData = filterMask != null ? PxQueryFilterData_new_1(filterMask, queryFlags) : PxQueryFilterData_new_2(queryFlags);
            var t = MakeTransform(pose.position, pose.rotation);
            PxOverlapHit hit_;
            using var structObj = geometry.GetPhysxStruct();
            bool hasHit = _scene->QueryExtOverlapAny(
                structObj.Address.As<PxGeometry>(),
                &t,
                &hit_,
                &filterData,
                filterCallback);
            hit = hit_;
            return hasHit;
        }

        public PhysxBatchQuery CreateBatchQuery(
            PxQueryFilterCallback* queryFilterCallback,
            uint maxRaycastCount,
            uint maxRaycastTouchCount,
            uint maxSweepCount,
            uint maxSweepTouchCount,
            uint maxOverlapCount,
            uint maxOverlapTouchCount)
        {
            var ptr = _scene->PhysPxCreateBatchQueryExt(
                queryFilterCallback,
                maxRaycastCount,
                maxRaycastTouchCount,
                maxSweepCount,
                maxSweepTouchCount,
                maxOverlapCount,
                maxOverlapTouchCount);
            return new PhysxBatchQuery(ptr);
        }

        public PhysxBatchQuery CreateBatchQuery(
            PxQueryFilterCallback* queryFilterCallback,
            PxRaycastBuffer* raycastBuffers,
            uint maxRaycastCount,
            PxRaycastHit* raycastTouches,
            uint maxRaycastTouchCount,
            PxSweepBuffer* sweepBuffers,
            uint maxSweepCount,
            PxSweepHit* sweepTouches,
            uint maxSweepTouchCount,
            PxOverlapBuffer* overlapBuffers,
            uint maxOverlapCount,
            PxOverlapHit* overlapTouches,
            uint maxOverlapTouchCount)
        {
            var ptr = _scene->PhysPxCreateBatchQueryExt1(
                queryFilterCallback,
                raycastBuffers,
                maxRaycastCount,
                raycastTouches,
                maxRaycastTouchCount,
                sweepBuffers,
                maxSweepCount,
                sweepTouches,
                maxSweepTouchCount,
                overlapBuffers,
                maxOverlapCount,
                overlapTouches,
                maxOverlapTouchCount);
            return new PhysxBatchQuery(ptr);
        }

        public override void AddActor(IAbstractPhysicsActor actor)
        {
            if (actor is not PhysxActor physxActor)
                return;
            
            AddActor(physxActor);
        }

        public override void RemoveActor(IAbstractPhysicsActor actor)
        {
            if (actor is not PhysxActor physxActor)
                return;
            
            RemoveActor(physxActor);
        }

        public override void NotifyShapeChanged(IAbstractPhysicsActor actor)
        {
            //RemoveActor(actor);
            //AddActor(actor);
        }

        private static RaycastHit ToRaycastHit(PxRaycastHit hit)
            => new()
            {
                Position = hit.position,
                Normal = hit.normal,
                Distance = hit.distance,
                FaceIndex = hit.faceIndex,
                UV = new Vector2(hit.u, hit.v),
            };

        public override bool RaycastAny(
            Segment worldSegment,
            out uint hitFaceIndex)
        {
            var start = worldSegment.Start;
            var end = worldSegment.End;
            var distance = worldSegment.Length;
            var unitDir = (end - start).Normalized();
            return RaycastAny(start, unitDir, distance, out hitFaceIndex, PxQueryFlags.AnyHit, null, null, null);
        }

        public override void RaycastSingle(
            Segment worldSegment,
            SortedDictionary<float, List<(XRComponent? item, object? data)>> results)
        {
            var start = worldSegment.Start;
            var end = worldSegment.End;
            var distance = worldSegment.Length;
            var unitDir = (end - start).Normalized();

            if (!RaycastSingle(start, unitDir, distance, PxHitFlags.Position | PxHitFlags.Normal, out PxRaycastHit hit, PxQueryFlags.AnyHit, null, null, null))
                return;

            PhysxRigidActor? actor = PhysxRigidActor.Get(hit.actor);
            if (actor is null)
                return;

            XRComponent? component = actor.GetOwningComponent();
            if (component is null)
                return;

            if (!results.TryGetValue(hit.distance, out var list))
                results.Add(hit.distance, list = []);

            list.Add((component, ToRaycastHit(hit)));
        }

        public override void RaycastMultiple(
            Segment worldSegment,
            SortedDictionary<float, List<(XRComponent? item, object? data)>> results)
        {
            var start = worldSegment.Start;
            var end = worldSegment.End;
            var distance = worldSegment.Length;
            var unitDir = (end - start).Normalized();

            var hits = RaycastMultiple(start, unitDir, distance, PxHitFlags.Position | PxHitFlags.Normal, out bool blockingHit, PxQueryFlags.AnyHit, null, null, null);
            foreach (var hit in hits)
            {
                PhysxRigidActor? actor = PhysxRigidActor.Get(hit.actor);
                if (actor is null)
                    continue;

                XRComponent? component = actor.GetOwningComponent();
                if (component is null)
                    continue;

                Vector3 hitPoint = start + unitDir * hit.distance;
                Vector3 hitNormal = hit.normal;
                //var d = (hitPoint - start).Length();
                //if (d > distance)
                //    continue;

                if (!results.TryGetValue(hit.distance, out var list))
                    results.Add(hit.distance, list = []);

                list.Add((component, (hitPoint, hitNormal, hit.distance)));
            }
        }

        public override bool SweepAny(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            Vector3 unitDir,
            float distance,
            out uint hitFaceIndex)
        {
            bool hasHit = SweepAny(
                geometry,
                pose,
                unitDir,
                distance,
                PxHitFlags.Position | PxHitFlags.Normal,
                out PxQueryHit hit,
                PxQueryFlags.AnyHit,
                null,
                0.0f,
                null,
                null);
            hitFaceIndex = hit.faceIndex;
            return hasHit;
        }

        public override void SweepSingle(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            Vector3 unitDir,
            float distance,
            SortedDictionary<float, List<(XRComponent? item, object? data)>> results)
        {
            if (SweepSingle(
                geometry,
                pose,
                unitDir,
                distance,
                PxHitFlags.Position | PxHitFlags.Normal,
                out PxSweepHit hit,
                PxQueryFlags.Static,
                null,
                0.0f,
                null,
                null))
                AddSweepHit(pose, distance, results, hit);
        }

        private static void AddSweepHit(
            (Vector3 position, Quaternion rotation) pose,
            float distance,
            SortedDictionary<float, List<(XRComponent? item, object? data)>> results,
            PxSweepHit hit)
        {
            PhysxRigidActor? actor = PhysxRigidActor.Get(hit.actor);
            if (actor is null)
                return;

            XRComponent? component = actor.GetOwningComponent();
            //if (component is null)
            //    return;

            Vector3 hitPoint = hit.position;
            Vector3 hitNormal = hit.normal;
            //var d = (hitPoint - pose.position).Length();
            //if (d > distance)
            //    return;

            if (!results.TryGetValue(hit.distance, out var list))
                results.Add(hit.distance, list = []);

            list.Add((component, (hitPoint, hitNormal, hit.distance)));
        }

        public override void SweepMultiple(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            Vector3 unitDir,
            float distance,
            SortedDictionary<float, List<(XRComponent? item, object? data)>> results)
        {
            var hits = SweepMultiple(geometry, pose, unitDir, distance, PxHitFlags.Position | PxHitFlags.Normal, out bool blockingHit, PxQueryFlags.AnyHit, null, 0.0f, null, null);
            foreach (var hit in hits)
            {
                PhysxRigidActor? actor = PhysxRigidActor.Get(hit.actor);
                if (actor is null)
                    continue;

                XRComponent? component = actor.GetOwningComponent();
                if (component is null)
                    continue;

                Vector3 hitPoint = hit.position;
                Vector3 hitNormal = hit.normal;
                //var d = (hitPoint - pose.position).Length();
                //if (d > distance)
                //    continue;

                if (!results.TryGetValue(hit.distance, out var list))
                    results.Add(hit.distance, list = []);

                list.Add((component, (hitPoint, hitNormal, hit.distance)));
            }
        }

        public override bool OverlapAny(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            SortedDictionary<float, List<(XRComponent? item, object? data)>> results)
        {
            bool hasHit = OverlapAny(geometry, pose, out PxOverlapHit hit, PxQueryFlags.AnyHit, null, null);
            if (hasHit)
            {
                PhysxRigidActor? actor = PhysxRigidActor.Get(hit.actor);
                if (actor is null)
                    return false;

                XRComponent? component = actor.GetOwningComponent();
                if (component is null)
                    return false;

                var d = 0.0f;

                if (!results.TryGetValue(d, out var list))
                    results.Add(0.0f, list = []);

                list.Add((component, null));
            }
            return hasHit;
        }

        public override void OverlapMultiple(
            IPhysicsGeometry geometry,
            (Vector3 position, Quaternion rotation) pose,
            SortedDictionary<float, List<(XRComponent? item, object? data)>> results)
        {
            var hits = OverlapMultiple(geometry, pose, PxQueryFlags.AnyHit, null, null, 32);
            foreach (var hit in hits)
            {
                PhysxRigidActor? actor = PhysxRigidActor.Get(hit.actor);
                if (actor is null)
                    continue;

                XRComponent? component = actor.GetOwningComponent();
                if (component is null)
                    continue;

                var d = 0.0f;

                if (!results.TryGetValue(d, out var list))
                    results.Add(0.0f, list = []);

                list.Add((component, null));
            }
        }

        public bool VisualizeEnabled
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.Scale, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.Scale) > 0.0f;
        }
        public bool VisualizeWorldAxes
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.WorldAxes, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.WorldAxes) > 0.0f;
        }
        public bool VisualizeBodyAxes
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.BodyAxes, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.BodyAxes) > 0.0f;
        }
        public bool VisualizeBodyMassAxes
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.BodyMassAxes, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.BodyMassAxes) > 0.0f;
        }
        public bool VisualizeBodyLinearVelocity
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.BodyLinVelocity, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.BodyLinVelocity) > 0.0f;
        }
        public bool VisualizeBodyAngularVelocity
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.BodyAngVelocity, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.BodyAngVelocity) > 0.0f;
        }
        public bool VisualizeContactPoint
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.ContactPoint, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.ContactPoint) > 0.0f;
        }
        public bool VisualizeContactNormal
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.ContactNormal, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.ContactNormal) > 0.0f;
        }
        public bool VisualizeContactError
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.ContactError, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.ContactError) > 0.0f;
        }
        public bool VisualizeContactForce
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.ContactForce, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.ContactForce) > 0.0f;
        }
        public bool VisualizeActorAxes
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.ActorAxes, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.ActorAxes) > 0.0f;
        }
        public bool VisualizeCollisionAabbs
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.CollisionAabbs, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.CollisionAabbs) > 0.0f;
        }
        public bool VisualizeCollisionShapes
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.CollisionShapes, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.CollisionShapes) > 0.0f;
        }
        public bool VisualizeCollisionAxes
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.CollisionAxes, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.CollisionAxes) > 0.0f;
        }
        public bool VisualizeCollisionCompounds
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.CollisionCompounds, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.CollisionCompounds) > 0.0f;
        }
        public bool VisualizeCollisionFaceNormals
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.CollisionFnormals, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.CollisionFnormals) > 0.0f;
        }
        public bool VisualizeCollisionEdges
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.CollisionEdges, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.CollisionEdges) > 0.0f;
        }
        public bool VisualizeCollisionStatic
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.CollisionStatic, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.CollisionStatic) > 0.0f;
        }
        public bool VisualizeCollisionDynamic
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.CollisionDynamic, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.CollisionDynamic) > 0.0f;
        }
        public bool VisualizeJointLocalFrames
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.JointLocalFrames, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.JointLocalFrames) > 0.0f;
        }
        public bool VisualizeJointLimits
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.JointLimits, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.JointLimits) > 0.0f;
        }
        public bool VisualizeCullBox
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.CullBox, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.CullBox) > 0.0f;
        }
        public bool VisualizeMbpRegions
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.MbpRegions, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.MbpRegions) > 0.0f;
        }
        public bool VisualizeSimulationMesh
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.SimulationMesh, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.SimulationMesh) > 0.0f;
        }
        public bool VisualizeSdf
        {
            set => _scene->SetVisualizationParameterMut(PxVisualizationParameter.Sdf, value ? 1.0f : 0.0f);
            get => _scene->GetVisualizationParameter(PxVisualizationParameter.Sdf) > 0.0f;
        }
    }
}