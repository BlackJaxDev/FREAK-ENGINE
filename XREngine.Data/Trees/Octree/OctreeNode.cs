using System.Drawing;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;

namespace XREngine.Data.Trees
{
    /// <summary>
    /// A node represents an AABB octant in the octree.
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="subDivIndex"></param>
    /// <param name="subDivLevel"></param>
    /// <param name="parent"></param>
    /// <param name="owner"></param>
    public class OctreeNode<T>(AABB bounds, int subDivIndex, int subDivLevel, OctreeNode<T>? parent, Octree<T> owner)
        : OctreeNodeBase(bounds, subDivIndex, subDivLevel) where T : class, IOctreeItem
    {
        protected List<T> _items = [];
        protected OctreeNode<T>?[] _subNodes = new OctreeNode<T>[OctreeBase.MaxChildNodeCount];
        protected OctreeNode<T>? _parentNode = parent;
        //private readonly ReaderWriterLockSlim _lock;

        protected override OctreeNodeBase? GetNodeInternal(int index)
            => _subNodes[index];

        public Octree<T> Owner { get; set; } = owner;
        public OctreeNode<T>? ParentNode { get => _parentNode; set => _parentNode = value; }
        public List<T> Items => _items;

        #region Child movement
        public void ItemMoved(T item)
        {
            //TODO: if the item is the only item within its volume, no need to subdivide more!!!
            //However, if the item is inserted into a volume with at least one other item in it, 
            //need to try subdividing for all items at that point.

            if (item?.CullingVolume != null)
                Owner.MovedItems.Enqueue(item);
        }
        public override void HandleMovedItem(IOctreeItem item)
        {
            if (item is not T t)
                return;

            //Still within the same volume?
            if (t.CullingVolume != null &&
                ((IVolume)_bounds).Contains(t.CullingVolume) == EContainment.Contains)
            {
                //Try subdividing
                for (int i = 0; i < OctreeBase.MaxChildNodeCount; ++i)
                {
                    AABB? bounds = GetSubdivision(i);
                    if (bounds is null)
                        return;
                    if (((IVolume)bounds.Value).Contains(t.CullingVolume) == EContainment.Contains)
                    {
                        bool shouldDestroy = RemoveHereOrSmaller(t);
                        if (shouldDestroy)
                            ClearSubNode(_subDivIndex);
                        CreateSubNode(bounds.Value, i)?.AddHereOrSmaller(t);
                        break;
                    }
                }
            }
            else if (ParentNode != null)
            {
                //Belongs in larger parent volume, remove from this node
                bool shouldDestroy = RemoveHereOrSmaller(t);
                if (!ParentNode.TryAddUp(t, shouldDestroy ? _subDivIndex : -1))
                {
                    //Force add to root node
                    Owner._head.AddHere(t);
                }
            }
        }
        /// <summary>
        /// Moves up the heirarchy instead of down to add an item.
        /// Returns true if the item was added to this node.
        /// </summary>
        private bool TryAddUp(T item, int childDestroyIndex)
        {
            ClearSubNode(childDestroyIndex);

            if (AddHereOrSmaller(item, childDestroyIndex))
                return true;
            else
            {
                bool shouldDestroy = _items.Count == 0 && HasNoSubNodesExcept(-1);
                if (ParentNode != null)
                    return ParentNode.TryAddUp(item, shouldDestroy ? _subDivIndex : -1);
            }

            return false;
        }
        #endregion

        #region Debug
        /// <summary>
        /// Renders the octree using debug bounding boxes.
        /// Boxes inside the collection voluem render in white, intersecting in green, and disjoint in red.
        /// </summary>
        /// <param name="renderChildren">If true, child boxes will be rendered recursively. If false, only this box will be rendered.</param>
        /// <param name="onlyContainingItems">If true, only boxes fully contained in the collection volume will render. If false, interesecting boxes will also render.</param>
        /// <param name="collectionVolume">The volume to use to determine what boxes to render. If null, all will render.</param>
        /// <param name="render">The delegate used to render boxes. Pass your own rendering logic method in here.</param>
        public void DebugRender(
            bool renderChildren,
            bool onlyContainingItems,
            IVolume? collectionVolume,
            DelRenderAABB render)
        {
            Color color = Color.Red;
            if (renderChildren)
            {
                EContainment containment = collectionVolume?.Contains(_bounds) ?? EContainment.Contains;
                color = containment == EContainment.Intersects ? Color.Green : containment == EContainment.Contains ? Color.White : Color.Red;
                if (containment != EContainment.Disjoint)
                    foreach (OctreeNode<T>? n in _subNodes)
                        n?.DebugRender(true, onlyContainingItems, collectionVolume, render);
            }
            if (!onlyContainingItems || _items.Count != 0)
                DebugRender(color, render);
        }
        public void DebugRender(Color color, DelRenderAABB render)
            => render(_bounds.Extents, _bounds.Center, color);
        #endregion

        #region Visible collection 
        public void CollectVisible(IVolume cullingVolume, bool containsOnly, Action<T> action)
        {
            switch (cullingVolume.Contains(_bounds))
            {
                case EContainment.Contains:
                    //If the culling volume contains this bounds, collect all items in this node and all sub nodes. No need to check individual items.
                    CollectAll(action);
                    break;
                case EContainment.Intersects:
                    //If the culling volume intersects this bounds, each item needs to be checked individually.
                    CollectIntersecting(cullingVolume, containsOnly, action);
                    break;
            }
        }

        private void CollectIntersecting(IVolume cullingVolume, bool containsOnly, Action<T> action)
        {
            //Collect items in this bounds by comparing to culling volume directly
            //Can't do this in parallel because SortedSet is not thread-safe.
            //Would have to program a custom thread-safe sorted tree class with logn add time.
            for (int i = 0; i < _items.Count; ++i)
            {
                T item = _items[i];
                if (item.ShouldRender && item.Intersects(cullingVolume, containsOnly))
                    action(item);
            }

            //Collect items from child bounds
            for (int i = 0; i < OctreeBase.MaxChildNodeCount; ++i)
                _subNodes[i]?.CollectVisible(cullingVolume, containsOnly, action);
        }

        public void CollectAll(Action<T> action)
        {
            for (int i = 0; i < _items.Count; ++i)
            {
                T item = _items[i];
                if (item.ShouldRender)
                    action(item);
            }

            for (int i = 0; i < OctreeBase.MaxChildNodeCount; ++i)
                _subNodes[i]?.CollectAll(action);
        }
        public void CollectAll(List<T> renderables)
        {
            for (int i = 0; i < _items.Count; ++i)
                renderables.Add(_items[i]);

            for (int i = 0; i < OctreeBase.MaxChildNodeCount; ++i)
                _subNodes[i]?.CollectAll(renderables);
        }
        #endregion

        #region Add/Remove
        /// <summary>
        /// Returns true if this node no longer contains anything.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public bool RemoveHereOrSmaller(T item)
        {
            if (_items.Contains(item))
                RemoveHere(item);
            else
                for (int i = 0; i < OctreeBase.MaxChildNodeCount; ++i)
                {
                    OctreeNode<T>? node = _subNodes[i];
                    if (node is null)
                        continue;
                    
                    if (node.RemoveHereOrSmaller(item))
                        _subNodes[i] = null;
                    else
                        return false;
                }

            return _items.Count == 0 && HasNoSubNodesExcept(-1);
        }
        /// <summary>
        /// Adds a list of items to this node. May subdivide.
        /// </summary>
        /// <param name="items">The items to add.</param>
        /// <param name="forceAddToThisNode">If true, will add each item regardless of if its culling volume fits within this node's bounds.</param>
        /// <returns>True if ANY node was added.</returns>
        internal void AddHereOrSmaller(List<T> items)
        {
            foreach (T item in items)
                AddHereOrSmaller(item, -1);
        }
        /// <summary>
        /// Adds an item to this node. May subdivide.
        /// </summary>
        /// <param name="items">The item to add.</param>
        /// <param name="forceAddToThisNode">If true, will add the item regardless of if its culling volume fits within the node's bounds.</param>
        /// <returns>True if the node was added.</returns>
        internal bool AddHereOrSmaller(T item, int ignoreSubNode = -1)
        {
            if (item is null)
                return false;

            if (item.CullingVolume != null && ((IVolume)_bounds).Contains(item.CullingVolume) == EContainment.Contains)
                for (int i = 0; i < OctreeBase.MaxChildNodeCount; ++i)
                {
                    if (i == ignoreSubNode)
                        continue;

                    AABB? subDiv = GetSubdivision(i);
                    if (subDiv is not null && ((IVolume)subDiv.Value).Contains(item.CullingVolume) == EContainment.Contains)
                    {
                        CreateSubNode(subDiv.Value, i)?.AddHereOrSmaller(item);
                        return true;
                    }
                }

            AddHere(item);
            return true;
        }
        #endregion

        internal void AddHere(T item)
        {
            _items.Add(item);
            item.OctreeNode = this;
        }
        internal void RemoveHere(T item)
        {
            _items.Remove(item);
            item.OctreeNode = null;
        }

        #region Convenience methods
        public T? FindClosest(Vector3 point, ref float closestDistance)
        {
            if (!_bounds.Contains(point))
                return null;

            //IsLoopingSubNodes = true;
            foreach (OctreeNode<T>? n in _subNodes)
            {
                T? t = n?.FindClosest(point, ref closestDistance);
                if (t is not null)
                    return t;
            }
            //IsLoopingSubNodes = false;

            if (_items.Count == 0)
                return null;

            T? closest = null;

            //IsLoopingItems = true;
            foreach (T item in _items)
            {
                var cullingVolume = item?.CullingVolume;
                if (cullingVolume is null)
                    continue;

                float dist = Vector3.Distance(cullingVolume.ClosestPoint(point, false), point);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = item;
                }
            }
            //IsLoopingItems = false;

            return closest;
        }
        public void FindAll(IShape shape, List<T> list, EContainment containment)
        {
            EContainment c = ((IVolume)Bounds).Contains(shape);
            if (c == EContainment.Intersects)
            {
                //Compare each item separately
                //IsLoopingItems = true;
                foreach (T item in _items)
                    if (item.CullingVolume != null)
                    {
                        c = shape.Contains(item.CullingVolume);
                        if (c == containment)
                            list.Add(item);
                    }
                //IsLoopingItems = false;
            }
            else if (c == containment)
            {
                //All items already have this containment
                //IsLoopingItems = true;
                list.AddRange(_items);
                //IsLoopingItems = false;
            }
            else //Not what we want
                return;

            //IsLoopingSubNodes = true;
            foreach (OctreeNode<T>? n in _subNodes)
                n?.FindAll(shape, list, containment);
            //IsLoopingSubNodes = false;
        }
        /// <summary>
        /// Simply collects all the items contained in this node and all of its sub nodes.
        /// </summary>
        /// <returns></returns>
        public List<T> CollectChildren()
        {
            List<T> list = new(_items);
            foreach (OctreeNode<T>? node in _subNodes)
                if (node is not null)
                    list.AddRange(node.CollectChildren());
            return list;
        }
        #endregion

