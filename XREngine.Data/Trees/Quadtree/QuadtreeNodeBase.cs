using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;

namespace XREngine.Data.Trees
{
    public abstract class QuadtreeNodeBase(BoundingRectangleF bounds, int subDivIndex, int subDivLevel) : XRBase, ITreeNode
    {
        protected int _subDivIndex = subDivIndex, _subDivLevel = subDivLevel;
        protected BoundingRectangleF _bounds = bounds;

        public BoundingRectangleF Bounds => _bounds;
        public Vector2 Center => _bounds.Translation;
        public Vector2 Min => _bounds.BottomLeft;
        public Vector2 Max => _bounds.TopRight;
        public Vector2 Extents => _bounds.Extents;

        protected abstract QuadtreeNodeBase? GetNodeInternal(int index);
        public abstract void HandleMovedItem(IQuadtreeItem item);
        public virtual void QueueItemMoved(IQuadtreeItem item) { }
    }
}
