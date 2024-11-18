using System.Collections.Concurrent;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;

namespace XREngine.Data.Trees
{
    /// <summary>
    /// A 3D space partitioning tree that recursively divides aabbs into 8 smaller aabbs depending on the items they contain.
    /// </summary>
    /// <typeparam name="T">The item type to use. Must be a class deriving from I3DBoundable.</typeparam>
    public class Octree<T> : OctreeBase, I3DRenderTree<T> where T : class, IOctreeItem
    {
        internal OctreeNode<T> _head;

        public Octree(AABB bounds)
            => _head = new OctreeNode<T>(bounds, 0, 0, null, this);

        public Octree(AABB bounds, List<T> items) : this(bounds)
            => _head.AddHereOrSmaller(items);

        //public class RenderEquality : IEqualityComparer<I3DRenderable>
        //{
        //    public bool Equals(I3DRenderable x, I3DRenderable y)
        //        => x.RenderInfo.SceneID == y.RenderInfo.SceneID;
        //    public int GetHashCode(I3DRenderable obj)
        //        => obj.RenderInfo.SceneID;
        //}
        
        public void Remake()
            => Remake(_head.Bounds);

        public void Remake(AABB newBounds)
        {
            List<T> renderables = [];
            _head.CollectAll(renderables);
            _head = new OctreeNode<T>(newBounds, 0, 0, null, this);

            for (int i = 0; i < renderables.Count; i++)
            {
                T item = renderables[i];
                if (!_head.AddHereOrSmaller(item))
                    _head.AddHere(item);
            }
        }
        
        internal ConcurrentQueue<T> AddedItems { get; } = new ConcurrentQueue<T>();
        internal ConcurrentQueue<T> RemovedItems { get; } = new ConcurrentQueue<T>();
        internal ConcurrentQueue<T> MovedItems { get; } = new ConcurrentQueue<T>();

        /// <summary>
        /// Updates all moved, added and removed items in the octree.
        /// </summary>
        public void Swap()
        {
            while (MovedItems.TryDequeue(out T? item))
                item?.OctreeNode?.HandleMovedItem(item);
            while (RemovedItems.TryDequeue(out T? item))
            {
                if (item is null)
                    continue;

                _head.RemoveHereOrSmaller(item);
            }
            while (AddedItems.TryDequeue(out T? item))
            {
                if (item is null)
                    continue;

                if (!_head.AddHereOrSmaller(item))
                    _head.AddHere(item);
            }
        }

        public void Add(T value)
            => AddedItems.Enqueue(value);

        public void AddRange(IEnumerable<T> value)
        {
            foreach (T item in value)
                Add(item);
        }

        void IRenderTree.Add(ITreeItem item)
        {
            if (item is T t)
                Add(t);
        }

        void IRenderTree.Remove(ITreeItem item)
        {
            if (item is T t)
                Remove(t);
        }

        public void RemoveRange(IEnumerable<T> value)
        {
            foreach (T item in value)
                Remove(item);
        }

        void IRenderTree.AddRange(IEnumerable<ITreeItem> renderedObjects)
        {
            foreach (ITreeItem item in renderedObjects)
                if (item is T t)
                    Add(t);
        }

        void IRenderTree.RemoveRange(IEnumerable<ITreeItem> renderedObjects)
        {
            foreach (ITreeItem item in renderedObjects)
                if (item is T t)
                    Remove(t);
        }

        public void Remove(T value)
            => RemovedItems.Enqueue(value);

        //public List<T> FindAll(float radius, Vector3 point, EContainment containment)
        //    => FindAll(new Sphere(point, radius), containment);
        //public List<T> FindAll(IShape shape, EContainment containment)
        //{
        //    List<T> list = [];
        //    _head.FindAll(shape, list, containment);
        //    return list;
        //}

        public void CollectAll(Action<T> action)
            => _head.CollectAll(action);

        /// <summary>
        /// Renders the octree using debug bounding boxes.
        /// </summary>
        /// <param name="volume">The frustum to display intersections with. If null, does not show frustum intersections.</param>
        /// <param name="onlyContainingItems">Only renders subdivisions that contain one or more items.</param>
        /// <param name="lineWidth">The width of the bounding box lines.</param>
        public void DebugRender(IVolume? volume, bool onlyContainingItems, DelRenderAABB render)
            => _head.DebugRender(true, onlyContainingItems, volume, render);

        public void CollectVisible(IVolume? volume, bool onlyContainingItems, Action<T> action, OctreeNode<T>.DelIntersectionTest intersectionTest)
            => _head.CollectVisible(volume, onlyContainingItems, action, intersectionTest);
        void I3DRenderTree.CollectVisible(IVolume? volume, bool onlyContainingItems, Action<IOctreeItem> action, OctreeNode<IOctreeItem>.DelIntersectionTestGeneric intersectionTest)
            => _head.CollectVisible(volume, onlyContainingItems, action, intersectionTest);
        public void CollectVisibleNodes(IVolume? cullingVolume, bool containsOnly, Action<(OctreeNodeBase node, bool intersects)> action)
            => _head.CollectVisibleNodes(cullingVolume, containsOnly, action);

        void I3DRenderTree.CollectAll(Action<IOctreeItem> action)
        {
            void Add(T item)
                => action(item);

            CollectAll(Add);
        }

        public T? FindFirst(Predicate<T> itemTester, Predicate<AABB> octreeNodeTester)
            => _head.FindFirst(itemTester, octreeNodeTester);

        public List<T> FindAll(Predicate<T> itemTester, Predicate<AABB> octreeNodeTester)
        {
            List<T> list = [];
            _head.FindAll(itemTester, octreeNodeTester, list);
            return list;
        }

        public void Raycast(Segment segment, out SortedDictionary<float, List<(T item, object? data)>> items, Func<T, Segment, (float? distance, object? data)> directTest)
        {
            items = [];
            _head.Raycast(segment, items, directTest);
        }

        public void Raycast(Segment segment, out SortedDictionary<float, List<(ITreeItem item, object? data)>> items, Func<ITreeItem, Segment, (float? distance, object? data)> directTest)
        {
            items = [];
            _head.Raycast(segment, items, directTest);
        }

        public void DebugRender(IVolume? cullingVolume, DelRenderAABB render, bool onlyContainingItems = false)
            => _head.DebugRender(true, onlyContainingItems, cullingVolume, render);
    }
}
