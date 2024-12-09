using Extensions;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using XREngine.Audio;
using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Trees;
using XREngine.Rendering.Info;
using XREngine.Rendering.Physics.Physx;
using XREngine.Scene;
using XREngine.Scene.Transforms;
using static XREngine.Engine;

namespace XREngine.Rendering
{
    /// <summary>
    /// This class handles all information pertaining to the rendering of a world.
    /// This object is assigned to a window and the window's renderer is responsible for applying the world's render data to the rendering API for that window.
    /// </summary>
    public partial class XRWorldInstance : XRObjectBase
    {
        public static Dictionary<XRWorld, XRWorldInstance> WorldInstances { get; } = [];

        public EventList<ListenerContext> Listeners { get; private set; } = [];
        
        public XREventGroup<GameMode> CurrentGameModeChanged;
        public XREvent<XRWorldInstance> PreBeginPlay;
        public XREvent<XRWorldInstance> PostBeginPlay;
        public XREvent<XRWorldInstance> PreEndPlay;
        public XREvent<XRWorldInstance> PostEndPlay;

        protected VisualScene3D _visualScene;
        public VisualScene3D VisualScene => _visualScene;

        protected AbstractPhysicsScene _physicsScene;
        public AbstractPhysicsScene PhysicsScene => _physicsScene;

        protected RootNodeCollection _rootNodes = [];
        public RootNodeCollection RootNodes => _rootNodes;
        public Lights3DCollection Lights { get; }

        /// <summary>
        /// Sequences are used to track the order of operations for debugging purposes.
        /// </summary>
        private Dictionary<int, List<string>> Sequences { get; } = [];

        public GameMode? GameMode { get; internal set; }

        public XRWorldInstance() : this(Engine.Rendering.NewVisualScene(), Engine.Rendering.NewPhysicsScene()) { }
        public XRWorldInstance(VisualScene3D visualScene, AbstractPhysicsScene physicsScene)
        {
            _visualScene = visualScene;
            _physicsScene = physicsScene;
            Lights = new Lights3DCollection(this);

            TickLists = [];
            TickLists.Add(ETickGroup.Normal, []);
            TickLists.Add(ETickGroup.Late, []);
            TickLists.Add(ETickGroup.PrePhysics, []);
            TickLists.Add(ETickGroup.PostPhysics, []);
        }

        public XRWorldInstance(XRWorld world) : this()
            => TargetWorld = world;
        public XRWorldInstance(XRWorld world, VisualScene3D visualScene, AbstractPhysicsScene physicsScene) : this(visualScene, physicsScene)
            => TargetWorld = world;

        public void FixedUpdate()
        {
            TickGroup(ETickGroup.PrePhysics);
            PhysicsScene.StepSimulation();
            TickGroup(ETickGroup.PostPhysics);
        }

        public bool IsPlaying { get; private set; }

        public void BeginPlay()
        {
            PreBeginPlay.Invoke(this);
            PhysicsScene.Initialize();
            BeginPlayInternal();
            LinkTimeCallbacks();
            PostBeginPlay.Invoke(this);
        }

        protected virtual void BeginPlayInternal()
        {
            VisualScene.GenericRenderTree.Swap();
            foreach (SceneNode node in RootNodes)
                if (node.IsActiveSelf)
                    node.OnSceneNodeActivated();
            IsPlaying = true;
        }

        public void EndPlay()
        {
            PreEndPlay.Invoke(this);
            UnlinkTimeCallbacks();
            PhysicsScene.Destroy();
            EndPlayInternal();
            PostEndPlay.Invoke(this);
        }
        protected virtual void EndPlayInternal()
        {
            VisualScene.GenericRenderTree.Swap();
            foreach (SceneNode node in RootNodes)
                if (node.IsActiveSelf)
                    node.OnSceneNodeDeactivated();
            IsPlaying = false;
        }

        private void LinkTimeCallbacks()
        {
            Time.Timer.UpdateFrame += Update;
            Time.Timer.FixedUpdate += FixedUpdate;
            Time.Timer.SwapBuffers += GlobalSwapBuffers;
            Time.Timer.CollectVisible += GlobalCollectVisible;
        }

        private void UnlinkTimeCallbacks()
        {
            Time.Timer.UpdateFrame -= Update;
            Time.Timer.FixedUpdate -= FixedUpdate;
            Time.Timer.SwapBuffers -= GlobalSwapBuffers;
            Time.Timer.CollectVisible -= GlobalCollectVisible;
        }

        public void GlobalCollectVisible()
        {
            //using var d = Profiler.Start();
            ProcessTransformQueue();
            VisualScene.GlobalCollectVisible();
            PhysicsScene.DebugRenderCollect();
            Lights.CollectVisibleItems();
        }

        private void GlobalSwapBuffers()
        {
            SwapTransformQueues();
            VisualScene.GlobalSwapBuffers();
            PhysicsScene.SwapDebugBuffers();
            Lights.SwapBuffers();
        }

