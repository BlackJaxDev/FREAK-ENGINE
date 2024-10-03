using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Rendering;
using XREngine.Data.Transforms.Rotations;
using XREngine.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine
{
    public static partial class Engine
    {
        public static partial class Rendering
        {
            /// <summary>
            /// Debug rendering functions.
            /// These should not be used in production code.
            /// </summary>
            public static class Debug
            {
                public static readonly Vector3 UIPositionBias = new(0.0f, 0.0f, 0.1f);
                public static readonly Rotator UIRotation = new(90.0f, 0.0f, 0.0f, ERotationOrder.YPR);

                public const float DefaultPointSize = 10.0f;
                public const float DefaultLineSize = 1.0f;

                private static unsafe void SetOptions(bool? depthTestEnabled, float? lineWidth, float? pointSize, XRMeshRenderer renderer)
                {
                    var opts = renderer.Material!.RenderOptions;

                    if (lineWidth.HasValue)
                        opts.LineWidth = lineWidth.Value;

                    if (pointSize.HasValue)
                        opts.PointSize = pointSize.Value;

                    if (depthTestEnabled.HasValue)
                        opts.DepthTest.Enabled = depthTestEnabled.Value ? ERenderParamUsage.Enabled : ERenderParamUsage.Disabled;
                }

                public static void RenderPoint(
                    Vector3 position,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float pointSize = DefaultPointSize)
                {
                    XRMeshRenderer renderer = GetDebugPrimitive(EDebugPrimitiveType.Point);
                    SetOptions(depthTestEnabled, null, pointSize, renderer);
                    renderer.SetParameter(0, color);
                    renderer.Render(Matrix4x4.CreateTranslation(position));
                }

                public static unsafe void RenderLine(
                    Vector3 start,
                    Vector3 end,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                {
                    XRMeshRenderer renderer = GetDebugPrimitive(EDebugPrimitiveType.Line);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    renderer.Render(
                        Matrix4x4.CreateTranslation(start) *
                        XRMath.LookatAngles(start, end).GetMatrix() *
                        Matrix4x4.CreateScale((end - start).Length()));
                }

                public static void RenderCircle(
                    Vector3 centerTranslation,
                    Rotator rotation,
                    float radius,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                {
                    XRMeshRenderer renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidCircle : EDebugPrimitiveType.WireCircle);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    renderer.Render(
                        Matrix4x4.CreateTranslation(centerTranslation) *
                        rotation.GetMatrix() *
                        Matrix4x4.CreateScale(radius, 1.0f, radius));
                }

                public static void RenderQuad(
                    Vector3 centerTranslation,
                    Rotator rotation,
                    Vector2 extents,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                {
                    var renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidQuad : EDebugPrimitiveType.WireQuad);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    renderer.Render(Matrix4x4.CreateTranslation(centerTranslation) * rotation.GetMatrix() * Matrix4x4.CreateScale(extents.X, 1.0f, extents.Y));
                }

                public static void RenderSphere(
                    Vector3 center,
                    float radius,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                {
                    XRMeshRenderer renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidSphere : EDebugPrimitiveType.WireSphere);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    //radius doesn't need to be multiplied by 2.0f; the sphere is already 2.0f in diameter
                    renderer.Render(Matrix4x4.CreateTranslation(center) * Matrix4x4.CreateScale(radius));
                }

                public static void RenderAABB(
                    Vector3 halfExtents,
                    Vector3 translation,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                    => RenderBox(
                        halfExtents,
                        Matrix4x4.CreateTranslation(translation),
                        solid,
                        color,
                        depthTestEnabled,
                        lineWidth);

                public static void RenderBox(
                    Vector3 halfExtents,
                    Matrix4x4 transform,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                {
                    var renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidBox : EDebugPrimitiveType.WireBox);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    //halfExtents doesn't need to be multiplied by 2.0f; the box is already 1.0f in each direction of each dimension (2.0f extents)
                    renderer.Render(transform * Matrix4x4.CreateScale(halfExtents));
                }

                public static void RenderCapsule(
                    Matrix4x4 transform,
                    Vector3 localUpAxis,
                    float radius,
                    float halfHeight,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                {
                    string cylStr = "_CYLINDER";
                    string topStr = "_TOPHALF";
                    string botStr = "_BOTTOMHALF";

                    _debugPrimitives.TryGetValue(cylStr, out XRMeshRenderer? mCyl);
                    _debugPrimitives.TryGetValue(topStr, out XRMeshRenderer? mTop);
                    _debugPrimitives.TryGetValue(botStr, out XRMeshRenderer? mBot);

                    if (mCyl is null || mTop is null || mBot is null)
                    {
                        //TODO: solid capsule parts
                        XRMesh.Shapes.WireframeCapsuleParts(Vector3.Zero, Globals.Up, 1.0f, 1.0f, 30,
                            out XRMesh cylData, out XRMesh topData, out XRMesh botData);
                        mCyl ??= AssignDebugPrimitive(cylStr, new XRMeshRenderer(cylData, XRMaterial.CreateUnlitColorMaterialForward()));
                        mTop ??= AssignDebugPrimitive(topStr, new XRMeshRenderer(topData, XRMaterial.CreateUnlitColorMaterialForward()));
                        mBot ??= AssignDebugPrimitive(botStr, new XRMeshRenderer(botData, XRMaterial.CreateUnlitColorMaterialForward()));
                    }

                    Matrix4x4 axisRotation = Matrix4x4.CreateFromQuaternion(XRMath.RotationBetweenVectors(Globals.Up, localUpAxis));
                    Matrix4x4 radiusMtx = Matrix4x4.CreateScale(radius);
                    Matrix4x4 cylTransform = transform * axisRotation * Matrix4x4.CreateScale(radius, halfHeight, radius);
                    Matrix4x4 topTransform = transform * axisRotation * Matrix4x4.CreateTranslation(0.0f, halfHeight, 0.0f) * radiusMtx;
                    Matrix4x4 botTransform = transform * axisRotation * Matrix4x4.CreateTranslation(0.0f, -halfHeight, 0.0f) * radiusMtx;

                    SetOptions(depthTestEnabled, lineWidth, null, mCyl);
                    SetOptions(depthTestEnabled, lineWidth, null, mTop);
                    SetOptions(depthTestEnabled, lineWidth, null, mBot);
                    mCyl.SetParameter(0, color);
                    mTop.SetParameter(0, color);
                    mBot.SetParameter(0, color);
                    mCyl.Render(cylTransform);
                    mTop.Render(topTransform);
                    mBot.Render(botTransform);
                }

                public static void RenderCylinder(Matrix4x4 transform, Vector3 localUpAxis, float radius, float halfHeight, bool solid, ColorF4 color, float lineWidth = DefaultLineSize)
                {
                    throw new NotImplementedException();
                }
                public static void RenderCone(Matrix4x4 transform, Vector3 localUpAxis, float radius, float height, bool solid, ColorF4 color, float lineWidth = DefaultLineSize)
                {
                    //SetLineSize(lineWidth);
                    XRMeshRenderer m = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidCone : EDebugPrimitiveType.WireCone);
                    m.Parameter<ShaderVector4>(0).Value = color;
                    transform = transform * XRMath.LookatAngles(localUpAxis).GetMatrix() * Matrix4x4.CreateScale(radius, radius, height);
                    m.Render(transform);
                }

                private static XRMeshRenderer AssignDebugPrimitive(string name, XRMeshRenderer m)
                {
                    if (!_debugPrimitives.ContainsKey(name))
                        _debugPrimitives.Add(name, m);
                    else
                        _debugPrimitives[name] = m;
                    return m;
                }

                public enum EDebugPrimitiveType
                {
                    Point,
                    Line,

                    WireQuad,
                    SolidQuad,

                    WireCircle,
                    SolidCircle,

                    WireSphere,
                    SolidSphere,

                    WireBox,
                    SolidBox,


                    WireCylinder,
                    SolidCylinder,

                    WireCone,
                    SolidCone,
                }

                private static readonly Dictionary<string, XRMeshRenderer> _debugPrimitives = [];
                private static readonly XRMeshRenderer[] _debugPrims = new XRMeshRenderer[14];

                public static XRMeshRenderer GetDebugPrimitive(EDebugPrimitiveType type)
                {
                    XRMeshRenderer mesh = _debugPrims[(int)type];
                    if (mesh != null)
                        return mesh;
                    else
                    {
                        XRMaterial mat = XRMaterial.CreateUnlitColorMaterialForward();
                        RenderingParameters p = new();
                        p.DepthTest.Enabled = ERenderParamUsage.Enabled;
                        mat.RenderOptions = p;
                        return _debugPrims[(int)type] = new XRMeshRenderer(GetMesh(type), mat);
                    }
                }

                public static XRMesh GetMesh(EDebugPrimitiveType type)
                    => type switch
                    {
                        EDebugPrimitiveType.Point => XRMesh.CreatePoints(Vector3.Zero),
                        EDebugPrimitiveType.Line => XRMesh.CreateLines(Vector3.Zero, Globals.Forward),
                        EDebugPrimitiveType.WireSphere => XRMesh.Shapes.WireframeSphere(Vector3.Zero, 1.0f, 60),//Diameter is set to 2.0f on purpose
                        EDebugPrimitiveType.SolidSphere => XRMesh.Shapes.SolidSphere(Vector3.Zero, 1.0f, 30),//Diameter is set to 2.0f on purpose
                        EDebugPrimitiveType.WireBox => XRMesh.Shapes.WireframeBox(new Vector3(-1.0f), new Vector3(1.0f)),
                        EDebugPrimitiveType.SolidBox => XRMesh.Shapes.SolidBox(new Vector3(-1.0f), new Vector3(1.0f)),
                        EDebugPrimitiveType.WireCircle => XRMesh.Shapes.WireframeCircle(1.0f, Vector3.UnitY, Vector3.Zero, 20),//Diameter is set to 2.0f on purpose
                        EDebugPrimitiveType.SolidCircle => XRMesh.Shapes.SolidCircle(1.0f, Vector3.UnitY, Vector3.Zero, 20),//Diameter is set to 2.0f on purpose
                        EDebugPrimitiveType.WireQuad => XRMesh.Create(VertexQuad.PosY(1.0f, false, false).ToLines()),
                        EDebugPrimitiveType.SolidQuad => XRMesh.Create(VertexQuad.PosY(1.0f, false, false)),
                        EDebugPrimitiveType.WireCone => XRMesh.Shapes.WireframeCone(Vector3.Zero, Globals.Forward, 1.0f, 1.0f, 20),
                        EDebugPrimitiveType.SolidCone => XRMesh.Shapes.SolidCone(Vector3.Zero, Globals.Forward, 1.0f, 1.0f, 20, true),
                        _ => throw new InvalidOperationException(),
                    };
            }
        }
    }
}