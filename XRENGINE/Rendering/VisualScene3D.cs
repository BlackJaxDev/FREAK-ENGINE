using System.Drawing;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Trees;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Scene
{
    /// <summary>
    /// Represents a scene with special optimizations for rendering in 3D.
    /// </summary>
    public class VisualScene3D : VisualScene
    {
        public VisualScene3D()
        {
            Lights = new Lights3DCollection(this);
        }

        public Lights3DCollection Lights { get; }
        public Octree<RenderInfo3D> RenderTree { get; } = new Octree<RenderInfo3D>(new AABB());

        public void SetBounds(AABB bounds)
        {
            RenderTree.Remake(bounds);
            Lights.LightProbeTree.Remake(bounds);
        }

        public override IRenderTree RenderablesTree => RenderTree;

        public override void GlobalCollectVisible()
        {
            base.GlobalCollectVisible();
            Lights.CollectVisibleItems();
        }

        public override void GlobalPreRender()
        {
            base.GlobalPreRender();
            Lights.RenderShadowMaps(false);
        }

        public override void GlobalSwapBuffers()
        {
            base.GlobalSwapBuffers();
            Lights.SwapBuffers();
        }

        public override void DebugRender(XRCamera? camera, bool onlyContainingItems = false)
            => RenderTree.DebugRender(camera?.WorldFrustum(), onlyContainingItems, RenderAABB);

        private void RenderAABB(Vector3 extents, Vector3 center, Color color)
            => Engine.Rendering.Debug.RenderAABB(extents, center, false, color, true);

        public void Raycast(
            CameraComponent cameraComponent,
            Vector2 normalizedScreenPoint,
            out SortedDictionary<float, List<(RenderInfo3D item, object? data)>> items,
            Func<RenderInfo3D, Segment, (float? distance, object? data)> directTest)
            => RenderTree.Raycast(cameraComponent.Camera.GetWorldSegment(normalizedScreenPoint), out items, directTest);

        public override void CollectRenderedItems(RenderCommandCollection meshRenderCommands, XRCamera? activeCamera, bool cullWithFrustum, Func<XRCamera>? cullingCameraOverride, bool shadowPass)
        {
            var cullingCamera = cullingCameraOverride?.Invoke() ?? activeCamera;
            var collectionVolume = cullWithFrustum ? cullingCamera?.WorldFrustum() : null;
            CollectRenderedItems(meshRenderCommands, collectionVolume, activeCamera, shadowPass);

        }
        public void CollectRenderedItems(RenderCommandCollection commands, IVolume? collectionVolume, XRCamera? camera, bool shadowPass)
        {
            bool IntersectionTest(RenderInfo3D item, IVolume? cullingVolume, bool containsOnly)
                => item.AllowRender(cullingVolume, commands, camera, shadowPass);

            void AddRenderCommands(ITreeItem item)
            {
                if (item is RenderInfo renderable)
                    renderable.AddRenderCommands(commands, camera, shadowPass);
            }

            RenderTree.CollectVisible(collectionVolume, false, AddRenderCommands, IntersectionTest);
        }
    }
}