        /// <summary>
        /// Called by windows on the render thread, right before rendering all viewports.
        /// </summary>
        public void GlobalPreRender()
        {
            //using var d = Profiler.Start();
            VisualScene.GlobalPreRender();
            Lights.RenderShadowMaps(false);
        }

        /// <summary>
        /// Called by windows on the render thread, right after rendering all viewports.
        /// </summary>
        public void GlobalPostRender()
        {
            //using var d = Profiler.Start();
            VisualScene.GlobalPostRender();
            Lights.CaptureLightProbes();
        }

        private void SwapTransformQueues()
        {
            (_transformBucketsByDepthRendering, _transformBucketsByDepthUpdating) = (_transformBucketsByDepthUpdating, _transformBucketsByDepthRendering);

            foreach (var bag in _transformBucketsByDepthUpdating.Values)
                bag.Clear();
        }

        private void ProcessTransformQueue()
        {
            //using var d = Profiler.Start();

            //This method updates transforms in breadth-first order of appearance in the scene.
            //Right now, it's operating in sequential order and could be subject to never finishing if transforms are added faster than they are processed.
            //Ideally, we'll want to add transforms to a bucket, then here we can take a snapshot, sort by depth and then process each depth in parallel.
            //Also, prioritize transforms that are visible according to bounding boxes of meshes that use them.

            List<int> depthKeys = [.. _transformBucketsByDepthRendering.Keys]; //Snapshot of current depths
            for (int i = 0; i < depthKeys.Count; i++)
            {
                int depth = depthKeys[i];
                if (!_transformBucketsByDepthRendering.TryGetValue(depth, out var bag))
                    continue;

                //This is good in theory for processing large amounts of nodes but the overhead of parallel initialization is too high for it to be worth the cost.
                //Parallel.ForEach(bag, transform =>
                //{
                //    if (!transform.ParallelDepthRecalculate())
                //        return;

                //    lock (depthKeys)
                //        depthKeys.Add(transform.Depth + 1);
                //});

                foreach (var transform in bag)
                    if (transform.ParallelDepthRecalculate())
                    {
                        int depthPlusOne = transform.Depth + 1;
                        if (!depthKeys.Contains(depthPlusOne))
                            depthKeys.Add(depthPlusOne);
                    }
            }
        }

        private ConcurrentDictionary<int, ConcurrentBag<TransformBase>> _transformBucketsByDepthUpdating = new();
        private ConcurrentDictionary<int, ConcurrentBag<TransformBase>> _transformBucketsByDepthRendering = new();
        
        public void AddDirtyTransform(TransformBase transform, out bool wasDepthAdded, bool directToRender)
        {
            bool added = false;
            ConcurrentDictionary<int, ConcurrentBag<TransformBase>> dict = directToRender ? _transformBucketsByDepthRendering : _transformBucketsByDepthUpdating;
            var bag = dict.GetOrAdd(transform.Depth, x =>
            {
                added = true;
                return [];
            });
            if (!bag.Contains(transform))
                bag.Add(transform);
            wasDepthAdded = added;
        }

        private XRWorld? _targetWorld;
        /// <summary>
        /// The world that this instance is rendering.
        /// </summary>
        public XRWorld? TargetWorld
        {
            get => _targetWorld;
            set
            {
                if (_targetWorld != null)
                    foreach (var scene in _targetWorld.Scenes)
                        UnloadScene(scene);

                _targetWorld = value;

                if (_targetWorld != null)
                {
                    foreach (var scene in _targetWorld.Scenes)
                        LoadScene(scene);

                    if (VisualScene.GenericRenderTree is I3DRenderTree tree)
                        tree.Remake(_targetWorld.Settings.Bounds);
                }
            }
        }

        public EventList<CameraComponent> FramebufferCameras { get; } = [];

        public void LoadScene(XRScene scene)
        {
            if (scene.IsVisible)
                LoadVisibleScene(scene);

            scene.PropertyChanged += ScenePropertyChanged;
        }

        public void UnloadScene(XRScene scene)
        {
            scene.PropertyChanged -= ScenePropertyChanged;

            if (scene.IsVisible)
                UnloadVisibleScene(scene);
        }

        void ScenePropertyChanged(object? sender, IXRPropertyChangedEventArgs args)
        {
            if (sender is not XRScene scene)
                return;

            switch (args.PropertyName)
            {
                case nameof(XRScene.IsVisible):
                    if (scene.IsVisible)
                        LoadVisibleScene(scene);
                    else
                        UnloadVisibleScene(scene);
                    break;
            }
        }

        private void LoadVisibleScene(XRScene scene)
        {
            foreach (var node in scene.RootNodes)
            {
                node.World = this;
                RootNodes.Add(node);
            }
        }

        private void UnloadVisibleScene(XRScene scene)
        {
            foreach (var node in scene.RootNodes)
            {
                RootNodes.Remove(node);
                node.World = null;
            }
        }

        /// <summary>
        /// Physics group > order (arbitrarily set by user> > list of objects to tick
        /// </summary>
        private readonly Dictionary<ETickGroup, SortedDictionary<int, TickList>> TickLists;

