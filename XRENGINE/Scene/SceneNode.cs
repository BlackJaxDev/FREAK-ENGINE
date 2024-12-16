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
    public sealed class SceneNode : XRWorldObjectBase
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
                SetTransform(transform, ETransformSetFlags.None);

            Transform.Parent = parent?.Transform;
            Name = name;
        }

        public SceneNode(SceneNode parent, TransformBase? transform = null)
            : this(parent, DefaultName, transform) { }
        public SceneNode(string name, TransformBase? transform = null)
        {
            if (transform != null)
                SetTransform(transform, ETransformSetFlags.None);

            Transform.Parent = null;
            Name = name;
            ComponentsInternal.PostAnythingAdded += ComponentAdded;
            ComponentsInternal.PostAnythingRemoved += ComponentRemoved;
        }
        public SceneNode(XRScene scene, string name, TransformBase? transform = null)
        {
            if (transform != null)
                SetTransform(transform, ETransformSetFlags.None);

            scene.RootNodes.Add(this);
            Name = name;
            ComponentsInternal.PostAnythingAdded += ComponentAdded;
            ComponentsInternal.PostAnythingRemoved += ComponentRemoved;
        }
        public SceneNode(XRWorldInstance? world, string? name = null, TransformBase? transform = null)
        {
            if (transform != null)
                SetTransform(transform, ETransformSetFlags.None);

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

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Transform):
                        if (_transform != null)
                            UnlinkTransform();
                        break;
                }
            }
            return change;
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
                    if (_transform != null)
                    {
                        _transform.Name = Name;
                        LinkTransform();
                    }
                    break;
                case nameof(Name):
                    if (_transform != null)
                        _transform.Name = Name;
                    break;
            }
        }

        private void UnlinkTransform()
        {
            if (_transform is null)
                return;

            if (IsActiveInHierarchy)
                _transform.OnSceneNodeDeactivated();
            _transform.PropertyChanged -= TransformPropertyChanged;
            _transform.PropertyChanging -= TransformPropertyChanging;
            _transform.SceneNode = null;
            _transform.World = null;
            _transform.Parent = null;
        }

        private void LinkTransform()
        {
            if (_transform is null)
                return;

            _transform.SceneNode = this;
            _transform.World = World;
            _transform.PropertyChanged += TransformPropertyChanged;
            _transform.PropertyChanging += TransformPropertyChanging;
            if (IsActiveInHierarchy)
                _transform.OnSceneNodeActivated();
        }

        private void TransformPropertyChanging(object? sender, IXRPropertyChangingEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TransformBase.Parent):
                    OnParentChanging();
                    break;
            }
        }

        private void TransformPropertyChanged(object? sender, IXRPropertyChangedEventArgs e)
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
            private set => SetField(ref _transform, value);
        }

        /// <summary>
        /// Retrieves the transform of this scene node as type T.
        /// If forceConvert is true, the transform will be converted to type T if it is not already.
        /// If the transform is a derived type of T, it will be returned as type T but will not be converted.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="forceConvert"></param>
        /// <returns></returns>
        public T? GetTransformAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(bool forceConvert = false) where T : TransformBase, new()
            => !forceConvert
                ? Transform as T :
                Transform is T value
                    ? value
                    : SetTransform<T>();

        /// <summary>
        /// Attempts to retrieve the transform of this scene node as type T.
        /// If the transform is not of type T, transform will be null and the method will return false.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transform"></param>
        /// <returns></returns>
        public bool TryGetTransformAs<T>([MaybeNullWhen(false)] out T? transform) where T : TransformBase
        {
            transform = Transform as T;
            return transform != null;
        }

        public enum ETransformSetFlags
        {
            /// <summary>
            /// Transform is set as-is.
            /// </summary>
            None = 0,
            /// <summary>
            /// The parent of the new transform will be set to the parent of the current transform.
            /// </summary>
            RetainCurrentParent = 1,
            /// <summary>
            /// The world transform of the new transform will be set to the world transform of the current transform, if possible.
            /// For a transform's world matrix to be preserved, 
            /// </summary>
            RetainWorldTransform = 2,
            /// <summary>
            /// The children of the new transform will be cleared before it is set.
            /// </summary>
            ClearNewChildren = 4,
            /// <summary>
            /// The children of the current transform will be retained when setting the new transform.
            /// </summary>
            RetainCurrentChildren = 8,

            /// <summary>
            /// Retain the current parent, clear the new children, and retain the current children.
            /// World transform will not be retained.
            /// </summary>
            Default = RetainCurrentParent | ClearNewChildren | RetainCurrentChildren
        }

        /// <summary>
        /// Sets the transform of this scene node.
        /// If retainParent is true, the parent of the new transform will be set to the parent of the current transform.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="retainParent"></param>
        public void SetTransform(TransformBase transform, ETransformSetFlags flags = ETransformSetFlags.Default)
        {
            if (flags.HasFlag(ETransformSetFlags.ClearNewChildren))
                transform.Clear();

            if (flags.HasFlag(ETransformSetFlags.RetainCurrentParent))
                transform.SetParent(_transform?.Parent, flags.HasFlag(ETransformSetFlags.RetainWorldTransform));

            if (flags.HasFlag(ETransformSetFlags.RetainCurrentChildren))
            {
                if (_transform is not null)
                {
                    lock (_transform.Children)
                    {
                        foreach (var child in _transform)
                            transform.Add(child);
                    }
                }
            }

            Transform = transform;
        }

        /// <summary>
        /// Sets the transform of this scene node to a new instance of type T.
        /// If retainParent is true, the parent of the new transform will be set to the parent of the current transform.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="retainParent"></param>
        public T SetTransform<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(ETransformSetFlags flags = ETransformSetFlags.Default) where T : TransformBase, new()
        {
            T value = new();
            SetTransform(value, flags);
            return value;
        }

        /// <summary>
        /// The immediate ancestor of this scene node, or null if this scene node is the root of the scene.
        /// </summary>
        public SceneNode? Parent
        {
            get => _transform?.Parent?.SceneNode;
            set
            {
                if (_transform is not null)
                    _transform.Parent = value?.Transform;
            }
        }

        // TODO: set and unset world to transform and components when enabled and disabled
        private void SetWorldToChildNodes(XRWorldInstance? value)
        {
            Transform.World = World;

            lock (Components)
            {
                foreach (var component in Components)
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

        public (T1? comp1, T2? comp2) AddComponents<T1, T2>() where T1 : XRComponent where T2 : XRComponent
        {
            var comp1 = AddComponent<T1>();
            var comp2 = AddComponent<T2>();
            return (comp1, comp2);
        }

        public (T1? comp1, T2? comp2, T3? comp3) AddComponents<T1, T2, T3>() where T1 : XRComponent where T2 : XRComponent where T3 : XRComponent
        {
            var comp1 = AddComponent<T1>();
            var comp2 = AddComponent<T2>();
            var comp3 = AddComponent<T3>();
            return (comp1, comp2, comp3);
        }

        public (T1? comp1, T2? comp2, T3? comp3, T4? comp4) AddComponents<T1, T2, T3, T4>() where T1 : XRComponent where T2 : XRComponent where T3 : XRComponent where T4 : XRComponent
        {
            var comp1 = AddComponent<T1>();
            var comp2 = AddComponent<T2>();
            var comp3 = AddComponent<T3>();
            var comp4 = AddComponent<T4>();
            return (comp1, comp2, comp3, comp4);
        }

        public (T1? comp1, T2? comp2, T3? comp3, T4? comp4, T5? comp5) AddComponents<T1, T2, T3, T4, T5>() where T1 : XRComponent where T2 : XRComponent where T3 : XRComponent where T4 : XRComponent where T5 : XRComponent
        {
            var comp1 = AddComponent<T1>();
            var comp2 = AddComponent<T2>();
            var comp3 = AddComponent<T3>();
            var comp4 = AddComponent<T4>();
            var comp5 = AddComponent<T5>();
            return (comp1, comp2, comp3, comp4, comp5);
        }

        public (T1? comp1, T2? comp2, T3? comp3, T4? comp4, T5? comp5, T6? comp6) AddComponents<T1, T2, T3, T4, T5, T6>() where T1 : XRComponent where T2 : XRComponent where T3 : XRComponent where T4 : XRComponent where T5 : XRComponent where T6 : XRComponent
        {
            var comp1 = AddComponent<T1>();
            var comp2 = AddComponent<T2>();
            var comp3 = AddComponent<T3>();
            var comp4 = AddComponent<T4>();
            var comp5 = AddComponent<T5>();
            var comp6 = AddComponent<T6>();
            return (comp1, comp2, comp3, comp4, comp5, comp6);
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

            foreach (XRComponent component in Components)
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

            foreach (XRComponent component in Components)
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

        public void AddChild(SceneNode node)
        {
            Transform.Add(node.Transform);
        }
        public void InsertChild(SceneNode node, int index)
        {
            Transform.Insert(index, node.Transform);
        }
        public void RemoveChild(SceneNode node)
        {
            Transform.Remove(node.Transform);
        }
        public void RemoveChildAt(int index)
        {
            Transform.RemoveAt(index);
        }

        public static SceneNode New<T1>(SceneNode? parentNode, out T1 comp1) where T1 : XRComponent
        {
            var node = parentNode is null ? new SceneNode() : new SceneNode(parentNode);
            comp1 = node.AddComponent<T1>()!;
            return node;
        }
        public static SceneNode New<T1, T2>(SceneNode? parentNode, out T1 comp1, out T2 comp2) where T1 : XRComponent where T2 : XRComponent
        {
            var node = parentNode is null ? new SceneNode() : new SceneNode(parentNode);
            comp1 = node.AddComponent<T1>()!;
            comp2 = node.AddComponent<T2>()!;
            return node;
        }
        public static SceneNode New<T1, T2, T3>(SceneNode? parentNode, out T1 comp1, out T2 comp2, out T3 comp3) where T1 : XRComponent where T2 : XRComponent where T3 : XRComponent
        {
            var node = parentNode is null ? new SceneNode() : new SceneNode(parentNode);
            comp1 = node.AddComponent<T1>()!;
            comp2 = node.AddComponent<T2>()!;
            comp3 = node.AddComponent<T3>()!;
            return node;
        }
        public static SceneNode New<T1, T2, T3, T4>(SceneNode? parentNode, out T1 comp1, out T2 comp2, out T3 comp3, out T4 comp4) where T1 : XRComponent where T2 : XRComponent where T3 : XRComponent where T4 : XRComponent
        {
            var node = parentNode is null ? new SceneNode() : new SceneNode(parentNode);
            comp1 = node.AddComponent<T1>()!;
            comp2 = node.AddComponent<T2>()!;
            comp3 = node.AddComponent<T3>()!;
            comp4 = node.AddComponent<T4>()!;
            return node;
        }
        public static SceneNode New<T1, T2, T3, T4, T5>(SceneNode? parentNode, out T1 comp1, out T2 comp2, out T3 comp3, out T4 comp4, out T5 comp5) where T1 : XRComponent where T2 : XRComponent where T3 : XRComponent where T4 : XRComponent where T5 : XRComponent
        {
            var node = parentNode is null ? new SceneNode() : new SceneNode(parentNode);
            comp1 = node.AddComponent<T1>()!;
            comp2 = node.AddComponent<T2>()!;
            comp3 = node.AddComponent<T3>()!;
            comp4 = node.AddComponent<T4>()!;
            comp5 = node.AddComponent<T5>()!;
            return node;
        }
        public static SceneNode New<T1, T2, T3, T4, T5, T6>(SceneNode? parentNode, out T1 comp1, out T2 comp2, out T3 comp3, out T4 comp4, out T5 comp5, out T6 comp6) where T1 : XRComponent where T2 : XRComponent where T3 : XRComponent where T4 : XRComponent where T5 : XRComponent where T6 : XRComponent
        {
            var node = parentNode is null ? new SceneNode() : new SceneNode(parentNode);
            comp1 = node.AddComponent<T1>()!;
            comp2 = node.AddComponent<T2>()!;
            comp3 = node.AddComponent<T3>()!;
            comp4 = node.AddComponent<T4>()!;
            comp5 = node.AddComponent<T5>()!;
            comp6 = node.AddComponent<T6>()!;
            return node;
        }

        /// <summary>
        /// Returns the first child of this scene node, if any.
        /// </summary>
        /// <returns></returns>
        public SceneNode? FirstChild
            => Transform.FirstChild()?.SceneNode;

        /// <summary>
        /// Returns the last child of this scene node, if any.
        /// </summary>
        /// <returns></returns>
        public SceneNode? LastChild
            => Transform.LastChild()?.SceneNode;

        public bool IsTransformNull => _transform is null;
    }
}
