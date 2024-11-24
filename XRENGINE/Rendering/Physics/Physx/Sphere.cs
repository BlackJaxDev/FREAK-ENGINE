using MagicPhysX;
using System.Numerics;
using XREngine.Data;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public static class PhysxGeometry
    {
        public unsafe struct Sphere(float radius) : IAbstractPhysicsGeometry
        {
            public float Radius = radius;

            public readonly PxSphereGeometry GetGeometry() => PxSphereGeometry_new(Radius);

            readonly DataSource IAbstractPhysicsGeometry.GetStruct() => DataSource.FromStruct(GetGeometry());
        }
        public unsafe struct Box(Vector3 halfExtents) : IAbstractPhysicsGeometry
        {
            public Vector3 HalfExtents = halfExtents;

            public readonly PxBoxGeometry GetGeometry() => PxBoxGeometry_new(HalfExtents.X, HalfExtents.Y, HalfExtents.Z);

            readonly DataSource IAbstractPhysicsGeometry.GetStruct() => DataSource.FromStruct(GetGeometry());
        }
        public unsafe struct Capsule(float radius, float halfHeight) : IAbstractPhysicsGeometry
        {
            public float Radius = radius;
            public float HalfHeight = halfHeight;

            public readonly PxCapsuleGeometry GetGeometry() => PxCapsuleGeometry_new(Radius, HalfHeight);

            readonly DataSource IAbstractPhysicsGeometry.GetStruct() => DataSource.FromStruct(GetGeometry());
        }
        public unsafe struct ConvexMesh(PxConvexMesh* mesh, Vector3 scale, Quaternion rotation, bool tightBounds) : IAbstractPhysicsGeometry
        {
            public PxConvexMesh* Mesh = mesh;
            public Vector3 Scale = scale;
            public Quaternion Rotation = rotation;
            public bool TightBounds = tightBounds;

            public readonly PxConvexMeshGeometry GetGeometry()
            {
                PxVec3 scale = Scale;
                PxQuat rotation = Rotation;
                PxMeshScale scaleRot = PxMeshScale_new_3(&scale, &rotation);
                PxConvexMeshGeometryFlags flags = TightBounds ? PxConvexMeshGeometryFlags.TightBounds : 0;
                return PxConvexMeshGeometry_new(Mesh, &scaleRot, flags);
            }

            readonly DataSource IAbstractPhysicsGeometry.GetStruct() => DataSource.FromStruct(GetGeometry());
        }
        public unsafe struct TriangleMesh(PxTriangleMesh* mesh, Vector3 scale, Quaternion rotation, bool tightBounds, bool doubleSided) : IAbstractPhysicsGeometry
        {
            public PxTriangleMesh* Mesh = mesh;
            public Vector3 Scale = scale;
            public Quaternion Rotation = rotation;
            public bool TightBounds = tightBounds;
            public bool DoubleSided = doubleSided;

            public readonly PxTriangleMeshGeometry GetGeometry()
            {
                PxVec3 scale = Scale;
                PxQuat rotation = Rotation;
                PxMeshScale scaleRot = PxMeshScale_new_3(&scale, &rotation);
                PxMeshGeometryFlags flags = (TightBounds ? PxMeshGeometryFlags.TightBounds : 0) | (DoubleSided ? PxMeshGeometryFlags.DoubleSided : 0);
                return PxTriangleMeshGeometry_new(Mesh, &scaleRot, flags);
            }

            readonly DataSource IAbstractPhysicsGeometry.GetStruct() => DataSource.FromStruct(GetGeometry());
        }
        public unsafe struct HeightField(PxHeightField* field, float heightScale, float rowScale, float columnScale, bool tightBounds, bool doubleSided) : IAbstractPhysicsGeometry
        {
            public PxHeightField* Field = field;
            public float HeightScale = heightScale;
            public float RowScale = rowScale;
            public float ColumnScale = columnScale;
            public bool TightBounds = tightBounds;
            public bool DoubleSided = doubleSided;

            public readonly PxHeightFieldGeometry GetGeometry()
            {
                PxMeshGeometryFlags flags = (TightBounds ? PxMeshGeometryFlags.TightBounds : 0) | (DoubleSided ? PxMeshGeometryFlags.DoubleSided : 0);
                return PxHeightFieldGeometry_new(Field, flags, HeightScale, RowScale, ColumnScale);
            }

            readonly DataSource IAbstractPhysicsGeometry.GetStruct() => DataSource.FromStruct(GetGeometry());
        }
        public unsafe struct Plane() : IAbstractPhysicsGeometry
        {
            public readonly PxPlaneGeometry GetGeometry() => PxPlaneGeometry_new();

            readonly DataSource IAbstractPhysicsGeometry.GetStruct() => DataSource.FromStruct(GetGeometry());
        }
        public unsafe struct ParticleSystem(PxParticleSolverType solver) : IAbstractPhysicsGeometry
        {
            public PxParticleSolverType Solver = solver;

            public readonly PxParticleSystemGeometry GetGeometry()
            {
                var g = PxParticleSystemGeometry_new();
                g.mSolverType = Solver;
                return g;
            }

            readonly DataSource IAbstractPhysicsGeometry.GetStruct() => DataSource.FromStruct(GetGeometry());
        }
        public unsafe struct TetrahedronMesh(PxTetrahedronMesh* mesh) : IAbstractPhysicsGeometry
        {
            public PxTetrahedronMesh* Mesh = mesh;

            public readonly PxTetrahedronMeshGeometry GetGeometry()
                => PxTetrahedronMeshGeometry_new(Mesh);

            readonly DataSource IAbstractPhysicsGeometry.GetStruct() => DataSource.FromStruct(GetGeometry());
        }
    }
}