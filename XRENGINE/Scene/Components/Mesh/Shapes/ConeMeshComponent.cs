using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Physics;
using XREngine.Rendering;

namespace XREngine.Scene.Components.Mesh.Shapes
{
    public class ConeMeshComponent(float radius, float height,
        XRMaterial material, RigidBodyConstructionInfo? info, int meshSides = 40, bool closeBottom = true) : ShapeMeshComponent(
            new Cone(Vector3.Zero, Globals.Up, height, radius),
            [
                new(material, XRMesh.Shapes.SolidCone(Vector3.Zero, Globals.Up, height, radius, meshSides,         closeBottom), radius *  8),
                new(material, XRMesh.Shapes.SolidCone(Vector3.Zero, Globals.Up, height, radius, meshSides / 4 * 3, closeBottom), radius * 16),
                new(material, XRMesh.Shapes.SolidCone(Vector3.Zero, Globals.Up, height, radius, meshSides / 2,     closeBottom), radius * 32),
                new(material, XRMesh.Shapes.SolidCone(Vector3.Zero, Globals.Up, height, radius, meshSides / 4,     closeBottom), radius * 64),
            ],
            XRCollisionConeY.New(radius, height),
            info)
    {
        public ConeMeshComponent()
            : this(1.0f, 1.0f) { }
        public ConeMeshComponent(float radius, float height)
            : this(radius, height, XRMaterial.CreateLitColorMaterial(Engine.InvalidColor)) { }
        public ConeMeshComponent(float radius, float height, XRMaterial material)
            : this(radius, height, material, null) { }
    }
}
