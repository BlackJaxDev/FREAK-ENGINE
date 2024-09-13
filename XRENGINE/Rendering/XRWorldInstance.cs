using Extensions;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Data.Trees;
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

        public XREventGroup<GameMode> CurrentGameModeChanged;
        public XREvent<XRWorldInstance> PreBeginPlay;
        public XREvent<XRWorldInstance> PostBeginPlay;
        public XREvent<XRWorldInstance> PreEndPlay;
        public XREvent<XRWorldInstance> PostEndPlay;

        protected VisualScene _visualScene;
        public VisualScene VisualScene => _visualScene;

        protected PhysicsScene _physicsScene;
        public PhysicsScene PhysicsScene => _physicsScene;

        protected RootNodeCollection _rootNodes = [];
        public RootNodeCollection RootNodes => _rootNodes;

        /// <summary>
        /// Sequences are used to track the order of operations for debugging purposes.
        /// </summary>
        private Dictionary<int, List<string>> Sequences { get; } = [];

        public GameMode? GameMode { get; internal set; }

        public XRWorldInstance() : this(Engine.Rendering.NewVisualScene(), Engine.Rendering.NewPhysicsScene()) { }
        public XRWorldInstance(VisualScene visualScene, PhysicsScene physicsScene)
        {
            _visualScene = visualScene;
            _physicsScene = physicsScene;

            TickLists = [];
            TickLists.Add(ETickGroup.Normal, []);
            TickLists.Add(ETickGroup.Late, []);
            TickLists.Add(ETickGroup.PrePhysics, []);
            TickLists.Add(ETickGroup.PostPhysics, []);
        }

        public XRWorldInstance(XRWorld world) : this()
            => TargetWorld = world;
        public XRWorldInstance(XRWorld world, VisualScene visualScene, PhysicsScene physicsScene) : this(visualScene, physicsScene)
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
            Time.Timer.UpdateFrame += Update;
            Time.Timer.FixedUpdate += FixedUpdate;
            PostBeginPlay.Invoke(this);
        }

        protected virtual void BeginPlayInternal()
        {
            VisualScene.RenderablesTree.Swap();
            foreach (SceneNode node in RootNodes)
                if (node.IsActiveSelf)
                    node.Start();
            IsPlaying = true;
            Task.Run(RecalcTransforms);
        }

        public void EndPlay()
        {
            PreEndPlay.Invoke(this);
            Time.Timer.UpdateFrame -= Update;
            PhysicsScene.Destroy();
            EndPlayInternal();
            PostEndPlay.Invoke(this);
        }
        protected virtual void EndPlayInternal()
        {
            VisualScene.RenderablesTree.Swap();
            foreach (SceneNode node in RootNodes)
                if (node.IsActiveSelf)
                    node.Stop();
            IsPlaying = false;
        }
        
        public async Task RecalcTransforms()
        {
            static Task TaskRunner(TransformBase t)
                => Task.Run(t.TryParallelDepthRecalculate);

            while (IsPlaying)
            {
                foreach (var key in DirtyTransforms.Keys)
                {
                    var (set, locker) = DirtyTransforms[key];
                    //locker.EnterReadLock();
                    await Task.WhenAll(set.Select(TaskRunner));
                    //locker.ExitReadLock();
                }
            }
        }
        /// <summary>
        /// Dictionary of dirty transforms that need to be recalculated.
        /// Key is depth of the transform in the hierarchy.
        /// Value is a queue of transforms that need to be recalculated for that depth.
        /// </summary>
        public SortedDictionary<int, (ConcurrentHashSet<TransformBase> set, ReaderWriterLockSlim locker)> DirtyTransforms { get; private set; } = [];
        //Lock the depth dictionary to prevent concurrent modification.
        //This shouldn't be a problem since we're only initializing sets for deeper and deeper transforms.
        private readonly ReaderWriterLockSlim _dictLock = new();
        public void AddDirtyTransform(TransformBase transform)
        {
            _dictLock.EnterReadLock();
            bool got = DirtyTransforms.TryGetValue(transform.Depth, out (ConcurrentHashSet<TransformBase> set, ReaderWriterLockSlim locker) pair);
            _dictLock.ExitReadLock();
            if (!got)
            {
                _dictLock.EnterWriteLock();
                DirtyTransforms.Add(transform.Depth, pair = ([], new ReaderWriterLockSlim()));
                _dictLock.ExitWriteLock();
            }
            //TODO: this locker is probably not necessary because the set is already thread-safe.
            //pair.locker.EnterWriteLock();
            pair.set.Add(transform);
            //pair.locker.ExitWriteLock();
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

                    if (VisualScene.RenderablesTree is I3DRenderTree tree)
                        tree.Remake(_targetWorld.Settings.Bounds);
                }
            }
        }

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

        void ScenePropertyChanged(object? sender, PropertyChangedEventArgs args)
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
            SortedDictionary<int, TickList> dic = TickLists[group];
            if (!dic.TryGetValue(order, out TickList? list))
                dic[order] = list = new TickList(Engine.Rendering.Settings.TickGroupedItemsInParallel);
            return list;
        }

        /// <summary>
        /// Ticks all sorted lists of methods registered to this group.
        /// </summary>
        public void TickGroup(ETickGroup group)
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

        public void CaptureIBL()
        {

        }
    }
}
