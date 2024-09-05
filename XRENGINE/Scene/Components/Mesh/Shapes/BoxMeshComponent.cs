using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Physics;
using XREngine.Rendering;
using XREngine.Rendering.Models;

namespace XREngine.Scene.Components.Mesh.Shapes
{
    public class BoxMeshComponent(Vector3 halfExtents, XRMaterial material, RigidBodyConstructionInfo? info) : ShapeMeshComponent(
            new AABB(-halfExtents, halfExtents),
            [new LOD(material, XRMesh.Shapes.SolidBox(-halfExtents, halfExtents), 0.0f)],
            XRCollisionBox.New(halfExtents),
            info)
    {
        public BoxMeshComponent()
            : this(new Vector3(0.5f)) { }
        public BoxMeshComponent(Vector3 halfExtents)
            : this(halfExtents, XRMaterial.CreateLitColorMaterial(Engine.InvalidColor)) { }
        public BoxMeshComponent(Vector3 halfExtents, XRMaterial material)
            : this(halfExtents, material, null) { }
    }
}
