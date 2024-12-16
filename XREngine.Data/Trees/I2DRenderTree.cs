using System.Numerics;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;

namespace XREngine.Data.Trees
{
    public delegate void DelRenderBounds(Vector2 extents, Vector2 center, ColorF4 color);
    public interface I2DRenderTree : IRenderTree
    {
        void Remake(BoundingRectangleF newBounds);
        void DebugRender(BoundingRectangleF? volume, bool onlyContainingItems, DelRenderBounds render);
        void CollectVisible(BoundingRectangleF? region, bool onlyContainingItems, Action<IQuadtreeItem> action, QuadtreeNode<IQuadtreeItem>.DelIntersectionTestGeneric intersectionTest);
        void CollectAll(Action<IQuadtreeItem> action);
        void CollectVisibleNodes(BoundingRectangleF? cullingVolume, bool containsOnly, Action<(QuadtreeNodeBase node, bool intersects)> action);
        void Raycast<T2>(
            Vector2 point,
            SortedDictionary<float, List<(T2 item, object? data)>> items) where T2 : class, IRenderableBase;
    }
    public interface I2DRenderTree<T> : IRenderTree<T>, I2DRenderTree where T : class, IQuadtreeItem
    {
        void CollectVisible(BoundingRectangleF? region, bool onlyContainingItems, Action<T> action, QuadtreeNode<T>.DelIntersectionTest intersectionTest);
    }
}
