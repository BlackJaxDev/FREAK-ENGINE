using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Models.Materials;
using XREngine.Scene;

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
                public static BoundingRectangle RenderArea => RenderingPipelineState?.CurrentRenderRegion ?? BoundingRectangle.Empty;
                public static XRWorldInstance? RenderingWorld => RenderingViewport?.World;
                public static XRViewport? RenderingViewport => RenderingPipelineState?.WindowViewport;
                public static VisualScene? RenderingScene => RenderingPipelineState?.Scene;
                public static XRCamera? RenderingCamera => RenderingPipelineState?.RenderingCamera;
                public static XRFrameBuffer? RenderingTargetOutputFBO => RenderingPipelineState?.OutputFBO;

                public static XRMaterial? OverrideMaterial => RenderingPipelineState?.OverrideMaterial;

                private static Stack<XRRenderPipelineInstance> RenderingPipelineStack { get; } = new();
                private static Stack<XRRenderPipelineInstance> CollectingVisiblePipelineStack { get; } = new();

                public static StateObject PushRenderingPipeline(XRRenderPipelineInstance pipeline)
                {
                    RenderingPipelineStack.Push(pipeline);
                    return new StateObject(PopRenderingPipeline);
                }
                public static StateObject PushCollectingVisiblePipeline(XRRenderPipelineInstance pipeline)
                {
                    CollectingVisiblePipelineStack.Push(pipeline);
                    return new StateObject(PopCollectingVisiblePipeline);
                }

                public static void PopRenderingPipeline()
                {
                    if (RenderingPipelineStack.Count > 0)
                        RenderingPipelineStack.Pop();
                }
                public static void PopCollectingVisiblePipeline()
                {
                    if (CollectingVisiblePipelineStack.Count > 0)
                        CollectingVisiblePipelineStack.Pop();
                }

                public static XRRenderPipelineInstance? CurrentRenderingPipeline => RenderingPipelineStack.Count > 0 ? RenderingPipelineStack.Peek() : null;
                public static XRRenderPipelineInstance.RenderingState? RenderingPipelineState => CurrentRenderingPipeline?.RenderState;

                public static XRRenderPipelineInstance? CurrentCollectingVisiblePipeline => CollectingVisiblePipelineStack.Count > 0 ? CollectingVisiblePipelineStack.Peek() : null;
                public static XRRenderPipelineInstance.RenderingState? CollectingVisiblePipelineState => CurrentCollectingVisiblePipeline?.RenderState;

                public static void ClearColor(ColorF4 color)
                    => AbstractRenderer.Current?.ClearColor(color);
                public static void ClearStencil(int v)
                    => AbstractRenderer.Current?.ClearStencil(v);
                public static void ClearDepth(float v)
                    => AbstractRenderer.Current?.ClearDepth(v);

                public static void Clear(bool color, bool depth, bool stencil)
                    => AbstractRenderer.Current?.Clear(color, depth, stencil);
                public static void ClearByBoundFBO()
                {
                    var boundFBO = XRFrameBuffer.BoundForWriting;
                    if (boundFBO is not null)
                    {
                        var textureTypes = boundFBO.TextureTypes;
                        Clear(
                            textureTypes.HasFlag(EFrameBufferTextureTypeFlags.Color),
                            textureTypes.HasFlag(EFrameBufferTextureTypeFlags.Depth),
                            textureTypes.HasFlag(EFrameBufferTextureTypeFlags.Stencil));
                    }
                    else
                        Clear(true, true, true);
                }

                public static void UnbindFrameBuffers(EFramebufferTarget target)
                    => BindFrameBuffer(target, null);
                private static void BindFrameBuffer(EFramebufferTarget fboTarget, XRFrameBuffer? fbo)
                    => AbstractRenderer.Current?.BindFrameBuffer(fboTarget, fbo);
                public static void SetReadBuffer(EReadBufferMode mode)
                    => AbstractRenderer.Current?.SetReadBuffer(mode);
                
                public static void SetReadBuffer(XRFrameBuffer? fbo, EReadBufferMode mode)
                    => AbstractRenderer.Current?.SetReadBuffer(fbo, mode);

                public static float GetDepth(float x, float y)
                    => AbstractRenderer.Current?.GetDepth(x, y) ?? 0.0f;

                public static byte GetStencilIndex(float x, float y)
                    => AbstractRenderer.Current?.GetStencilIndex(x, y) ?? 0;

                public static void EnableDepthTest(bool enable)
                    => AbstractRenderer.Current?.EnableDepthTest(enable);

                public static void StencilMask(uint mask)
                    => AbstractRenderer.Current?.StencilMask(mask);

                public static void AllowDepthWrite(bool allow)
                    => AbstractRenderer.Current?.AllowDepthWrite(allow);

                public static void DepthFunc(EComparison always)
                    => AbstractRenderer.Current?.DepthFunc(always);

                public static bool TryCalculateDotLuminance(XRTexture2D texture, out float dotLuminance, bool genMipmapsNow)
                {
                    dotLuminance = 1.0f;
                    return AbstractRenderer.Current?.CalcDotLuminance(texture, out dotLuminance, genMipmapsNow) ?? false;
                }
                public static float CalculateDotLuminance(XRTexture2D texture, bool generateMipmapsNow)
                    => TryCalculateDotLuminance(texture, out float dotLum, generateMipmapsNow) ? dotLum : 1.0f;
            }
        }
    }
}