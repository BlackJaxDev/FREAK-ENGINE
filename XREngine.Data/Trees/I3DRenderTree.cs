using System.Drawing;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;

namespace XREngine.Data.Trees
{
    public delegate void DelRenderAABB(Vector3 extents, Vector3 center, Color color);
    public interface I3DRenderTree
    {
        void Remake(AABB newBounds);
        void DebugRender(IVolume volume, bool onlyContainingItems, DelRenderAABB render);
        void CollectIntersecting(IVolume volume, bool onlyContainingItems, Action<IOctreeItem> action);
        void CollectAll(Action<IOctreeItem> action);
    }
    public interface I3DRenderTree<T> : I3DRenderTree, IRenderTree<T> where T : class, IOctreeItem
    {
        void CollectIntersecting(IVolume volume, bool onlyContainingItems, Action<T> action);
    }
}
