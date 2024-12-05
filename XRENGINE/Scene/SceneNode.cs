using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using XREngine.Components;
using XREngine.Core;
using XREngine.Core.Attributes;
using XREngine.Data.Core;
using XREngine.Rendering;
using XREngine.Scene.Transforms;

namespace XREngine.Scene
{
    [Serializable]
    public sealed class SceneNode : XRWorldObjectBase, IEventListReadOnly<XRComponent>
    {
        //private static SceneNode? _dummy;
        //internal static SceneNode Dummy => _dummy ??= new SceneNode() { IsDummy = true };
        //internal bool IsDummy { get; private set; } = false;

        public const string DefaultName = "New Scene Node";

        public SceneNode() : this(DefaultName) { }
        public SceneNode(TransformBase transform) : this(DefaultName, transform) { }
        public SceneNode(XRScene scene) : this(scene, DefaultName) { }
        public SceneNode(SceneNode parent) : this(parent, DefaultName) { }
        public SceneNode(SceneNode parent, string name, TransformBase? transform = null)
        {
            if (transform != null)
                SetTransform(transform, false);

            Transform.Parent = parent?.Transform;
            Name = name;
        }

        public SceneNode(SceneNode parent, TransformBase? transform = null)
            : this(parent, DefaultName, transform) { }
        public SceneNode(string name, TransformBase? transform = null)
        {
            if (transform != null)
                SetTransform(transform, false);

            Transform.Parent = null;
            Name = name;
            ComponentsInternal.PostAnythingAdded += ComponentAdded;
            ComponentsInternal.PostAnythingRemoved += ComponentRemoved;
        }
        public SceneNode(XRScene scene, string name, TransformBase? transform = null)
        {
            if (transform != null)
                SetTransform(transform, false);

            scene.RootNodes.Add(this);
            Name = name;
            ComponentsInternal.PostAnythingAdded += ComponentAdded;
            ComponentsInternal.PostAnythingRemoved += ComponentRemoved;
        }
        public SceneNode(XRWorldInstance? world, string? name = null, TransformBase? transform = null)
        {
            if (transform != null)
                SetTransform(transform, false);

            World = world;
            Name = name ?? DefaultName;
            ComponentsInternal.PostAnythingAdded += ComponentAdded;
            ComponentsInternal.PostAnythingRemoved += ComponentRemoved;
        }

        private void ComponentRemoved(XRComponent item)
            => item.RemovedFromSceneNode(this);
        private void ComponentAdded(XRComponent item)
            => item.AddedToSceneNode(this);

        private readonly EventList<XRComponent> _components = [];
        private EventList<XRComponent> ComponentsInternal => _components;

        private bool _isActiveSelf = true;
        /// <summary>
        /// Determines if the scene node is active in the scene hierarchy.
        /// When set to false, Stop() will be called and all child nodes and components will be deactivated.
        /// When set to true, Start() will be called and all child nodes and components will be activated.
        /// </summary>
        public bool IsActiveSelf
        {
            get => _isActiveSelf;
            set => SetField(ref _isActiveSelf, value);
        }

        /// <summary>
        /// If the scene node is active in the scene hierarchy. Dependent on the IsActiveSelf property of this scene node and all of its ancestors. 
        /// If any ancestor is inactive, this will return false. 
        /// When setting to true, if the scene node has a parent, it will set the parent's IsActiveInHierarchy property to true, recursively. 
        /// When setting to false, it will set the IsActiveSelf property to false.
        /// </summary>
        public bool IsActiveInHierarchy
        {
            get
            {
                if (!IsActiveSelf)
                    return false;

                var parent = Parent;
                return parent is null || parent.IsActiveInHierarchy;
            }
            set
            {
                if (!value)
                    IsActiveSelf = false;
                else
                {
                    IsActiveSelf = true;
                    var parent = Parent;
                    if (parent != null)
                        parent.IsActiveInHierarchy = true;
                }
            }
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(IsActiveSelf):
                    if (IsActiveSelf)
                        OnSceneNodeActivated();
                    else
                        OnSceneNodeDeactivated();
                    break;
                case nameof(World):
                    SetWorldToChildNodes(World);
                    break;
                case nameof(Transform):
                    Transform.Name = Name;
                    break;
                case nameof(Name):
                    Transform.Name = Name;
                    break;
            }
        }

