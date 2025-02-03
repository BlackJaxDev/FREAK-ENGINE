using MagicPhysX;
using System.Numerics;
using XREngine.Data;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe class PhysxHeightField(PhysxScene scene, PxHeightField* heightFieldPtr) : PhysxBase
    {
        public PxHeightField* HeightFieldPtr { get; } = heightFieldPtr;
        public override unsafe PxBase* BasePtr => (PxBase*)HeightFieldPtr;
        public PhysxScene Scene { get; } = scene;

        public PxHeightFieldFlags Flags => HeightFieldPtr->GetFlags();
        public unsafe uint RowCount => HeightFieldPtr->GetNbRows();
        public unsafe uint ColumnCount => HeightFieldPtr->GetNbColumns();
        public unsafe PxHeightFieldFormat Format => HeightFieldPtr->GetFormat();
        public unsafe uint SampleStride => HeightFieldPtr->GetSampleStride();
        public unsafe float ConvexEdgeThreshold => HeightFieldPtr->GetConvexEdgeThreshold();

        public uint SaveCells(DataSource data)
            => HeightFieldPtr->SaveCells(data.Address, data.Length);

        public void Release()
            => HeightFieldPtr->ReleaseMut();

        public unsafe PxHeightFieldGeometry NewGeometry(PxMeshGeometryFlags flags, float heightScale = 1.0f, float rowScale = 1.0f, float columnScale = 1.0f)
            => PxHeightFieldGeometry_new(HeightFieldPtr, flags, heightScale, rowScale, columnScale);

        public unsafe bool ModifySamplesMut(int startCol, int startRow, PxHeightFieldDesc* subfieldDesc, bool shrinkBounds)
            => HeightFieldPtr->ModifySamplesMut(startCol, startRow, subfieldDesc, shrinkBounds);

        public unsafe float GetHeight(float x, float z)
            => HeightFieldPtr->GetHeight(x, z);

        public unsafe ushort GetTriangleMaterialIndex(uint triangleIndex)
            => HeightFieldPtr->GetTriangleMaterialIndex(triangleIndex);

        public unsafe Vector3 GetTriangleNormal(uint triangleIndex)
            => HeightFieldPtr->GetTriangleNormal(triangleIndex);

        public unsafe PxHeightFieldSample* GetSample(uint row, uint column)
            => HeightFieldPtr->GetSample(row, column);

        public unsafe uint GetTimestamp()
            => HeightFieldPtr->GetTimestamp();
    }
}