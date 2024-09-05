using Extensions;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Physics;
using XREngine.Rendering;

namespace XREngine.Scene.Components.Mesh.Shapes
{
    public class SphereMeshComponent(float radius, XRMaterial material, RigidBodyConstructionInfo? info, uint meshPrecision = 40u) : ShapeMeshComponent(
        new Sphere(Vector3.Zero, radius),
        [
            new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, meshPrecision), radius * 8),
            new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, (uint)(meshPrecision * 0.8f).ClampMin(1.0f)), radius * 16),
            new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, (uint)(meshPrecision * 0.6f).ClampMin(1.0f)), radius * 32),
            new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, (uint)(meshPrecision * 0.4f).ClampMin(1.0f)), radius * 64),
            new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, (uint)(meshPrecision * 0.2f).ClampMin(1.0f)), radius * 128),
        ],
        //[
        //    new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, meshPrecision), radius * 2),
        //    new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, (uint)(meshPrecision * 0.7f).ClampMin(1.0f)), radius * 4),
        //    new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, (uint)(meshPrecision * 0.5f).ClampMin(1.0f)), radius * 8),
        //    new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, (uint)(meshPrecision * 0.3f).ClampMin(1.0f)), radius * 16),
        //    new(material, XRMesh.Shapes.SolidSphere(Vector3.Zero, radius, (uint)(meshPrecision * 0.15f).ClampMin(1.0f)), radius * 32),
        //],
        XRCollisionSphere.New(radius),
        info)
    {
        public SphereMeshComponent()
            : this(0.5f) { }
        public SphereMeshComponent(float radius)
            : this(radius, XRMaterial.CreateLitColorMaterial(Engine.InvalidColor)) { }
        public SphereMeshComponent(float radius, XRMaterial material)
            : this(radius, material, null) { }
    }
}
