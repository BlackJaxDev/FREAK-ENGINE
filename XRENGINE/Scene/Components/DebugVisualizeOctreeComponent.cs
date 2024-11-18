using XREngine.Data.Colors;
using XREngine.Data.Trees;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene;

namespace XREngine.Data.Components
{
    public class DebugVisualizeOctreeComponent : DebugVisualize3DComponent
    {
        private static List<(OctreeNodeBase node, bool intersects)> _octreeNodesUpdating = [];
        private static List<(OctreeNodeBase node, bool intersects)> _octreeNodesRendering = [];

        protected override void RenderInfo_SwapBuffersCallback(RenderInfo info, RenderCommand command, bool shadowPass)
        {
            if (shadowPass)
                return;

            base.RenderInfo_SwapBuffersCallback(info, command, shadowPass);

            //Debug.LogWarning("DebugVisualizeOctreeComponent.RenderInfo_SwapBuffersCallback");
            _octreeNodesRendering.Clear();
            (_octreeNodesUpdating, _octreeNodesRendering) = (_octreeNodesRendering, _octreeNodesUpdating);
        }
        protected override void RenderInfo_PreRenderCallback(RenderInfo info, RenderCommand command, XRCamera? camera, bool shadowPass)
        {
            base.RenderInfo_PreRenderCallback(info, command, camera, shadowPass);

            if (shadowPass || World?.VisualScene is not VisualScene3D scene)
                return;

            static void AddNodes((OctreeNodeBase node, bool intersects) d)
                => _octreeNodesUpdating.Add(d);

            //Debug.LogWarning("DebugVisualizeOctreeComponent.RenderInfo_PreRenderCallback");
            scene.RenderTree.CollectVisibleNodes(camera?.WorldFrustum(), false, AddNodes);
        }

        protected override void Render(bool shadowPass)
        {
            base.Render(shadowPass);

            if (shadowPass)
                return;

            //Debug.LogWarning("DebugVisualizeOctreeComponent.Render");
            foreach ((OctreeNodeBase node, bool intersects) in _octreeNodesRendering)
                Engine.Rendering.Debug.RenderAABB(node.Bounds.HalfExtents, node.Center, false, intersects ? ColorF4.Red : ColorF4.White, false);
        }
    }
}
