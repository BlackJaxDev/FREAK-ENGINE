using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxShape(PhysxScene scene, PxShape* shape) : PhysxRefCounted, IAbstractPhysicsShape
    {
        public PxShape* ShapePtr => shape;
        public PhysxScene Scene => scene;

        public override unsafe PxBase* BasePtr => (PxBase*)shape;
        public override unsafe PxRefCounted* RefCountedPtr => (PxRefCounted*)shape;

        public void SetSimulationFilterData(PxFilterData filterData)
            => ShapePtr->SetSimulationFilterDataMut(&filterData);

        public void SetQueryFilterData(PxFilterData filterData)
            => ShapePtr->SetQueryFilterDataMut(&filterData);

        public void SetLocalPose(PxTransform pose)
            => ShapePtr->SetLocalPoseMut(&pose);

        public PxShapeFlags Flags
        {
            get => ShapePtr->GetFlags(); 
            set => ShapePtr->SetFlagsMut(value);
        }

        public void SetFlag(PxShapeFlag flag, bool value)
            => ShapePtr->SetFlagMut(flag, value);

        public void SetRestOffset(float offset)
            => ShapePtr->SetRestOffsetMut(offset);

        public void SetContactOffset(float offset)
            => ShapePtr->SetContactOffsetMut(offset);

        public void SetDensityForFluid(float density)
            => ShapePtr->SetDensityForFluidMut(density);

        public void SetTorsionalPatchRadius(float radius)
            => ShapePtr->SetTorsionalPatchRadiusMut(radius);

        public void SetMinTorsionalPatchRadius(float radius)
            => ShapePtr->SetMinTorsionalPatchRadiusMut(radius);

        public void SetMaterials(PxMaterial** materials, ushort materialCount)
            => ShapePtr->SetMaterialsMut(materials, materialCount);

        public override void Release()
            => ShapePtr->ReleaseMut();

        public PhysxGeometry? Geometry
        {
            get => Scene.GetGeometry(ShapePtr->GetGeometry());
            set => ShapePtr->SetGeometryMut(value is null ? null : value.Geometry);
        }

        //public unsafe PhysxRigidActor GetActor()
        //{
        //    return Scene.GetRigidActor(Scene, ShapePtr->GetActor());
        //}

        //public unsafe static void SetLocalPoseMut(PxTransform* pose)
        //{
        //    NativeMethods.PxShape_setLocalPose_mut((PxShape*)Unsafe.AsPointer(ref self_), pose);
        //}

        //public unsafe static PxTransform GetLocalPose()
        //{
        //    return NativeMethods.PxShape_getLocalPose((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe static void SetSimulationFilterDataMut(PxFilterData* data)
        //{
        //    NativeMethods.PxShape_setSimulationFilterData_mut((PxShape*)Unsafe.AsPointer(ref self_), data);
        //}

        //public unsafe static PxFilterData GetSimulationFilterData()
        //{
        //    return NativeMethods.PxShape_getSimulationFilterData((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe static void SetQueryFilterDataMut(PxFilterData* data)
        //{
        //    NativeMethods.PxShape_setQueryFilterData_mut((PxShape*)Unsafe.AsPointer(ref self_), data);
        //}

        //public unsafe static PxFilterData GetQueryFilterData()
        //{
        //    return NativeMethods.PxShape_getQueryFilterData((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe static void SetMaterialsMut(PxMaterial** materials, ushort materialCount)
        //{
        //    NativeMethods.PxShape_setMaterials_mut((PxShape*)Unsafe.AsPointer(ref self_), materials, materialCount);
        //}

        //public unsafe static ushort GetNbMaterials()
        //{
        //    return NativeMethods.PxShape_getNbMaterials((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe static uint GetMaterials(PxMaterial** userBuffer, uint bufferSize, uint startIndex)
        //{
        //    return NativeMethods.PxShape_getMaterials((PxShape*)Unsafe.AsPointer(ref self_), userBuffer, bufferSize, startIndex);
        //}

        //public unsafe static PxBaseMaterial* GetMaterialFromInternalFaceIndex(uint faceIndex)
        //{
        //    return NativeMethods.PxShape_getMaterialFromInternalFaceIndex((PxShape*)Unsafe.AsPointer(ref self_), faceIndex);
        //}

        //public unsafe static void SetContactOffsetMut(float contactOffset)
        //{
        //    NativeMethods.PxShape_setContactOffset_mut((PxShape*)Unsafe.AsPointer(ref self_), contactOffset);
        //}


        //public unsafe static float GetContactOffset()
        //{
        //    return NativeMethods.PxShape_getContactOffset((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe static void SetRestOffsetMut(float restOffset)
        //{
        //    NativeMethods.PxShape_setRestOffset_mut((PxShape*)Unsafe.AsPointer(ref self_), restOffset);
        //}

        //public unsafe static float GetRestOffset()
        //{
        //    return NativeMethods.PxShape_getRestOffset((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe static void SetDensityForFluidMut(float densityForFluid)
        //{
        //    NativeMethods.PxShape_setDensityForFluid_mut((PxShape*)Unsafe.AsPointer(ref self_), densityForFluid);
        //}

        //public unsafe static float GetDensityForFluid()
        //{
        //    return NativeMethods.PxShape_getDensityForFluid((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe static void SetTorsionalPatchRadiusMut(float radius)
        //{
        //    NativeMethods.PxShape_setTorsionalPatchRadius_mut((PxShape*)Unsafe.AsPointer(ref self_), radius);
        //}

        //public unsafe static float GetTorsionalPatchRadius()
        //{
        //    return NativeMethods.PxShape_getTorsionalPatchRadius((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe static void SetMinTorsionalPatchRadiusMut(float radius)
        //{
        //    NativeMethods.PxShape_setMinTorsionalPatchRadius_mut((PxShape*)Unsafe.AsPointer(ref self_), radius);
        //}

        //public unsafe static float GetMinTorsionalPatchRadius()
        //{
        //    return NativeMethods.PxShape_getMinTorsionalPatchRadius((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe bool IsExclusive()
        //{
        //    return NativeMethods.PxShape_isExclusive((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe void SetNameMut(byte* name)
        //{
        //    NativeMethods.PxShape_setName_mut((PxShape*)Unsafe.AsPointer(ref self_), name);
        //}

        //public unsafe byte* GetName()
        //{
        //    return NativeMethods.PxShape_getName((PxShape*)Unsafe.AsPointer(ref self_));
        //}

        //public unsafe PxQueryCache QueryCacheNew1(this ref PxShape s, uint findex)
        //{
        //    return NativeMethods.PxQueryCache_new_1((PxShape*)Unsafe.AsPointer(ref s), findex);
        //}

        //public unsafe static PxTransform ExtGetGlobalPose(PxRigidActor* actor)
        //{
        //    return NativeMethods.PxShapeExt_getGlobalPose((PxShape*)Unsafe.AsPointer(ref shape), actor);
        //}

        //public unsafe static uint ExtRaycast(PxRigidActor* actor, PxVec3* rayOrigin, PxVec3* rayDir, float maxDist, PxHitFlags hitFlags, uint maxHits, PxRaycastHit* rayHits)
        //{
        //    return NativeMethods.PxShapeExt_raycast((PxShape*)Unsafe.AsPointer(ref shape), actor, rayOrigin, rayDir, maxDist, hitFlags, maxHits, rayHits);
        //}

        //public unsafe static bool ExtOverlap(PxRigidActor* actor, PxGeometry* otherGeom, PxTransform* otherGeomPose)
        //{
        //    return NativeMethods.PxShapeExt_overlap((PxShape*)Unsafe.AsPointer(ref shape), actor, otherGeom, otherGeomPose);
        //}

        //public unsafe static bool ExtSweep(PxRigidActor* actor, PxVec3* unitDir, float distance, PxGeometry* otherGeom, PxTransform* otherGeomPose, PxSweepHit* sweepHit, PxHitFlags hitFlags)
        //{
        //    return NativeMethods.PxShapeExt_sweep((PxShape*)Unsafe.AsPointer(ref shape), actor, unitDir, distance, otherGeom, otherGeomPose, sweepHit, hitFlags);
        //}

        //public unsafe static PxBounds3 ExtGetWorldBounds(PxRigidActor* actor, float inflation)
        //{
        //    return NativeMethods.PxShapeExt_getWorldBounds((PxShape*)Unsafe.AsPointer(ref shape), actor, inflation);
        //}
    }
}