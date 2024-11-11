using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Data.Trees;

namespace XREngine.Data
{
    public interface IOctreeItem : ITreeItem
    {
        AABB? LocalCullingVolume { get; }
        OctreeNodeBase? OctreeNode { get; set; }
    }
}