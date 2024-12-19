using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Trees;

namespace XREngine.Data
{
    public interface IOctreeItem : ITreeItem
    {
        AABB? LocalCullingVolume { get; }
        Matrix4x4 CullingOffsetMatrix { get; }
        Box? WorldCullingVolume => LocalCullingVolume?.ToBox(CullingOffsetMatrix);
        OctreeNodeBase? OctreeNode { get; set; }
    }
}