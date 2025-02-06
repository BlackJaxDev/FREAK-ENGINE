using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;

namespace XREngine.Data.Trees
{
    public abstract class OctreeNodeBase(AABB bounds, int subDivIndex, int subDivLevel) : XRBase, ITreeNode
    {
        public const float MinVolume = 1f;

        protected int _subDivIndex = subDivIndex, _subDivLevel = subDivLevel;
        protected AABB _bounds = bounds;
        public int SubDivIndex { get => _subDivIndex; set => _subDivIndex = value; }
        public int SubDivLevel { get => _subDivLevel; set => _subDivLevel = value; }
        public AABB Bounds => _bounds;
        public Vector3 Center => _bounds.Center;
        public Vector3 Min => _bounds.Min;
        public Vector3 Max => _bounds.Max;
        public abstract OctreeNodeBase? GenericParent { get; }

        protected abstract OctreeNodeBase? GetNodeInternal(int index);
        public abstract void HandleMovedItem(IOctreeItem item);
        public abstract bool Remove(IOctreeItem item, out bool destroyNode);

        public AABB? GetSubdivision(int index)
        {
            OctreeNodeBase? node = GetNodeInternal(index);
            if (node != null)
                return node.Bounds;

            if (Min.X >= Max.X ||
                Min.Y >= Max.Y ||
                Min.Z >= Max.Z)
                return null;

            Vector3 center = Center;
            AABB? box = index switch
            {
                0 => new(new Vector3(Min.X, Min.Y, Min.Z), new Vector3(center.X, center.Y, center.Z)),
                1 => new(new Vector3(Min.X, Min.Y, center.Z), new Vector3(center.X, center.Y, Max.Z)),
                2 => new(new Vector3(Min.X, center.Y, Min.Z), new Vector3(center.X, Max.Y, center.Z)),
                3 => new(new Vector3(Min.X, center.Y, center.Z), new Vector3(center.X, Max.Y, Max.Z)),
                4 => new(new Vector3(center.X, Min.Y, Min.Z), new Vector3(Max.X, center.Y, center.Z)),
                5 => new(new Vector3(center.X, Min.Y, center.Z), new Vector3(Max.X, center.Y, Max.Z)),
                6 => new(new Vector3(center.X, center.Y, Min.Z), new Vector3(Max.X, Max.Y, center.Z)),
                7 => new(new Vector3(center.X, center.Y, center.Z), new Vector3(Max.X, Max.Y, Max.Z)),
                _ => null,
            };

            if (box is not null && box.Value.Volume < MinVolume)
                return null;

            return box;
        }

        public virtual void QueueItemMoved(IOctreeItem item)
        {

        }

        public virtual void Destroy()
            => GenericParent?.RemoveNodeAt(SubDivIndex);

        protected abstract void RemoveNodeAt(int subDivIndex);
    }
}