        private void UnlinkTransform()
        {
            if (IsActiveInHierarchy)
                Transform.OnSceneNodeDeactivated();
            Transform.PropertyChanged -= TransformPropertyChanged;
            Transform.PropertyChanging -= TransformPropertyChanging;
            Transform.SceneNode = null;
            Transform.World = null;
        }

        private void LinkTransform()
        {
            Transform.SceneNode = this;
            Transform.World = World;
            Transform.PropertyChanged += TransformPropertyChanged;
            Transform.PropertyChanging += TransformPropertyChanging;
            if (IsActiveInHierarchy)
                Transform.OnSceneNodeActivated();
        }

        private void TransformPropertyChanging(object? sender, PropertyChangingEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TransformBase.Parent):
                    OnParentChanging();
                    break;
            }
        }

        private void TransformPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TransformBase.Parent):
                    OnParentChanged();
                    break;
            }
        }

        private void OnParentChanging()
        {

        }

        private void OnParentChanged()
        {
            World = Parent?.World;
        }

        /// <summary>
        /// The components attached to this scene node.
        /// Use AddComponent<T>() and RemoveComponent<T>() or XRComponent.Destroy() to add and remove components.
        /// </summary>
        public IEventListReadOnly<XRComponent> Components => ComponentsInternal;

        private TransformBase? _transform = null;
        /// <summary>
        /// The transform of this scene node.
        /// Will never be null, because scene nodes all have transformations in the scene.
        /// </summary>
        public TransformBase Transform
        {
            get
            {
                if (_transform is null)
                    SetTransform<Transform>();

                return _transform!;
            }
        }

        public T? GetTransformAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(bool forceConvert = false) where T : TransformBase, new()
            => !forceConvert ? Transform as T : Transform is T value ? value : SetTransform<T>();

        public bool TryGetTransformAs<T>([MaybeNullWhen(false)] out T? transform) where T : TransformBase
        {
            transform = Transform as T;
            return transform != null;
        }

        /// <summary>
        /// Sets the transform of this scene node.
        /// If retainParent is true, the parent of the new transform will be set to the parent of the current transform.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="retainParent"></param>
        public void SetTransform(TransformBase transform, bool retainParent = true)
        {
            if (retainParent)
                transform.Parent = _transform?.Parent;
            if (_transform is not null)
                UnlinkTransform();
            SetField(ref _transform, transform);
            if (_transform is not null)
                LinkTransform();
        }

        /// <summary>
        /// Sets the transform of this scene node to a new instance of type T.
        /// If retainParent is true, the parent of the new transform will be set to the parent of the current transform.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="retainParent"></param>
        public T SetTransform<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : TransformBase, new()
        {
            T value = new();
            SetTransform(value, true);
            return value;
        }

        /// <summary>
        /// The immediate ancestor of this scene node, or null if this scene node is the root of the scene.
        /// </summary>
        public SceneNode? Parent
        {
            get => Transform.Parent?.SceneNode;
            set => Transform.Parent = value?.Transform;
        }

        // TODO: set and unset world to transform and components when enabled and disabled
        private void SetWorldToChildNodes(XRWorldInstance? value)
        {
            Transform.World = World;

            lock (Components)
            {
                foreach (var component in this)
                    component.World = value;
            }

            lock (Transform.Children)
            {
                for (int i = 0; i < Transform.Children.Count; i++)
                {
                    var child = Transform.Children[i];
                    if (child?.SceneNode is SceneNode node)
                        node.World = value;
                }
            }
        }

        /// <summary>
        /// Returns the full path of the scene node in the scene hierarchy.
        /// </summary>
        /// <param name="splitter"></param>
        /// <returns></returns>
        public string GetPath(string splitter = "/")
        {
            var path = Name ?? string.Empty;
            var parent = Parent;
            while (parent != null)
            {
                path = $"{parent.Name}{splitter}{path}";
                parent = parent.Parent;
            }
            return path;
        }

        /// <summary>
        /// Creates and adds a component of type T to the scene node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T? AddComponent<T>() where T : XRComponent
        {
            var comp = XRComponent.New<T>(this);
            comp.World = World;

            if (!VerifyComponentAttributesOnAdd(comp))
                return null;

            AddComponent(comp);
            return comp;
        }

        /// <summary>
        /// Creates and adds a component of type to the scene node.
        /// </summary>
        /// <param name="type"></param>
        public XRComponent? AddComponent(Type type)
        {
            if (XRComponent.New(this, type) is not XRComponent comp || !VerifyComponentAttributesOnAdd(comp))
                return null;

            AddComponent(comp);
            return comp;
        }

        private void AddComponent(XRComponent comp)
        {
            lock (Components)
            {
                ComponentsInternal.Add(comp);
            }

            comp.Destroying += ComponentDestroying;
            comp.Destroyed += ComponentDestroyed;

            if (IsActiveInHierarchy && World is not null)
            {
                comp.VerifyInterfacesOnStart();
                comp.OnComponentActivated();
            }
        }

        public bool TryAddComponent<T>(out T? comp) where T : XRComponent
        {
            comp = AddComponent<T>();
            return comp != null;
        }

        public bool TryAddComponent(Type type, out XRComponent? comp)
        {
            comp = AddComponent(type);
            return comp != null;
        }

        /// <summary>
        /// Reads the attributes of the component and runs the logic for them.
        /// Returns true if the component should be added, false if it should not.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="comp"></param>
        /// <returns></returns>
        private bool VerifyComponentAttributesOnAdd<T>(T comp) where T : XRComponent
        {
            var attribs = comp.GetType().GetCustomAttributes(true);
            if (attribs.Length == 0)
                return true;

            foreach (var attrib in attribs)
                if (attrib is XRComponentAttribute xrAttrib && !xrAttrib.VerifyComponentOnAdd(this, comp))
                    return false;

            return true;
        }

        /// <summary>
        /// Removes the first component of type T from the scene node and destroys it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RemoveComponent<T>() where T : XRComponent
        {
            var comp = GetComponent<T>();
            if (comp is null)
                return;

            lock (Components)
            {
                ComponentsInternal.Remove(comp);
            }
            comp.Destroying -= ComponentDestroying;
            comp.Destroyed -= ComponentDestroyed;
            comp.Destroy();
        }

        /// <summary>
        /// Removes the first component of type from the scene node and destroys it.
        /// </summary>
        /// <param name="type"></param>
        public void RemoveComponent(Type type)
        {
            var comp = GetComponent(type);
            if (comp is null)
                return;

            lock (Components)
            {
                ComponentsInternal.Remove(comp);
            }
            comp.Destroying -= ComponentDestroying;
            comp.Destroyed -= ComponentDestroyed;
            comp.Destroy();
        }

        private bool ComponentDestroying(XRObjectBase comp)
        {
            return true;
        }
        private void ComponentDestroyed(XRObjectBase comp)
        {
            if (comp is not XRComponent xrComp)
                return;

            lock (Components)
            {
                ComponentsInternal.Remove(xrComp);
            }
            xrComp.Destroying -= ComponentDestroying;
            xrComp.Destroyed -= ComponentDestroyed;
        }

        /// <summary>
        /// Returns the first component of type T attached to the scene node.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public T1? GetComponent<T1>() where T1 : XRComponent
        {
            lock (Components)
            {
                return ComponentsInternal.FirstOrDefault(x => x is T1) as T1;
            }
        }

        /// <summary>
        /// Gets or adds a component of type T to the scene node.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="wasAdded"></param>
        /// <returns></returns>
        public T1? GetOrAddComponent<T1>(out bool wasAdded) where T1 : XRComponent
        {
            var comp = GetComponent<T1>();
            if (comp is null)
            {
                comp = AddComponent<T1>();
                wasAdded = true;
            }
            else
                wasAdded = false;

            return comp;
        }

        /// <summary>
        /// Returns the last component of type T attached to the scene node.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public T1? GetLastComponent<T1>() where T1 : XRComponent
        {
            lock (Components)
            {
                return ComponentsInternal.LastOrDefault(x => x is T1) as T1;
            }
        }

        /// <summary>
        /// Returns all components of type T attached to the scene node.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public T1[] GetComponents<T1>() where T1 : XRComponent
        {
            lock (Components)
            {
                return ComponentsInternal.OfType<T1>().ToArray();
            }
        }

        /// <summary>
        /// Returns the first component of type attached to the scene node.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public XRComponent? GetComponent(Type type)
        {
            lock (Components)
            {
                return ComponentsInternal.FirstOrDefault(type.IsInstanceOfType);
            }
        }

        /// <summary>
        /// Returns the last component of type attached to the scene node.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public XRComponent? GetLastComponent(Type type)
        {
            lock (Components)
            {
                return ComponentsInternal.LastOrDefault(type.IsInstanceOfType);
            }
        }

        /// <summary>
        /// Returns all components of type attached to the scene node.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public XRComponent[] GetComponents(Type type)
        {
            lock (Components)
            {
                return ComponentsInternal.Where(type.IsInstanceOfType).ToArray();
            }
        }

        public XRComponent this[int index]
        {
            get
            {
                lock (Components)
                {
                    return ComponentsInternal.ElementAtOrDefault(index) ?? throw new IndexOutOfRangeException();
                }
            }
        }
        public XRComponent? this[Type type] => GetComponent(type);

        /// <summary>
        /// Called when the scene node is added to a world or activated.
        /// </summary>
        public void OnSceneNodeActivated()
        {
            Transform.OnSceneNodeActivated();

            foreach (XRComponent component in this)
                if (component.IsActive)
                {
                    component.VerifyInterfacesOnStart();
                    component.OnComponentActivated();
                }

            lock (Transform.Children)
            {
                foreach (var child in Transform.Children)
                {
                    var node = child?.SceneNode;
                    if (node is null)
                        continue;

                    if (node.IsActiveSelf)
                        node.OnSceneNodeActivated();
                }
            }
        }
        /// <summary>
        /// Called when the scene node is removed from a world or deactivated.
        /// </summary>
        public void OnSceneNodeDeactivated()
        {
            Transform.OnSceneNodeDeactivated();

            foreach (XRComponent component in this)
                if (component.IsActive)
                {
                    component.OnComponentDeactivated();
                    component.VerifyInterfacesOnStop();
                    if (component.UnregisterTicksOnStop)
                        ClearTicks();
                }

            lock (Transform.Children)
            {
                foreach (var child in Transform.Children)
                {
                    var node = child?.SceneNode;
                    if (node is null)
                        continue;

                    if (node.IsActiveSelf)
                        node.OnSceneNodeDeactivated();
                }
            }
        }

        public IEnumerator<XRComponent> GetEnumerator()
            => ComponentsInternal.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)ComponentsInternal).GetEnumerator();

        public void IterateComponents(Action<XRComponent> componentAction, bool iterateChildHierarchy)
        {
            lock (Components)
            {
                foreach (var component in ComponentsInternal)
                    componentAction(component);
            }

            if (!iterateChildHierarchy)
                return;

            lock (Transform.Children)
            {
                foreach (var child in Transform.Children)
                    child?.SceneNode?.IterateComponents(componentAction, true);
            }
        }

        public void IterateComponents<T>(Action<T> componentAction, bool iterateChildHierarchy) where T : XRComponent
            => IterateComponents(c =>
            {
                if (c is T t)
                    componentAction(t);
            }, iterateChildHierarchy);

        public void IterateHierarchy(Action<SceneNode> nodeAction)
        {
            nodeAction(this);

            lock (Transform.Children)
            {
                foreach (var child in Transform.Children)
                    child?.SceneNode?.IterateHierarchy(nodeAction);
            }
        }

        public bool HasComponent(Type requiredType)
        {
            lock (Components)
            {
                return ComponentsInternal.Any(requiredType.IsInstanceOfType);
            }
        }

        public bool HasComponent<T>() where T : XRComponent
        {
            lock (Components)
            {
                return ComponentsInternal.Any(x => x is T);
            }
        }

        public bool TryGetComponent(Type type, out XRComponent? comp)
        {
            comp = GetComponent(type);
            return comp != null;
        }
        public bool TryGetComponent<T>(out T? comp) where T : XRComponent
        {
            comp = GetComponent<T>();
            return comp != null;
        }

        public bool TryGetComponents(Type type, out XRComponent[] comps)
        {
            comps = GetComponents(type);
            return comps.Length > 0;
        }
        public bool TryGetComponents<T>(out T[] comps) where T : XRComponent
        {
            comps = GetComponents<T>();
            return comps.Length > 0;
        }

        #region EventList Implementation

        public int Count => ((IEventListReadOnly<XRComponent>)ComponentsInternal).Count;

        public bool IsReadOnly => ((ICollection<XRComponent>)ComponentsInternal).IsReadOnly;

        public bool IsFixedSize => ((IList)ComponentsInternal).IsFixedSize;

        public bool IsSynchronized => ((ICollection)ComponentsInternal).IsSynchronized;

        public object SyncRoot => ((ICollection)ComponentsInternal).SyncRoot;

        public event EventList<XRComponent>.SingleCancelableHandler PreAnythingAdded
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreAnythingAdded += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreAnythingAdded -= value;
            }
        }

        public event EventList<XRComponent>.SingleHandler PostAnythingAdded
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostAnythingAdded += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostAnythingAdded -= value;
            }
        }

        public event EventList<XRComponent>.SingleCancelableHandler PreAnythingRemoved
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreAnythingRemoved += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreAnythingRemoved -= value;
            }
        }

        public event EventList<XRComponent>.SingleHandler PostAnythingRemoved
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostAnythingRemoved += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostAnythingRemoved -= value;
            }
        }

        public event EventList<XRComponent>.SingleCancelableHandler PreAdded
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreAdded += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreAdded -= value;
            }
        }

        public event EventList<XRComponent>.SingleHandler PostAdded
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostAdded += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostAdded -= value;
            }
        }

        public event EventList<XRComponent>.MultiCancelableHandler PreAddedRange
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreAddedRange += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreAddedRange -= value;
            }
        }

        public event EventList<XRComponent>.MultiHandler PostAddedRange
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostAddedRange += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostAddedRange -= value;
            }
        }

        public event EventList<XRComponent>.SingleCancelableHandler PreRemoved
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreRemoved += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreRemoved -= value;
            }
        }

        public event EventList<XRComponent>.SingleHandler PostRemoved
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostRemoved += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostRemoved -= value;
            }
        }

        public event EventList<XRComponent>.MultiCancelableHandler PreRemovedRange
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreRemovedRange += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreRemovedRange -= value;
            }
        }

        public event EventList<XRComponent>.MultiHandler PostRemovedRange
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostRemovedRange += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostRemovedRange -= value;
            }
        }

        public event EventList<XRComponent>.SingleCancelableInsertHandler PreInserted
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreInserted += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreInserted -= value;
            }
        }

        public event EventList<XRComponent>.SingleInsertHandler PostInserted
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostInserted += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostInserted -= value;
            }
        }

        public event EventList<XRComponent>.MultiCancelableInsertHandler PreInsertedRange
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreInsertedRange += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreInsertedRange -= value;
            }
        }

        public event EventList<XRComponent>.MultiInsertHandler PostInsertedRange
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostInsertedRange += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostInsertedRange -= value;
            }
        }

        public event Func<bool> PreModified
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreModified += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreModified -= value;
            }
        }

        public event Action PostModified
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostModified += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostModified -= value;
            }
        }

        public event EventList<XRComponent>.PreIndexSetHandler PreIndexSet
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreIndexSet += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PreIndexSet -= value;
            }
        }

        public event EventList<XRComponent>.PostIndexSetHandler PostIndexSet
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostIndexSet += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).PostIndexSet -= value;
            }
        }

        public event TCollectionChangedEventHandler<XRComponent> CollectionChanged
        {
            add
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).CollectionChanged += value;
            }

            remove
            {
                ((IEventListReadOnly<XRComponent>)ComponentsInternal).CollectionChanged -= value;
            }
        }

        public int IndexOf(XRComponent item)
        {
            return ((IList<XRComponent>)ComponentsInternal).IndexOf(item);
        }

        public void Insert(int index, XRComponent item)
        {
            ((IList<XRComponent>)ComponentsInternal).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<XRComponent>)ComponentsInternal).RemoveAt(index);
        }

        public void Add(XRComponent item)
        {
            ((ICollection<XRComponent>)ComponentsInternal).Add(item);
        }

        public void Clear()
        {
            ((ICollection<XRComponent>)ComponentsInternal).Clear();
        }

        public bool Contains(XRComponent item)
        {
            return ((ICollection<XRComponent>)ComponentsInternal).Contains(item);
        }

        public void CopyTo(XRComponent[] array, int arrayIndex)
        {
            ((ICollection<XRComponent>)ComponentsInternal).CopyTo(array, arrayIndex);
        }

        public bool Remove(XRComponent item)
        {
            return ((ICollection<XRComponent>)ComponentsInternal).Remove(item);
        }

        public int Add(object? value)
        {
            return ((IList)ComponentsInternal).Add(value);
        }

        public bool Contains(object? value)
        {
            return ((IList)ComponentsInternal).Contains(value);
        }

        public int IndexOf(object? value)
        {
            return ((IList)ComponentsInternal).IndexOf(value);
        }

        public void Insert(int index, object? value)
        {
            ((IList)ComponentsInternal).Insert(index, value);
        }

        public void Remove(object? value)
        {
            ((IList)ComponentsInternal).Remove(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)ComponentsInternal).CopyTo(array, index);
        }

        #endregion

        public string PrintTree()
        {
            string name = Name ?? "<no name>";
            string depth = new(' ', Transform.Depth * 2);
            string output = $"{depth}{Transform}{Environment.NewLine}";
            lock (Transform.Children)
            {
                foreach (var child in Transform.Children)
                    if (child?.SceneNode is SceneNode node)
                        output += node.PrintTree();
            }
            return output;
        }

        public delegate bool DelFindDescendant(string fullPath, string nodeName);
        public SceneNode? FindDescendantByName(string name, StringComparison comp = StringComparison.Ordinal)
            => FindDescendant((fullPath, nodeName) => string.Equals(name, nodeName, comp));
        public SceneNode? FindDescendant(string path, string pathSplitter = "/")
            => FindDescendant(path, (fullPath, nodeName) => fullPath == nodeName, pathSplitter);
        public SceneNode? FindDescendant(DelFindDescendant comparer, string pathSplitter = "/")
            => FindDescendant(Name ?? string.Empty, comparer, pathSplitter);
        private SceneNode? FindDescendant(string fullPath, DelFindDescendant comparer, string pathSplitter)
        {
            string name = Name ?? string.Empty;
            if (comparer(fullPath, name))
                return this;
            fullPath += $"{pathSplitter}{name}";
            lock (Transform.Children)
            {
                foreach (var child in Transform.Children)
                    if (child?.SceneNode is SceneNode node)
                        if (node.FindDescendant(fullPath, comparer, pathSplitter) is SceneNode found)
                            return found;
            }
            return null;
        }
    }
}
