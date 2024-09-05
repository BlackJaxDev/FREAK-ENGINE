using XREngine.Components.Scene.Mesh;
using XREngine.Data.Geometry;
using XREngine.Physics;
using XREngine.Rendering.Models;

namespace XREngine.Scene.Components.Mesh.Shapes
{
    public abstract class ShapeMeshComponent(
        IShape shape,
        EventList<LOD> lods,
        XRCollisionShape? collisionShape = null,
        RigidBodyConstructionInfo? info = null)
        : StaticMeshComponent(
            new StaticModel(new StaticRigidSubMesh(shape, lods))
            {
                CollisionShape = collisionShape
            },
            info)
    { }
}
