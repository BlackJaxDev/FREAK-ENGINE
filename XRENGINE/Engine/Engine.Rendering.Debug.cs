using Extensions;
using System.Collections.Concurrent;
using System.Numerics;
using System.Transactions;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
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

                private static ConcurrentQueue<IShapeData> _debugShapesRendering = new();
                private static ConcurrentQueue<IShapeData> _debugShapesUpdating = new();

                public static void EnqueueShape(IShapeData shape)
                {
                    _debugShapesUpdating.Enqueue(shape);
                }

                public static void SwapBuffers()
                {
                    _debugShapesRendering.Clear();
                    (_debugShapesUpdating, _debugShapesRendering) = (_debugShapesRendering, _debugShapesUpdating);
                }

                public static void RenderShapes()
                {
                    foreach (IShapeData shape in _debugShapesRendering)
                    {
                        switch (shape)
                        {
                            case PointData p:
                                RenderPoint(p.Position, p.Color, p.DepthTestEnabled);
                                break;
                            case LineData l:
                                RenderLine(l.Start, l.End, l.Color, l.DepthTestEnabled);
                                break;
                            case CircleData c:
                                RenderCircle(c.Center, c.Rotation, c.Radius, c.Solid, c.Color, c.DepthTestEnabled);
                                break;
                            case QuadData q:
                                RenderQuad(q.Center, q.Rotation, q.Extents, q.Solid, q.Color, q.DepthTestEnabled);
                                break;
                            case SphereData s:
                                RenderSphere(s.Center, s.Radius, s.Solid, s.Color, s.DepthTestEnabled);
                                break;
                            case AABBData a:
                                RenderAABB(a.HalfExtents, a.Translation, a.Solid, a.Color, a.DepthTestEnabled);
                                break;
                            case BoxData b:
                                RenderBox(b.HalfExtents, b.Center, b.Transform, b.Solid, b.Color, b.DepthTestEnabled);
                                break;
                            case CapsuleData c:
                                RenderCapsule(c.Capsule, c.Color, c.DepthTestEnabled);
                                break;
                            case TriangleData t:
                                RenderTriangle(t.Value, t.Color, t.Solid, t.DepthTestEnabled);
                                break;
                            case CylinderData c:
                                RenderCylinder(c.Transform, c.LocalUpAxis, c.Radius, c.HalfHeight, c.Solid, c.Color);
                                break;
                            case ConeData c:
                                RenderCone(c.Center, c.UpAxis, c.Radius, c.Height, c.Solid, c.Color);
                                break;
                        }
                    }
                }

                public interface IShapeData
                {
                    public ColorF4 Color { get; }
                    public bool DepthTestEnabled { get; }
                    public bool Solid { get; }
                }
                public struct PointData(Vector3 position, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Vector3 Position = position;
                    public ColorF4 Color { get; } = color;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                    public bool Solid { get; } = false;
                }
                public struct LineData(Vector3 start, Vector3 end, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Vector3 Start = start;
                    public Vector3 End = end;
                    public ColorF4 Color { get; } = color;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                    public bool Solid { get; } = false;
                }
                public struct CircleData(bool solid, Vector3 center, Rotator rotation, float radius, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Vector3 Center = center;
                    public Rotator Rotation = rotation;
                    public float Radius = radius;
                    public ColorF4 Color { get; } = color;
                    public bool Solid { get; } = solid;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                }
                public struct QuadData(bool solid, Vector3 center, Rotator rotation, Vector2 extents, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Vector3 Center = center;
                    public Rotator Rotation = rotation;
                    public Vector2 Extents = extents;
                    public ColorF4 Color { get; } = color;
                    public bool Solid { get; } = solid;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                }
                public struct SphereData(bool solid, Vector3 center, float radius, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Vector3 Center = center;
                    public float Radius = radius;
                    public ColorF4 Color { get; } = color;
                    public bool Solid { get; } = solid;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                }
                public struct AABBData(bool solid, Vector3 halfExtents, Vector3 translation, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Vector3 HalfExtents = halfExtents;
                    public Vector3 Translation = translation;
                    public ColorF4 Color { get; } = color;
                    public bool Solid { get; } = solid;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                }
                public struct BoxData(bool solid, Vector3 halfExtents, Vector3 center, Matrix4x4 transform, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Vector3 HalfExtents = halfExtents;
                    public Vector3 Center = center;
                    public Matrix4x4 Transform = transform;
                    public ColorF4 Color { get; } = color;
                    public bool Solid { get; } = solid;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                }
                public struct CapsuleData(bool solid, Capsule capsule, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Capsule Capsule = capsule;
                    public ColorF4 Color { get; } = color;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                    public bool Solid { get; } = solid;
                }
                public struct TriangleData(bool solid, Vector3 A, Vector3 B, Vector3 C, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Triangle Value = new(A, B, C);
                    public ColorF4 Color { get; } = color;
                    public bool Solid { get; } = solid;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                }
                public struct CylinderData(bool solid, Matrix4x4 transform, Vector3 localUpAxis, float radius, float halfHeight, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Matrix4x4 Transform = transform;
                    public Vector3 LocalUpAxis = localUpAxis;
                    public float Radius = radius;
                    public float HalfHeight = halfHeight;
                    public ColorF4 Color { get; } = color;
                    public bool Solid { get; } = solid;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                }
                public struct ConeData(bool solid, Vector3 center, Vector3 upAxis, float radius, float height, ColorF4 color, bool depthTestEnabled = true) : IShapeData
                {
                    public Vector3 Center = center;
                    public Vector3 UpAxis = upAxis;
                    public float Radius = radius;
                    public float Height = height;
                    public ColorF4 Color { get; } = color;
                    public bool Solid { get; } = solid;
                    public bool DepthTestEnabled { get; } = depthTestEnabled;
                }

                private static unsafe void SetOptions(bool? depthTestEnabled, float? lineWidth, float? pointSize, XRMeshRenderer renderer)
                {
                    var mat = renderer.Material;
                    if (mat is null)
                        return;

                    var opts = mat.RenderOptions;

                    if (lineWidth.HasValue)
                        opts.LineWidth = lineWidth.Value;

                    if (pointSize.HasValue)
                        opts.PointSize = pointSize.Value;

                    if (depthTestEnabled.HasValue)
                    {
                        var enabled = depthTestEnabled.Value;
                        if (enabled)
                        {
                            opts.DepthTest.Enabled = ERenderParamUsage.Enabled;
                            mat.RenderPass = (int)EDefaultRenderPass.OpaqueForward;
                        }
                        else
                        {
                            opts.DepthTest.Enabled = ERenderParamUsage.Disabled;
                            mat.RenderPass = (int)EDefaultRenderPass.OnTopForward;
                        }
                    }
                }

                public static void RenderPoint(
                    Vector3 position,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float pointSize = DefaultPointSize)
                {
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new PointData(position, color, depthTestEnabled));
                        return;
                    }

                    XRMeshRenderer renderer = GetDebugPrimitive(EDebugPrimitiveType.Point, out _);
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
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new LineData(start, end, color, depthTestEnabled));
                        return;
                    }

                    XRMeshRenderer renderer = GetDebugPrimitive(EDebugPrimitiveType.Line, out _);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    Vector3 dir = (end - start).Normalized();
                    Vector3 arb = Vector3.UnitX;
                    if (Vector3.Dot(dir, Vector3.UnitX) > 0.99f || Vector3.Dot(dir, Vector3.UnitX) < -0.99f)
                        arb = Vector3.UnitZ;
                    Vector3 perp = Vector3.Cross(dir, arb).Normalized();
                    renderer.Render(Matrix4x4.CreateScale(Vector3.Distance(start, end)) * Matrix4x4.CreateWorld(start, dir, perp));
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
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new CircleData(solid, centerTranslation, rotation, radius, color, depthTestEnabled));
                        return;
                    }

                    XRMeshRenderer renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidCircle : EDebugPrimitiveType.WireCircle, out _);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    renderer.Render(
                        Matrix4x4.CreateScale(radius, 1.0f, radius) *
                        rotation.GetMatrix() *
                        Matrix4x4.CreateTranslation(centerTranslation));
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
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new QuadData(solid, centerTranslation, rotation, extents, color, depthTestEnabled));
                        return;
                    }

                    var renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidQuad : EDebugPrimitiveType.WireQuad, out _);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    renderer.Render(
                        Matrix4x4.CreateScale(extents.X, 1.0f, extents.Y) *
                        rotation.GetMatrix() *
                        Matrix4x4.CreateTranslation(centerTranslation));
                }

                public static void RenderSphere(
                    Vector3 center,
                    float radius,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                {
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new SphereData(solid, center, radius, color, depthTestEnabled));
                        return;
                    }

                    XRMeshRenderer renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidSphere : EDebugPrimitiveType.WireSphere, out _);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    //radius doesn't need to be multiplied by 2.0f; the sphere is already 2.0f in diameter
                    renderer.Render(
                        Matrix4x4.CreateScale(radius) * 
                        Matrix4x4.CreateTranslation(center));
                }

                public static void RenderRect2D(BoundingRectangleF bounds, bool solid, ColorF4 color, bool depthTestEnabled = true)
                {
                    RenderQuad(
                        new Vector3(bounds.Center.X, bounds.Center.Y, 0.0f),
                        Rotator.GetZero(),
                        new Vector2(bounds.Extents.X, bounds.Extents.Y),
                        solid,
                        color,
                        depthTestEnabled);
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
                        translation,
                        Matrix4x4.Identity,
                        solid,
                        color,
                        depthTestEnabled,
                        lineWidth);

                public static void RenderBox(
                    Vector3 halfExtents,
                    Vector3 center,
                    Matrix4x4 transform,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                {
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new BoxData(solid, halfExtents, center, transform, color, depthTestEnabled));
                        return;
                    }

                    var renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidBox : EDebugPrimitiveType.WireBox, out _);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    //halfExtents doesn't need to be multiplied by 2.0f; the box is already 1.0f in each direction of each dimension (2.0f extents)
                    renderer.Render(
                        Matrix4x4.CreateScale(halfExtents) *
                        Matrix4x4.CreateTranslation(center) *
                        transform);
                }
                public static void RenderCapsule(
                    Capsule capsule,
                    ColorF4 color,
                    bool depthTestEnabled = true)
                    => RenderCapsule(
                        capsule.Center,
                        capsule.UpAxis,
                        capsule.Radius,
                        capsule.HalfHeight,
                        false,
                        color,
                        depthTestEnabled);
                public static void RenderCapsule(
                    Vector3 start,
                    Vector3 end,
                    float radius,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                    => RenderCapsule(
                        (start + end) * 0.5f,
                        (end - start).Normalized(),
                        radius,
                        Vector3.Distance(start, end) * 0.5f,
                        solid,
                        color,
                        depthTestEnabled,
                        lineWidth);
                public static void RenderCapsule(
                    Vector3 center,
                    Vector3 localUpAxis,
                    float radius,
                    float halfHeight,
                    bool solid,
                    ColorF4 color,
                    bool depthTestEnabled = true,
                    float lineWidth = DefaultLineSize)
                {
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new CapsuleData(solid, new Capsule(center, localUpAxis, radius, halfHeight), color, depthTestEnabled));
                        return;
                    }

                    string cylStr = "_CYLINDER";
                    string topStr = "_TOPHALF";
                    string botStr = "_BOTTOMHALF";

                    _debugPrimitives.TryGetValue(cylStr, out XRMeshRenderer? mCyl);
                    _debugPrimitives.TryGetValue(topStr, out XRMeshRenderer? mTop);
                    _debugPrimitives.TryGetValue(botStr, out XRMeshRenderer? mBot);

                    if (mCyl is null || mTop is null || mBot is null)
                    {
                        XRMesh.Shapes.WireframeCapsuleParts(Vector3.Zero, Globals.Up, 1.0f, 1.0f, 30,
                            out XRMesh cylData, out XRMesh topData, out XRMesh botData);
                        mCyl ??= AssignDebugPrimitive(cylStr, new XRMeshRenderer(cylData, XRMaterial.CreateUnlitColorMaterialForward()));
                        mTop ??= AssignDebugPrimitive(topStr, new XRMeshRenderer(topData, XRMaterial.CreateUnlitColorMaterialForward()));
                        mBot ??= AssignDebugPrimitive(botStr, new XRMeshRenderer(botData, XRMaterial.CreateUnlitColorMaterialForward()));
                    }
                    Vector3 arb = Vector3.UnitX;
                    if (Vector3.Dot(localUpAxis, Vector3.UnitX) > 0.99f || Vector3.Dot(localUpAxis, Vector3.UnitX) < -0.99f)
                        arb = Vector3.UnitZ;
                    Vector3 perp = Vector3.Cross(localUpAxis, arb).Normalized();
                    Matrix4x4 tfm = Matrix4x4.CreateWorld(center, perp, localUpAxis);
                    Matrix4x4 radiusMtx = Matrix4x4.CreateScale(radius);
                    Matrix4x4 cylTransform = Matrix4x4.CreateScale(radius, halfHeight, radius) * tfm;
                    Matrix4x4 topTransform = radiusMtx * Matrix4x4.CreateTranslation(0.0f, halfHeight, 0.0f) * tfm;
                    Matrix4x4 botTransform = radiusMtx * Matrix4x4.CreateTranslation(0.0f, -halfHeight, 0.0f) * tfm;

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
                public static void RenderTriangle(
                    Triangle triangle,
                    ColorF4 color,
                    bool solid,
                    bool depthTestEnabled = true)
                    => RenderTriangle(triangle.A, triangle.B, triangle.C, color, solid, depthTestEnabled);
                public static void RenderTriangle(
                    Vector3 A,
                    Vector3 B,
                    Vector3 C,
                    ColorF4 color,
                    bool solid,
                    bool depthTestEnabled = true)
                {
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new TriangleData(solid, A, B, C, color, depthTestEnabled));
                        return;
                    }

                    EDebugPrimitiveType type = solid ? EDebugPrimitiveType.SolidTriangle : EDebugPrimitiveType.WireTriangle;
                    XRMeshRenderer renderer = GetDebugPrimitive(type, out bool created);
                    if (created)
                    {
                        renderer.Mesh!.PositionsBuffer!.Usage = EBufferUsage.DynamicDraw;
                        renderer.GenerateAsync = false;
                        renderer.Generate();
                    }
                    SetOptions(depthTestEnabled, 1.0f, null, renderer);
                    renderer.Parameter<ShaderVector4>(0)!.Value = color;
                    var posBuf = renderer.Mesh!.PositionsBuffer!;
                    if (solid)
                    {
                        posBuf.Set(0, A);
                        posBuf.Set(0, B);
                        posBuf.Set(0, C);
                    }
                    else
                    {
                        posBuf.Set(0, A);
                        posBuf.Set(0, B);

                        posBuf.Set(0, B);
                        posBuf.Set(0, C);

                        posBuf.Set(0, C);
                        posBuf.Set(0, A);
                    }
                    posBuf.PushSubData();
                    renderer.Render();
                }

                public static void RenderCylinder(Matrix4x4 transform, Vector3 localUpAxis, float radius, float halfHeight, bool solid, ColorF4 color, float lineWidth = DefaultLineSize)
                {
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new CylinderData(solid, transform, localUpAxis, radius, halfHeight, color));
                        return;
                    }

                    throw new NotImplementedException();
                }
                public static void RenderCone(Vector3 center, Vector3 localUpAxis, float radius, float height, bool solid, ColorF4 color, float lineWidth = DefaultLineSize)
                {
                    if (!IsRenderThread)
                    {
                        _debugShapesUpdating.Enqueue(new ConeData(solid, center, localUpAxis, radius, height, color));
                        return;
                    }

                    //SetLineSize(lineWidth);
                    XRMeshRenderer m = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidCone : EDebugPrimitiveType.WireCone, out _);
                    m.Parameter<ShaderVector4>(0)!.Value = color;
                    m.Render(Matrix4x4.CreateScale(radius, radius, height) * XRMath.LookatAngles(localUpAxis).GetMatrix() * Matrix4x4.CreateTranslation(center));
                }

                private static XRMeshRenderer AssignDebugPrimitive(string name, XRMeshRenderer m)
                {
                    if (!_debugPrimitives.TryAdd(name, m))
                        _debugPrimitives[name] = m;
                    return m;
                }

                public enum EDebugPrimitiveType
                {
                    Point,
                    Line,

                    WireTriangle,
                    SolidTriangle,

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

                public static XRMeshRenderer GetDebugPrimitive(EDebugPrimitiveType type, out bool created, Func<XRMaterial>? materialFactory = null)
                {
                    created = false;
                    XRMeshRenderer mesh = _debugPrims[(int)type];
                    if (mesh != null)
                        return mesh;
                    else
                    {
                        created = true;
                        XRMaterial mat = materialFactory?.Invoke() ?? XRMaterial.CreateUnlitColorMaterialForward();
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
                        EDebugPrimitiveType.WireTriangle => XRMesh.CreateLinestrip(true, Vector3.Zero, Vector3.Zero, Vector3.Zero),
                        EDebugPrimitiveType.SolidTriangle => XRMesh.CreateTriangles(Vector3.Zero, Vector3.Zero, Vector3.Zero),
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

                public static void RenderFrustum(Frustum frustum, ColorF4 color)
                {
                    RenderLine(frustum.LeftTopNear, frustum.RightTopNear, color);
                    RenderLine(frustum.RightTopNear, frustum.RightBottomNear, color);
                    RenderLine(frustum.RightBottomNear, frustum.LeftBottomNear, color);
                    RenderLine(frustum.LeftBottomNear, frustum.LeftTopNear, color);
                    RenderLine(frustum.LeftTopFar, frustum.RightTopFar, color);
                    RenderLine(frustum.RightTopFar, frustum.RightBottomFar, color);
                    RenderLine(frustum.RightBottomFar, frustum.LeftBottomFar, color);
                    RenderLine(frustum.LeftBottomFar, frustum.LeftTopFar, color);
                    RenderLine(frustum.LeftTopNear, frustum.LeftTopFar, color);
                    RenderLine(frustum.RightTopNear, frustum.RightTopFar, color);
                    RenderLine(frustum.RightBottomNear, frustum.RightBottomFar, color);
                    RenderLine(frustum.LeftBottomNear, frustum.LeftBottomFar, color);
                }

                public static void RenderShape(IShape shape, bool solid, ColorF4 color)
                {
                    switch (shape)
                    {
                        case Sphere s:
                            RenderSphere(s.Center, s.Radius, solid, color);
                            break;
                        case AABB a:
                            RenderAABB(a.HalfExtents, a.Center, solid, color);
                            break;
                        case Box b:
                            RenderBox(b.LocalHalfExtents, b.LocalCenter, b.Transform, solid, color);
                            break;
                        case Capsule c:
                            RenderCapsule(c.Center, c.UpAxis, c.Radius, c.HalfHeight, solid, color);
                            break;
                        //case Cylinder c:
                        //    RenderCylinder(c.Transform, c.LocalUpAxis, c.Radius, c.HalfHeight, solid, color);
                        //    break;
                        case Cone c:
                            RenderCone(c.Center, c.Up, c.Radius, c.Height, solid, color);
                            break;
                    }
                }
            }
        }
    }
}