using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models.Materials;

namespace XREngine
{
    public static partial class Engine
    {
        /// <summary>
        /// This static class contains all rendering-related functionality.
        /// </summary>
        public static partial class Rendering
        {
            /// <summary>
            /// This static class dictates the current state of rendering.
            /// </summary>
            public static partial class State
            {
                public static XRCamera? RenderingCamera
                    => RenderingCameras.TryPeek(out var c) ? c : null;
                public static Stack<XRCamera> RenderingCameras { get; } = new();
                public static StateObject PushRenderingCamera(XRCamera camera)
                {
                    RenderingCameras.Push(camera);
                    return new StateObject(() => RenderingCameras.Pop());
                }

                public static BoundingRectangle RenderArea
                    => RenderAreas.TryPeek(out var area) ? area : BoundingRectangle.Empty;
                public static Stack<BoundingRectangle> RenderAreas { get; } = new();
                public static StateObject PushRenderArea(BoundingRectangle area)
                {
                    RenderAreas.Push(area);
                    return new StateObject(() => RenderAreas.Pop());
                }

                public static XRWorldInstance? RenderingWorld
                    => RenderingWorlds.TryPeek(out var w) ? w : null;
                public static Stack<XRWorldInstance> RenderingWorlds { get; } = new();
                public static StateObject PushRenderingWorld(XRWorldInstance camera)
                {
                    RenderingWorlds.Push(camera);
                    return new StateObject(() => RenderingWorlds.Pop());
                }

                public static XRMaterial? OverrideMaterial
                    => OverrideMaterials.TryPeek(out var m) ? m : null;
                /// <summary>
                /// This material will be used to render all objects in the scene if set.
                /// </summary>
                public static Stack<XRMaterial> OverrideMaterials { get; } = new();
                public static StateObject PushOverrideMaterial(XRMaterial material)
                {
                    OverrideMaterials.Push(material);
                    return new StateObject(() => OverrideMaterials.Pop());
                }

                public static XRViewport? RenderingViewport
                    => RenderingViewports.TryPeek(out var vp) ? vp : null;
                public static Stack<XRViewport> RenderingViewports { get; } = new();

                public static XRRenderPipeline? RenderPipeline { get; set; }

                public static StateObject PushRenderingViewport(XRViewport vp)
                {
                    RenderingViewports.Push(vp);
                    return new StateObject(() => RenderingViewports.Pop());
                }

                public static void Clear(ColorF4 color)
                    => AbstractRenderer.Current?.ClearColor(color);

                public static void Clear(bool color, bool depth, bool stencil)
                    => AbstractRenderer.Current?.Clear(color, depth, stencil);

                public static void BindFrameBuffer(EFramebufferTarget fboTarget, int bindingId)
                    => AbstractRenderer.Current?.BindFrameBuffer(fboTarget, bindingId);

                public static void SetReadBuffer(EDrawBuffersAttachment attachment)
                    => AbstractRenderer.Current?.SetReadBuffer(attachment);

                public static float GetDepth(float x, float y)
                    => AbstractRenderer.Current?.GetDepth(x, y) ?? 0.0f;

                public static byte GetStencilIndex(float x, float y)
                    => AbstractRenderer.Current?.GetStencilIndex(x, y) ?? 0;

                public static void EnableDepthTest(bool v)
                    => AbstractRenderer.Current?.EnableDepthTest(v);

                public static void StencilMask(uint mask)
                    => AbstractRenderer.Current?.StencilMask(mask);

                public static void ClearStencil(int v)
                    => AbstractRenderer.Current?.ClearStencil(v);

                public static void ClearDepth(float v)
                    => AbstractRenderer.Current?.ClearDepth(v);

                public static void AllowDepthWrite(bool v)
                    => AbstractRenderer.Current?.AllowDepthWrite(v);

                public static void DepthFunc(EComparison always)
                    => AbstractRenderer.Current?.DepthFunc(always);

                public static bool CalcDotLuminance(XRTexture2D texture, out float dotLuminance, bool genMipmapsNow)
                {
                    dotLuminance = 1.0f;
                    return AbstractRenderer.Current?.CalcDotLuminance(texture, out dotLuminance, genMipmapsNow) ?? false;
                }
                public static float CalculateDotLuminance(XRTexture2D texture, bool generateMipmapsNow)
                    => CalcDotLuminance(texture, out float dotLum, generateMipmapsNow) ? dotLum : 1.0f;
            }
        }
    }
}