using XREngine.Components;
using XREngine.Data.Colors;
using XREngine.Data.Trees;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Data.Components
{
    public class DebugVisualizeQuadtreeComponent : DebugVisualize2DComponent
    {
        public UICanvasComponent? UICanvas { get; set; }

        public UICanvasComponent? GetUICanvas()
        {
            if (UICanvas is not null)
                return UICanvas;

            return GetSiblingComponent<UICanvasComponent>();
        }

        private static List<(QuadtreeNodeBase node, bool intersects)> _quadtreeNodesUpdating = [];
        private static List<(QuadtreeNodeBase node, bool intersects)> _quadtreeNodesRendering = [];
        protected override void RenderInfo_SwapBuffersCallback(RenderInfo info, RenderCommand command, bool shadowPass)
        {
            base.RenderInfo_SwapBuffersCallback(info, command, shadowPass);
            _quadtreeNodesRendering.Clear();
            (_quadtreeNodesUpdating, _quadtreeNodesRendering) = (_quadtreeNodesRendering, _quadtreeNodesUpdating);
        }
        protected override void RenderInfo_PreRenderCallback(RenderInfo info, RenderCommand command, XRCamera? camera, bool shadowPass)
        {
            base.RenderInfo_PreRenderCallback(info, command, camera, shadowPass);
            static void AddNodes((QuadtreeNodeBase node, bool intersects) d)
                => _quadtreeNodesUpdating.Add(d);
            GetUICanvas()?.VisualScene2D.RenderTree?.CollectVisibleNodes(null, false, AddNodes);
        }
        protected override void Render()
        {
            base.Render();

            foreach ((QuadtreeNodeBase node, bool intersects) in _quadtreeNodesRendering)
                Engine.Rendering.Debug.RenderRect2D(
                    node.Bounds,
                    false,
                    intersects 
                        ? Engine.Rendering.Settings.QuadtreeIntersectedBoundsColor
                        : Engine.Rendering.Settings.QuadtreeContainedBoundsColor, 
                    false);
        }
    }
}
