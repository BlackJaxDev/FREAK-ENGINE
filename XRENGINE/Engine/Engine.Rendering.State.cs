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
                public static XRViewport? CurrentlyRenderingViewport
                    => RenderingViewports.TryPeek(out var vp) ? vp : null;

                public static XRCamera? CurrentlyRenderingCamera
                    => RenderingCameras.TryPeek(out var c) ? c : null;

                public static XRWorldInstance? CurrentlyRenderingWorld
                    => RenderingWorlds.TryPeek(out var w) ? w : null;

                public static Stack<XRCamera> RenderingCameras { get; } = new();
                public static StateObject PushRenderingCamera(XRCamera camera)
                {
                    RenderingCameras.Push(camera);
                    return new StateObject(() => RenderingCameras.Pop());
                }

                public static Stack<BoundingRectangle> RenderAreas { get; } = new();
                public static StateObject PushRenderArea(BoundingRectangle area)
                {
                    RenderAreas.Push(area);
                    return new StateObject(() => RenderAreas.Pop());
                }

                public static Stack<XRWorldInstance> RenderingWorlds { get; } = new();
                public static StateObject PushRenderingWorld(XRWorldInstance camera)
                {
                    RenderingWorlds.Push(camera);
                    return new StateObject(() => RenderingWorlds.Pop());
                }

                /// <summary>
                /// This material will be used to render all objects in the scene if set.
                /// </summary>
                public static Stack<XRMaterial> OverrideMaterials { get; } = new();
                public static StateObject PushOverrideMaterial(XRMaterial material)
                {
                    OverrideMaterials.Push(material);
                    return new StateObject(() => OverrideMaterials.Pop());
                }

                public static Stack<XRViewport> RenderingViewports { get; } = new();
                public static StateObject PushRenderingViewport(XRViewport vp)
                {
                    RenderingViewports.Push(vp);
                    return new StateObject(() => RenderingViewports.Pop());
                }

                public static void Clear(ColorF4 color)
                {
                    throw new NotImplementedException();
                }

                public static void Clear(EFrameBufferTextureType type)
                {
                    throw new NotImplementedException();
                }

                internal static void BindFrameBuffer(EFramebufferTarget readFramebuffer, int v)
                {
                    throw new NotImplementedException();
                }

                internal static void SetReadBuffer(EDrawBuffersAttachment none)
                {
                    throw new NotImplementedException();
                }

                internal static float GetDepth(float x, float y)
                {
                    throw new NotImplementedException();
                }

                internal static byte GetStencilIndex(float x, float y)
                {
                    throw new NotImplementedException();
                }

                internal static void EnableDepthTest(bool v)
                {
                    throw new NotImplementedException();
                }

                internal static void StencilMask(int v)
                {
                    throw new NotImplementedException();
                }

                internal static void ClearStencil(int v)
                {
                    throw new NotImplementedException();
                }

                internal static void ClearDepth(float v)
                {
                    throw new NotImplementedException();
                }

                internal static void AllowDepthWrite(bool v)
                {
                    throw new NotImplementedException();
                }

                internal static void DepthFunc(EComparison always)
                {
                    throw new NotImplementedException();
                }

                public class StateObject(Action onStateEnded) : IDisposable
                {
                    void IDisposable.Dispose()
                    {
                        onStateEnded();
                        GC.SuppressFinalize(this);
                    }
                }
            }
        }
    }
}