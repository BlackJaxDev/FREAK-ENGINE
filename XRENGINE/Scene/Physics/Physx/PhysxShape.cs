using MagicPhysX;
using System.Numerics;
using System.Runtime.InteropServices;
using XREngine.Data.Geometry;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxShape : PhysxRefCounted, IAbstractPhysicsShape
    {
        private readonly PhysxScene _scene;
        private readonly unsafe PxShape* _shape;

        public PhysxShape(PhysxScene scene, PxShape* shape)
        {
            _scene = scene;
            _shape = shape;
            All.Add((nint)shape, this);
        }

        public PxShape* ShapePtr => _shape;
        public PhysxScene Scene => _scene;

        public override unsafe PxBase* BasePtr => (PxBase*)_shape;
        public override unsafe PxRefCounted* RefCountedPtr => (PxRefCounted*)_shape;

        public static Dictionary<nint, PhysxShape> All { get; } = [];
        public static PhysxShape? Get(PxShape* ptr)
            => All.TryGetValue((nint)ptr, out var shape) ? shape : null;

        public bool SimulationShape
        {
            get => Flags.HasFlag(PxShapeFlags.SimulationShape);
            set => SetFlag(PxShapeFlag.SimulationShape, value);
        }
        public bool SceneQueryShape
        {
            get => Flags.HasFlag(PxShapeFlags.SceneQueryShape);
            set => SetFlag(PxShapeFlag.SceneQueryShape, value);
        }
        public bool TriggerShape
        {
            get => Flags.HasFlag(PxShapeFlags.TriggerShape);
            set => SetFlag(PxShapeFlag.TriggerShape, value);
        }
        public bool Visualization
        {
            get => Flags.HasFlag(PxShapeFlags.Visualization);
            set => SetFlag(PxShapeFlag.Visualization, value);
        }

        public PxShapeFlags Flags
        {
            get => ShapePtr->GetFlags(); 
            set => ShapePtr->SetFlagsMut(value);
        }

        public void SetFlag(PxShapeFlag flag, bool value)
            => ShapePtr->SetFlagMut(flag, value);

        public float ContactOffset
        {
            get => ShapePtr->GetContactOffset();
            set => ShapePtr->SetContactOffsetMut(value);
        }

        public override void Release()
            => ShapePtr->ReleaseMut();

        public PxGeometry* Geometry
        {
            get => ShapePtr->GetGeometry();
            set => ShapePtr->SetGeometryMut(value);
        }

        public unsafe PhysxRigidActor? GetActor()
            => PhysxRigidActor.Get(ShapePtr->GetActor());

        public (Vector3 position, Quaternion rotation) LocalPose
        {
            get
            {
                var tfm = ShapePtr->GetLocalPose();
                return (tfm.p, tfm.q);
            }
            set
            {
                var tfm = PhysxScene.MakeTransform(value.position, value.rotation);
                ShapePtr->SetLocalPoseMut(&tfm);
            }
        }

        public PxFilterData SimulationFilterData
        {
            get => ShapePtr->GetSimulationFilterData();
            set => ShapePtr->SetSimulationFilterDataMut(&value);
        }

        public PxFilterData QueryFilterData
        {
            get => ShapePtr->GetQueryFilterData();
            set => ShapePtr->SetQueryFilterDataMut(&value);
        }

        public PhysxMaterial[] Materials
        {
            get => GetMaterials();
            set => SetMaterials(value);
        }

        private unsafe void SetMaterials(PhysxMaterial[] materials)
        {
            PxMaterial** mats = stackalloc PxMaterial*[materials.Length];
            for (int i = 0; i < materials.Length; i++)
                mats[i] = materials[i].MaterialPtr;
            ShapePtr->SetMaterialsMut(mats, (ushort)materials.Length);
        }

        public unsafe ushort MaterialCount
            => ShapePtr->GetNbMaterials();

        private unsafe PhysxMaterial[] GetMaterials()
        {
            PxMaterial*[] materials = new PxMaterial*[MaterialCount];
            fixed (PxMaterial** materialsPtr = materials)
                ShapePtr->GetMaterials(materialsPtr, MaterialCount, 0u);
            PhysxMaterial[] mats = new PhysxMaterial[MaterialCount];
            for (int i = 0; i < MaterialCount; i++)
                mats[i] = PhysxMaterial.Get(materials[i])!;
            return mats;
        }

        public unsafe PxBaseMaterial* GetMaterialFromInternalFaceIndex(uint faceIndex)
            => ShapePtr->GetMaterialFromInternalFaceIndex(faceIndex);

        public float RestOffset
        {
            get => ShapePtr->GetRestOffset();
            set => ShapePtr->SetRestOffsetMut(value);
        }

        public float DensityForFluid
        {
            get => ShapePtr->GetDensityForFluid();
            set => ShapePtr->SetDensityForFluidMut(value);
        }

        public float TorsionalPatchRadius
        {
            get => ShapePtr->GetTorsionalPatchRadius();
            set => ShapePtr->SetTorsionalPatchRadiusMut(value);
        }

        public float MinTorsionalPatchRadius
        {
            get => ShapePtr->GetMinTorsionalPatchRadius();
            set => ShapePtr->SetMinTorsionalPatchRadiusMut(value);
        }

        public unsafe bool IsExclusive
            => ShapePtr->IsExclusive();

        public string Name
        {
            get => Marshal.PtrToStringAnsi((IntPtr)ShapePtr->GetName()) ?? string.Empty;
            set => ShapePtr->SetNameMut((byte*)Marshal.StringToHGlobalAnsi(value).ToPointer());
        }

        public unsafe PxQueryCache QueryCacheNew1(uint findex)
            => ShapePtr->QueryCacheNew1(findex);

        public unsafe (Vector3 position, Quaternion rotation) GetGlobalPose(PhysxRigidActor actor)
        {
            var tfm = ShapePtr->ExtGetGlobalPose(actor.RigidActorPtr);
            return (tfm.p, tfm.q);
        }

        public unsafe PxRaycastHit[] Raycast(PhysxRigidActor actor, Vector3 rayOrigin, Vector3 rayDir, float maxDist, PxHitFlags hitFlags, uint maxHits = 32)
        {
            PxVec3 ro = rayOrigin;
            PxVec3 rd = rayDir;
            PxRaycastHit[] rayHits = new PxRaycastHit[maxHits];
            fixed (PxRaycastHit* rayHitsPtr = rayHits)
            {
                uint num = ShapePtr->ExtRaycast(actor.RigidActorPtr, &ro, &rd, maxDist, hitFlags, maxHits, rayHitsPtr);
                PxRaycastHit[] hits = new PxRaycastHit[num];
                Array.Copy(rayHits, hits, num);
                return hits;
            }
        }

        public unsafe bool Overlap(PhysxRigidActor actor, IAbstractPhysicsGeometry otherGeom, (Vector3 position, Quaternion rotation) otherGeomPose)
        {
            var tfm = PhysxScene.MakeTransform(otherGeomPose.position, otherGeomPose.rotation);
            var structObj = otherGeom.GetPhysxStruct();
            return ShapePtr->ExtOverlap(actor.RigidActorPtr, structObj.Address.As<PxGeometry>(), &tfm);
        }

        public unsafe bool Sweep(PhysxRigidActor actor, Vector3 unitDir, float distance, IAbstractPhysicsGeometry otherGeom, (Vector3 position, Quaternion rotation) otherGeomPose, out PxSweepHit sweepHit, PxHitFlags hitFlags)
        {
            var tfm = PhysxScene.MakeTransform(otherGeomPose.position, otherGeomPose.rotation);
            PxSweepHit h;
            PxVec3 ud = unitDir;
            var structObj = otherGeom.GetPhysxStruct();
            bool result = ShapePtr->ExtSweep(actor.RigidActorPtr, &ud, distance, structObj.Address.As<PxGeometry>(), &tfm, &h, hitFlags);
            sweepHit = h;
            return result;
        }

        public unsafe AABB GetWorldBounds(PhysxRigidActor actor, float inflation)
        {
            var bounds = ShapePtr->ExtGetWorldBounds(actor.RigidActorPtr, inflation);
            return new AABB(bounds.minimum, bounds.maximum);
        }
    }
}