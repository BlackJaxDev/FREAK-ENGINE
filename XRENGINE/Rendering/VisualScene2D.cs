using System.Drawing;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Data.Trees;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using Vector3 = System.Numerics.Vector3;

namespace XREngine.Scene
{
    /// <summary>
    /// Represents a scene with special optimizations for rendering in 2D.
    /// </summary>
    public class VisualScene2D : VisualScene
    {
        public VisualScene2D() { }

        public Quadtree<RenderInfo2D> RenderTree { get; } = new Quadtree<RenderInfo2D>(new BoundingRectangleF());

        public void SetBounds(BoundingRectangleF bounds)
            => RenderTree.Remake(bounds);

        public override void DebugRender(XRCamera? camera, bool onlyContainingItems = false)
            => RenderTree.DebugRender(camera?.GetOrthoCameraBounds(), onlyContainingItems, RenderAABB);

        private void RenderAABB(Vector2 extents, Vector2 center, ColorF4 color)
            => Engine.Rendering.Debug.RenderQuad(new Vector3(center, 0.0f) + AbstractRenderer.UIPositionBias, AbstractRenderer.UIRotation, extents, false, color, false, 1.0f);

        public override IRenderTree RenderablesTree => RenderTree;

        public void Raycast(
            CameraComponent cameraComponent,
            Vector2 normalizedScreenPoint,
            out SortedDictionary<float, List<(RenderInfo2D item, object? data)>> items,
            Func<RenderInfo2D, Segment, (float? distance, object? data)> directTest)
            => RenderTree.Raycast(cameraComponent.Camera.GetWorldSegment(normalizedScreenPoint), out items, directTest);

        public override void CollectRenderedItems(RenderCommandCollection meshRenderCommands, XRCamera? activeCamera, bool cullWithFrustum, Func<XRCamera>? cullingCameraOverride, bool shadowPass)
        {
            var cullingCamera = cullingCameraOverride?.Invoke() ?? activeCamera;
            CollectRenderedItems(meshRenderCommands, cullWithFrustum ? cullingCamera?.GetOrthoCameraBounds() : null, activeCamera);
        }
        /// <summary>
        /// Collects render commands for all renderables in the scene that intersect with the given volume.
        /// If the volume is null, all renderables are collected.
        /// Typically, the collectionVolume is the camera's frustum.
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="collectionVolume"></param>
        /// <param name="camera"></param>
        public void CollectRenderedItems(RenderCommandCollection commands, BoundingRectangleF? collectionVolume, XRCamera? camera)
        {
            bool IntersectionTest(RenderInfo2D item, BoundingRectangleF cullingVolume, bool containsOnly)
            {
                if (item.CullingVolume is null)
                    return false;
                
                var contain = cullingVolume.ContainmentOf(item.CullingVolume.Value);
                return containsOnly ? contain == EContainment.Contains : contain != EContainment.Disjoint;
            }
            void AddRenderCommands(ITreeItem item)
            {
                if (item is RenderInfo renderable)
                    renderable.AddRenderCommands(commands, camera, false);
            }
            if (collectionVolume is null)
                RenderTree.CollectAll(AddRenderCommands);
            else
                RenderTree.CollectVisible(collectionVolume, false, AddRenderCommands, IntersectionTest);
        }
    }
}