using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Trees;

namespace System
{
    public interface IQuadtreeItem : ITreeItem
    {
        BoundingRectangleF? CullingVolume { get; }
        QuadtreeNodeBase? QuadtreeNode { get; set; }
        bool Intersects(BoundingRectangleF cullingVolume, bool containsOnly);
        bool Contains(Vector2 point);
        Vector2 ClosestPoint(Vector2 point);
        bool DeeperThan(IQuadtreeItem other);
    }
}
