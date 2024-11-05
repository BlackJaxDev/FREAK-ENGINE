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
    /// Represents a scene with special optimizations for rendering in 2D.
    /// </summary>
    public class VisualScene2D : VisualScene
    {
        public VisualScene2D() { }

        public Quadtree<RenderInfo2D> RenderTree { get; } = new Quadtree<RenderInfo2D>(new BoundingRectangleF());

        public void SetBounds(BoundingRectangleF bounds)
        {
            RenderTree.Remake(bounds);
        }

        public override IRenderTree RenderablesTree => RenderTree;

        public void Raycast(CameraComponent cameraComponent, Vector2 screenPoint, out SortedDictionary<float, RenderInfo2D> items, Func<RenderInfo2D, Segment, float?> directTest)
            => RenderTree.Raycast(cameraComponent.Camera.GetWorldSegment(screenPoint), out items, directTest);
    }

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

        public void Raycast(CameraComponent cameraComponent, Vector2 screenPoint, out SortedDictionary<float, RenderInfo3D> items, Func<RenderInfo3D, Segment, float?> directTest)
            => RenderTree.Raycast(cameraComponent.Camera.GetWorldSegment(screenPoint), out items, directTest);
    }
}