        #region Private Helper Methods
        private void ClearSubNode(int index)
        {
            if (index >= 0)
                _subNodes[index] = null;
        }
        private bool HasNoSubNodesExcept(int index)
        {
            for (int i = 0; i < OctreeBase.MaxChildNodeCount; ++i)
                if (i != index && _subNodes[i] != null)
                    return false;
            return true;
        }
        private OctreeNode<T> CreateSubNode(AABB bounds, int index)
        {
            try
            {
                //IsLoopingSubNodes = true;
                return _subNodes[index] ??= new OctreeNode<T>(bounds, index, _subDivLevel + 1, this, Owner);
            }
            finally
            {
                //IsLoopingSubNodes = false;
            }
        }

        public T? FindFirst(Predicate<T> itemTester, Predicate<AABB> octreeNodeTester)
        {
            if (!octreeNodeTester(_bounds))
                return null;

            for (int i = 0; i < _items.Count; ++i)
                if (_items[i] is T item && itemTester(item))
                    return item;
            
            for (int i = 0; i < _subNodes.Length; ++i)
            {
                if (_subNodes[i] is null)
                    continue;

                T? item = _subNodes[i]?.FindFirst(itemTester, octreeNodeTester);
                if (item != null)
                    return item;
            }

            return null;
        }

        public void FindAll(Predicate<T> itemTester, Predicate<AABB> octreeNodeTester, List<T> list)
        {
            if (!octreeNodeTester(_bounds))
                return;

            for (int i = 0; i < _items.Count; ++i)
                if (_items[i] is T item && itemTester(item))
                    list.Add(item);

            for (int i = 0; i < _subNodes.Length; ++i)
                _subNodes[i]?.FindAll(itemTester, octreeNodeTester, list);
        }

        #endregion
    }
}
