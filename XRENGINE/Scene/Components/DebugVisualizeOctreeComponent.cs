using XREngine.Data.Colors;
using XREngine.Data.Trees;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene;

namespace XREngine.Data.Components
{
    /// <summary>
    /// Renders a debug visualization of the scene's octree.
    /// Not intended for production use.
    /// </summary>
    public class DebugVisualizeOctreeComponent : DebugVisualize3DComponent
    {
        private static List<(OctreeNodeBase node, bool intersects)> _octreeNodesUpdating = [];
        private static List<(OctreeNodeBase node, bool intersects)> _octreeNodesRendering = [];

        protected override void RenderInfo_SwapBuffersCallback(RenderInfo info, RenderCommand command, bool shadowPass)
        {
            if (shadowPass)
                return;

            base.RenderInfo_SwapBuffersCallback(info, command, shadowPass);

            _octreeNodesRendering.Clear();
            (_octreeNodesUpdating, _octreeNodesRendering) = (_octreeNodesRendering, _octreeNodesUpdating);
        }
        protected override void RenderInfo_PreRenderCallback(RenderInfo info, RenderCommand command, XRCamera? camera, bool shadowPass)
        {
            if (shadowPass)
                return;

            base.RenderInfo_PreRenderCallback(info, command, camera, shadowPass);

            static void AddNodes((OctreeNodeBase node, bool intersects) d)
                => _octreeNodesUpdating.Add(d);

            World?.VisualScene?.RenderTree?.CollectVisibleNodes(camera?.WorldFrustum(), false, AddNodes);
        }

        protected override void Render(bool shadowPass)
        {
            if (shadowPass)
                return;

            base.Render(shadowPass);

            foreach ((OctreeNodeBase node, bool intersects) in _octreeNodesRendering)
                Engine.Rendering.Debug.RenderAABB(node.Bounds.HalfExtents, node.Center, false, intersects ? ColorF4.Red : ColorF4.Yellow, false);
        }
    }
}
