using XREngine.Data.Rendering;
using XREngine.Data.Trees;

namespace XREngine.Data
{
    public interface IOctreeItem : ITreeItem
    {
        IVolume? CullingVolume { get; }
        OctreeNodeBase? OctreeNode { get; set; }
        bool Intersects(IVolume cullingVolume, bool containsOnly);
    }
}