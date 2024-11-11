﻿using Extensions;
using System.Numerics;
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
                    XRMeshRenderer renderer = GetDebugPrimitive(EDebugPrimitiveType.Line, out _);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    Vector3 dir = (end - start).Normalize();
                    Vector3 arb = Vector3.UnitX;
                    if (Vector3.Dot(dir, Vector3.UnitX) > 0.99f || Vector3.Dot(dir, Vector3.UnitX) < -0.99f)
                        arb = Vector3.UnitZ;
                    Vector3 perp = Vector3.Cross(dir, arb).Normalize();
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
                    XRMeshRenderer renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidSphere : EDebugPrimitiveType.WireSphere, out _);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    //radius doesn't need to be multiplied by 2.0f; the sphere is already 2.0f in diameter
                    renderer.Render(
                        Matrix4x4.CreateScale(radius) * 
                        Matrix4x4.CreateTranslation(center));
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
                    var renderer = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidBox : EDebugPrimitiveType.WireBox, out _);
                    SetOptions(depthTestEnabled, lineWidth, null, renderer);
                    renderer.SetParameter(0, color);
                    //halfExtents doesn't need to be multiplied by 2.0f; the box is already 1.0f in each direction of each dimension (2.0f extents)
                    renderer.Render(
                        Matrix4x4.CreateScale(halfExtents) *
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
                    Vector3 center,
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

                    Matrix4x4 tfm = Matrix4x4.CreateWorld(center, Vector3.Cross(localUpAxis, Vector3.UnitX), localUpAxis);
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
                    Triangle value,
                    ColorF4 color,
                    bool solid,
                    bool depthTestEnabled = true)
                {
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
                        posBuf.Set(0, value.A);
                        posBuf.Set(0, value.B);
                        posBuf.Set(0, value.C);
                    }
                    else
                    {
                        posBuf.Set(0, value.A);
                        posBuf.Set(0, value.B);

                        posBuf.Set(0, value.B);
                        posBuf.Set(0, value.C);

                        posBuf.Set(0, value.C);
                        posBuf.Set(0, value.A);
                    }
                    posBuf.PushSubData();
                    renderer.Render();
                }

                public static void RenderCylinder(Matrix4x4 transform, Vector3 localUpAxis, float radius, float halfHeight, bool solid, ColorF4 color, float lineWidth = DefaultLineSize)
                {
                    throw new NotImplementedException();
                }
                public static void RenderCone(Matrix4x4 transform, Vector3 localUpAxis, float radius, float height, bool solid, ColorF4 color, float lineWidth = DefaultLineSize)
                {
                    //SetLineSize(lineWidth);
                    XRMeshRenderer m = GetDebugPrimitive(solid ? EDebugPrimitiveType.SolidCone : EDebugPrimitiveType.WireCone, out _);
                    m.Parameter<ShaderVector4>(0)!.Value = color;
                    transform = transform * XRMath.LookatAngles(localUpAxis).GetMatrix() * Matrix4x4.CreateScale(radius, radius, height);
                    m.Render(transform);
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
            }
        }
    }
}