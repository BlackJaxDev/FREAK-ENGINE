﻿using System.Diagnostics;
using System.Numerics;
using XREngine.Data.Geometry;

namespace XREngine.Data.Trees
{
    public class QuadtreeNode<T>(BoundingRectangleF bounds, int subDivIndex, int subDivLevel, QuadtreeNode<T>? parent, Quadtree<T> owner)
        : QuadtreeNodeBase(bounds, subDivIndex, subDivLevel) where T : class, IQuadtreeItem
    {
        protected List<T> _items = [];
        protected QuadtreeNode<T>?[] _subNodes = new QuadtreeNode<T>?[QuadtreeBase.MaxChildNodeCount];
        protected QuadtreeNode<T>? _parentNode = parent;

        //private ReaderWriterLockSlim _lock;

        public Quadtree<T> Owner { get => owner; set => owner = value; }
        public QuadtreeNode<T>? ParentNode { get => _parentNode; set => _parentNode = value; }
        public int SubDivIndex { get => _subDivIndex; set => _subDivIndex = value; }
        public int SubDivLevel { get => _subDivLevel; set => _subDivLevel = value; }
        public List<T> Items => _items;

        protected override QuadtreeNodeBase? GetNodeInternal(int index)
            => _subNodes[index];

        public BoundingRectangleF GetSubdivision(int index)
        {
            QuadtreeNode<T>? node = _subNodes[index];
            if (node is not null)
                return node.Bounds;

            Vector2 halfExtents = Extents * 0.5f;
            Vector2 min = Min;
            return index switch
            {
                0 => new BoundingRectangleF(min.X, min.Y, halfExtents.X, halfExtents.Y),
                1 => new BoundingRectangleF(min.X, min.Y + halfExtents.Y, halfExtents.X, halfExtents.Y),
                2 => new BoundingRectangleF(min.X + halfExtents.X, min.Y + halfExtents.Y, halfExtents.X, halfExtents.Y),
                3 => new BoundingRectangleF(min.X + halfExtents.X, min.Y, halfExtents.X, halfExtents.Y),
                _ => BoundingRectangleF.Empty,
            };
        }

        #region Child movement
        public void ItemMoved(IQuadtreeItem item)
        {
            if (item is T t)
                ItemMoved(t);
        }
        public void ItemMoved(T item)
        {
            //TODO: if the item is the only item within its volume, no need to subdivide more!!!
            //However, if the item is inserted into a volume with at least one other item in it, 
            //need to try subdividing for all items at that point.

            if (item?.CullingVolume != null)
                Owner.MovedItems.Enqueue(item);
        }

        public override void HandleMovedItem(IQuadtreeItem item)
        {
            if (item is not T t)
                return;

            //Still within the same volume?
            if (t.CullingVolume.ContainmentWithin(_bounds) == EContainment.Contains)
            {
                //Try subdividing
                for (int i = 0; i < QuadtreeBase.MaxChildNodeCount; ++i)
                {
                    BoundingRectangleF bounds = GetSubdivision(i);
                    if (t.CullingVolume.ContainmentWithin(bounds) == EContainment.Contains)
                    {
                        RemoveHere(t);
                        CreateSubNode(bounds, i)?.AddHereOrSmaller(t);
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

        //#region Debug
        //public void DebugRender(bool recurse, bool onlyContainingItems, BoundingRectangleF? f, float lineWidth)
        //{
        //    Color color = Color.Red;
        //    if (recurse)
        //    {
        //        EContainment containment = f?.ContainmentOf(_bounds) ?? EContainment.Contains;
        //        color = containment == EContainment.Intersects ? Color.Green : containment == EContainment.Contains ? Color.White : Color.Red;
        //        if (containment != EContainment.Disjoint)
        //            foreach (Node n in _subNodes)
        //                n?.DebugRender(true, onlyContainingItems, f, lineWidth);
        //    }
        //    if (!onlyContainingItems || _items.Count != 0)
        //    {
        //        BoundingRectangleF region;
        //        //bool anyVisible = false;
        //        for (int i = 0; i < _items.Count; ++i)
        //        {
        //            region = _items[i].RenderInfo.AxisAlignedRegion;
        //            Api.RenderQuad(region.Center + AbstractRenderer.UIPositionBias, AbstractRenderer.UIRotation, region.Extents, false, Color.Orange, lineWidth);
        //        }
        //        //if (anyVisible)
        //            DebugRender(color, lineWidth);
        //    }
        //}
        //public void DebugRender(Color color, float lineWidth)
        //    => Api.RenderQuad(_bounds.Center + AbstractRenderer.UIPositionBias, AbstractRenderer.UIRotation, _bounds.Extents, false, color, lineWidth);
        //#endregion

        #region Visible collection
        public void CollectVisible(BoundingRectangleF bounds, bool containsOnly, Action<T> action)
        {
            EContainment c = bounds.ContainmentOf(_bounds);
            if (c != EContainment.Disjoint)
            {
                if (c == EContainment.Contains)
                    CollectAll(action);
                else
                {
                    //IsLoopingItems = true;
                    for (int i = 0; i < _items.Count; ++i)
                    {
                        T r = _items[i];
                        if (containsOnly)
                        {
                            if (r.CullingVolume.ContainmentWithin(bounds) == EContainment.Contains)
                                action(r);
                        }
                        else
                        {
                            if (r.CullingVolume.ContainmentWithin(bounds) != EContainment.Disjoint)
                                action(r);
                        }
                    }
                    //IsLoopingItems = false;

                    //IsLoopingSubNodes = true;
                    for (int i = 0; i < QuadtreeBase.MaxChildNodeCount; ++i)
                        _subNodes[i]?.CollectVisible(bounds, containsOnly, action);
                    //IsLoopingSubNodes = false;
                }
            }
        }
        public void CollectAll(Action<T> action)
        {
            //IsLoopingItems = true;
            for (int i = 0; i < _items.Count; ++i)
                if (_items[i].ShouldRender)
                    action(_items[i]);
            //IsLoopingItems = false;

            //IsLoopingSubNodes = true;
            for (int i = 0; i < QuadtreeBase.MaxChildNodeCount; ++i)
                _subNodes[i]?.CollectAll(action);
            //IsLoopingSubNodes = false;
        }
        public void CollectAll(List<T> collected)
        {
            for (int i = 0; i < _items.Count; ++i)
                collected.Add(_items[i]);

            for (int i = 0; i < QuadtreeBase.MaxChildNodeCount; ++i)
                _subNodes[i]?.CollectAll(collected);
        }
        #endregion

        #region Add/Remove
        /// <summary>
        /// Returns true if this node no longer contains anything.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public bool RemoveHereOrSmaller(T item)
        {
            if (item is null)
                return false;

            if (_items.Contains(item))
                RemoveHere(item);
            else
                for (int i = 0; i < QuadtreeBase.MaxChildNodeCount; ++i)
                {
                    QuadtreeNode<T>? node = _subNodes[i];
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
                AddHereOrSmaller(item);
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

            if (!item.CullingVolume.IsEmpty())
            {
                if (item.CullingVolume.ContainmentWithin(_bounds) != EContainment.Contains)
                    return false;

                for (int i = 0; i < QuadtreeBase.MaxChildNodeCount; ++i)
                {
                    if (i == ignoreSubNode)
                        continue;

                    BoundingRectangleF bounds = GetSubdivision(i);
                    if (item.CullingVolume.ContainmentWithin(bounds) == EContainment.Contains)
                    {
                        CreateSubNode(bounds, i)?.AddHereOrSmaller(item);
                        return true;
                    }
                }
            }

            AddHere(item);
            return true;
        }
        #endregion

        internal void AddHere(T item)
        {
            if (item is null)
                return;

            _items.Add(item);
            item.QuadtreeNode = this;
        }
        internal void RemoveHere(T item)
        {
            if (item is null)
                return;

            _items.Remove(item);
            item.QuadtreeNode = null;
        }

        //#region Loop Threading Backlog

        ////Backlog for adding and removing items when other threads are currently looping
        //protected ConcurrentQueue<Tuple<bool, T>> _itemQueue = new ConcurrentQueue<Tuple<bool, T>>();
        ////Backlog for setting sub nodes when other threads are currently looping
        //protected ConcurrentQueue<Tuple<int, Node>> _subNodeQueue = new ConcurrentQueue<Tuple<int, Node>>();
        //private bool _isLoopingItems = false;
        //private bool _isLoopingSubNodes = false;

        //protected bool IsLoopingItems
        //{
        //    get => _isLoopingItems;
        //    set
        //    {
        //        _isLoopingItems = value;
        //        while (!_isLoopingItems && !_itemQueue.IsEmpty && _itemQueue.TryDequeue(out Tuple<bool, T> result))
        //        {
        //            if (result.Item1)
        //                AddHereOrSmaller(result.Item2, -1);
        //            else
        //                RemoveHereOrSmaller(result.Item2);
        //        }
        //    }
        //}
        //protected bool IsLoopingSubNodes
        //{
        //    get => _isLoopingSubNodes;
        //    set
        //    {
        //        _isLoopingSubNodes = value;
        //        while (!_isLoopingSubNodes && !_subNodeQueue.IsEmpty && _subNodeQueue.TryDequeue(out Tuple<int, Node> result))
        //            _subNodes[result.Item1] = result.Item2;
        //    }
        //}

        //private bool QueueAdd(T item)
        //{
        //    if (IsLoopingItems)
        //    {
        //        _itemQueue.Enqueue(new Tuple<bool, T>(true, item));
        //        return false;
        //    }
        //    else
        //    {
        //        if (Owner.AllItems.Add(item))
        //        {
        //            _items.Add(item);
        //            item.RenderInfo.QuadtreeNode = this;
        //        }
        //        return true;
        //    }
        //}
        //private bool RemoveHere(T item)
        //{
        //    if (IsLoopingItems)
        //    {
        //        _itemQueue.Enqueue(new Tuple<bool, T>(false, item));
        //        return false;
        //    }
        //    else
        //    {
        //        if (Owner.AllItems.Remove(item))
        //        {
        //            _items.Remove(item);
        //            item.RenderInfo.QuadtreeNode = null;
        //        }
        //        return true;
        //    }
        //}

        //#endregion

        #region Convenience methods
        public T? FindNearest(Vector2 point, ref float closestDistance)
        {
            closestDistance = 0.0f;

            if (!_bounds.Contains(point))
                return default;

            //IsLoopingSubNodes = true;
            foreach (QuadtreeNode<T>? n in _subNodes)
            {
                T? t = n?.FindNearest(point, ref closestDistance);
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
                float dist = Vector2.Distance(item.ClosestPoint(point), point);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = item;
                }
            }
            //IsLoopingItems = false;

            return closest;
        }
        public void FindDeepest(Vector2 point, ref T? currentDeepest)
        {
            if (!_bounds.Contains(point))
                return;

            //IsLoopingSubNodes = true;
            try
            {
                foreach (QuadtreeNode<T>? n in _subNodes)
                    n?.FindDeepest(point, ref currentDeepest);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            //IsLoopingSubNodes = false;

            if (_items.Count == 0)
                return;

            //IsLoopingItems = true;
            try
            {
                foreach (T? item in _items)
                    if (item is not null && item.Contains(point) &&
                        item.DeeperThan(currentDeepest))
                        currentDeepest = item;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            //IsLoopingItems = false;
        }
        public void FindAllIntersecting(Vector2 point, List<T> intersecting, Predicate<T>? predicate = null)
        {
            if (!_bounds.Contains(point))
                return;

            //IsLoopingSubNodes = true;
            try
            {
                foreach (QuadtreeNode<T>? n in _subNodes)
                    n?.FindAllIntersecting(point, intersecting);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            //IsLoopingSubNodes = false;

            if (_items.Count == 0)
                return;

            //IsLoopingItems = true;
            try
            {
                foreach (T? item in _items)
                    if (item.Contains(point) && (predicate?.Invoke(item) ?? true))
                        intersecting.Add(item);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            //IsLoopingItems = false;
        }
        public void FindAllIntersecting(Vector2 point, SortedSet<T> intersecting, Predicate<T>? predicate = null)
        {
            if (!_bounds.Contains(point))
                return;

            //IsLoopingSubNodes = true;
            try
            {
                foreach (QuadtreeNode<T>? n in _subNodes)
                    n?.FindAllIntersecting(point, intersecting);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            //IsLoopingSubNodes = false;

            if (_items.Count == 0)
                return;

            //IsLoopingItems = true;
            try
            {
                foreach (T item in _items)
                    if (item.Contains(point) && (predicate?.Invoke(item) ?? true))
                        intersecting.Add(item);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            //IsLoopingItems = false;
        }
        //public void FindAll(Shape shape, List<T> list, EContainment containment)
        //{
        //    EContainment c = shape.ContainedWithin(Bounds);
        //    if (c == EContainment.Intersects)
        //    {
        //        //Compare each item separately
        //        IsLoopingItems = true;
        //        foreach (T item in _items)
        //            if (item.CullingVolume != null)
        //            {
        //                c = shape.Contains(item.CullingVolume);
        //                if (c == containment)
        //                    list.Add(item);
        //            }
        //        IsLoopingItems = false;
        //    }
        //    else if (c == containment)
        //    {
        //        //All items already have this containment
        //        IsLoopingItems = true;
        //        list.AddRange(_items);
        //        IsLoopingItems = false;
        //    }
        //    else //Not what we want
        //        return;

        //    IsLoopingSubNodes = true;
        //    foreach (Node n in _subNodes)
        //        n?.FindAll(shape, list, containment);
        //    IsLoopingSubNodes = false;
        //}
        /// <summary>
        /// Simply collects all the items contained in this node and all of its sub nodes.
        /// </summary>
        /// <returns></returns>
        public List<T> CollectChildren()
        {
            List<T> list = new(_items);
            foreach (QuadtreeNode<T>? node in _subNodes)
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
            for (int i = 0; i < QuadtreeBase.MaxChildNodeCount; ++i)
                if (i != index && _subNodes[i] != null)
                    return false;
            return true;
        }
        private QuadtreeNode<T> CreateSubNode(BoundingRectangleF bounds, int index)
        {
            try
            {
                //IsLoopingSubNodes = true;
                return _subNodes[index] ??= new QuadtreeNode<T>(bounds, index, _subDivLevel + 1, this, Owner);
            }
            finally
            {
                //IsLoopingSubNodes = false;
            }
        }
        #endregion
    }
}