        /// <summary>
        /// Registers a method to execute in a specific order every update tick.
        /// </summary>
        /// <param name="group">The first grouping of when to tick: before, after, or during the physics tick update.</param>
        /// <param name="order">The order to execute the function within its group.</param>
        /// <param name="function">The function to execute per update tick.</param>
        /// <param name="pausedBehavior">If the function should even execute at all, depending on the pause state.</param>
        public void RegisterTick(ETickGroup group, int order, TickList.DelTick function)
        {
            if (function is null)
                return;

            GetTickList(group, order)?.Add(function);
        }

        /// <summary>
        /// Stops running a tick method that was previously registered with the same parameters.
        /// </summary>
        public void UnregisterTick(ETickGroup group, int order, TickList.DelTick function)
        {
            if (function is null)
                return;

            GetTickList(group, order)?.Remove(function);
        }

        /// <summary>
        /// Gets a list of items to tick (in no particular order) that were registered with the following parameters.
        /// </summary>
        private TickList GetTickList(ETickGroup group, int order)
        {
            lock (_lock)
            {
                SortedDictionary<int, TickList> dic = TickLists[group];
                if (!dic.TryGetValue(order, out TickList? list))
                    dic.Add(order, list = new TickList(Engine.Rendering.Settings.TickGroupedItemsInParallel));
                return list;
            }
        }

        /// <summary>
        /// Ticks all sorted lists of methods registered to this group.
        /// </summary>
        public void TickGroup(ETickGroup group)
        {
            lock (_lock)
            {
                var tickListDic = TickLists[group];
                List<int> toRemove = [];
                foreach (var kv in tickListDic)
                {
                    kv.Value.Tick();
                    if (kv.Value.Count == 0)
                        toRemove.Add(kv.Key);
                }
                foreach (int key in toRemove)
                    tickListDic.Remove(key);
            }
        }

        private readonly object _lock = new();

        /// <summary>
        /// Ticks the before, during, and after physics groups. Also steps the physics simulation during the during physics tick group.
        /// Does not tick physics if paused.
        /// </summary>
        private void Update()
        {
#if DEBUG
            ClearMarkers();
#endif
            TickGroup(ETickGroup.Normal);
            TickGroup(ETickGroup.Late);
#if DEBUG
            PrintMarkers();
#endif
        }

        public void SequenceMarker(int id, [CallerMemberName] string name = "")
        {
            if (Sequences.TryGetValue(id, out List<string>? value))
                value.Add(name);
            else
                Sequences[id] = [name];
        }
        private void PrintMarkers()
        {
            foreach (var kv in Sequences)
                Trace.WriteLine($"Sequence {kv.Key}: {kv.Value.ToStringList(",")}");
        }
        private void ClearMarkers()
        {
            Sequences.Clear();
        }

        public SortedDictionary<float, List<(ITreeItem item, object? data)>>? Raycast(
            CameraComponent cameraComponent,
            Vector2 normalizedScreenPoint,
            bool testOctree,
            bool testPhysics,
            out Vector3 hitNormalWorld,
            out Vector3 hitPositionWorld,
            out float hitDistance)
            => Raycast(
                cameraComponent.Camera.GetWorldSegment(normalizedScreenPoint),
                testOctree,
                testPhysics,
                out hitNormalWorld,
                out hitPositionWorld,
                out hitDistance);

        public SortedDictionary<float, List<(ITreeItem item, object? data)>>? Raycast(
            Segment worldSegment,
            bool testOctree,
            bool testPhysics,
            out Vector3 hitNormalWorld,
            out Vector3 hitPositionWorld,
            out float hitDistance)
        {
            var items = new SortedDictionary<float, List<(ITreeItem item, object? data)>>();
            if (testOctree)
                VisualScene.Raycast(worldSegment, out items, DirectItemTest);
            if (testPhysics)
                PhysicsScene.Raycast(worldSegment, items, out hitNormalWorld, out hitPositionWorld, out hitDistance);
            else
            {
                hitNormalWorld = Vector3.Zero;
                hitPositionWorld = Vector3.Zero;
                hitDistance = float.MaxValue;
            }
            return items;
        }

        private static (float? distance, object? data) DirectItemTest(ITreeItem item, Segment segment)
        {
            if (item is not RenderInfo renderable)
                return (null, null);
            
            switch (renderable.Owner)
            {
                //case ModelComponent model:
                //    {
                //        float? dist = model.Intersect(segment, out Triangle? tri);
                //        return (dist ?? 0.0f, tri);
                //    }
                case TransformBase transform:
                    {
                        Capsule c = transform.Capsule;
                        if (!c.IntersectsSegment(segment, out Vector3[] points))
                            break;
                        
                        //Get closest point distance
                        float min = float.MaxValue;
                        Vector3 bestPoint = Vector3.Zero;
                        for (int i = 0; i < points.Length; i++)
                        {
                            float dist = (points[i] - segment.Start).LengthSquared();
                            if (dist < min)
                            {
                                min = dist;
                                bestPoint = points[i];
                            }
                        }
                        return ((float)Math.Sqrt(min), bestPoint);
                    }
            }
            return (null, null);
        }
    }
}
