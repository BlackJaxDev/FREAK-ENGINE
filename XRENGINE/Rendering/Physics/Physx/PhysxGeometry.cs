using MagicPhysX;
using System.Numerics;
using XREngine.Data.Geometry;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxGeometry(PxGeometry* geo) : AbstractPhysicsGeometry
    {
        public PxGeometry* GeometryPtr => geo;

        public PxGeometryHolder NewHolder() => GeometryPtr->HolderNew1();

        public bool ValidQuery => GeometryPtr->QueryIsValid();

        public PxGeometryType GeometryType => PxGeometry_getType(GeometryPtr);
        public bool QueryOverlap(
            (Vector3 position, Quaternion rotation) pose0,
            PhysxGeometry geom1,
            (Vector3 position, Quaternion rotation) pose1,
            PxGeometryQueryFlags queryFlags,
            PxQueryThreadContext* threadContext)
        {
            PxTransform p0 = PhysxScene.MakeTransform(pose0.position, pose0.rotation);
            PxTransform p1 = PhysxScene.MakeTransform(pose1.position, pose1.rotation);
            return PxGeometryQuery_overlap(GeometryPtr, &p0, geom1.GeometryPtr, &p1, queryFlags, threadContext);
        }
        public (Matrix4x4 inertiaTensor, Vector3 centerOfMass, float mass) MassProperties
        {
            get
            {
                PxMassProperties props = GeometryPtr->MassPropertiesNew2();
                var it = props.inertiaTensor;
                Vector3 col0 = it.column0;
                Vector3 col1 = it.column1;
                Vector3 col2 = it.column2;
                Matrix4x4 itMtx = new(
                    col0.X, col1.X, col2.X, 0,
                    col0.Y, col1.Y, col2.Y, 0,
                    col0.Z, col1.Z, col2.Z, 0,
                    0, 0, 0, 1);
                return (itMtx, props.centerOfMass, props.mass);
            }
        }

        public PxPoissonSampler* PoissonSamplerNew((Vector3 position, Quaternion rotation) transform, AABB worldBounds, float initialSamplingRadius, int numSampleAttemptsAroundPoint)
        {
            PxTransform tfm = PhysxScene.MakeTransform(transform.position, transform.rotation);
            PxVec3 min = PxVec3_new_3(worldBounds.Min.X, worldBounds.Min.Y, worldBounds.Min.Z);
            PxVec3 max = PxVec3_new_3(worldBounds.Max.X, worldBounds.Max.Y, worldBounds.Max.Z);
            PxBounds3 bounds = PxBounds3_new_1(&min, &max);
            return GeometryPtr->PhysPxCreateShapeSampler(&tfm, &bounds, initialSamplingRadius, numSampleAttemptsAroundPoint);
        }

        public uint MeshQueryFindOverlapTriangleMesh(
            (Vector3 position, Quaternion rotation) pose,
            PxTriangleMeshGeometry* meshGeom,
            (Vector3 position, Quaternion rotation) meshPose,
            uint* results,
            uint maxResults,
            uint startIndex,
            bool* overflow,
            PxGeometryQueryFlags queryFlags)
        {
            PxTransform tfm = PhysxScene.MakeTransform(pose.position, pose.rotation);
            PxTransform meshTfm = PhysxScene.MakeTransform(meshPose.position, meshPose.rotation);
            return GeometryPtr->MeshQueryFindOverlapTriangleMesh(&tfm, meshGeom, &meshTfm, results, maxResults, startIndex, overflow, queryFlags);
        }

        public uint MeshQueryFindOverlapHeightField(
            (Vector3 position, Quaternion rotation) pose,
            PxHeightFieldGeometry* heightField,
            (Vector3 position, Quaternion rotation) heightFieldPose,
            uint* results,
            uint maxResults,
            uint startIndex,
            bool* overflow,
            PxGeometryQueryFlags queryFlags)
        {
            PxTransform p = PhysxScene.MakeTransform(pose.position, pose.rotation);
            PxTransform hfPose = PhysxScene.MakeTransform(heightFieldPose.position, heightFieldPose.rotation);
            return GeometryPtr->MeshQueryFindOverlapHeightField(&p, heightField, &hfPose, results, maxResults, startIndex, overflow, queryFlags);
        }
    }
}