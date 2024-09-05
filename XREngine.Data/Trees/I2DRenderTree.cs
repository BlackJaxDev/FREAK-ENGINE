using System.Drawing;
using System.Numerics;
using XREngine.Data.Geometry;

namespace XREngine.Data.Trees
{
    public delegate void DelRenderBounds(Vector2 extents, Vector2 center, Color color);
    public interface I2DRenderTree : IRenderTree
    {
        void Remake(BoundingRectangleF newBounds);
        void DebugRender(BoundingRectangleF volume, bool onlyContainingItems, DelRenderBounds render);
        void CollectIntersecting(BoundingRectangleF region, bool onlyContainingItems, Action<IQuadtreeItem> action);
        void CollectAll(Action<IQuadtreeItem> action);
    }
    public interface I2DRenderTree<T> : IRenderTree<T>, I2DRenderTree where T : class, IQuadtreeItem
    {
        void CollectIntersecting(BoundingRectangleF region, bool onlyContainingItems, Action<T> action);
    }
}
