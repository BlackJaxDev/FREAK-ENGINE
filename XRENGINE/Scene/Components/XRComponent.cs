using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using XREngine.Data.Core;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Components
{
    [Serializable]
    public abstract class XRComponent : XRWorldObjectBase
    {
        /// <summary>
        /// Global event for when a component is created.
        /// </summary>
        public static XREvent<XRComponent> ComponentCreated;

        /// <summary>
        /// Global event for when a component is destroyed.
        /// </summary>
        public static XREvent<XRComponent> ComponentDestroyed;

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            private set => SetField(ref _isActive, value);
        }

        public XREvent<XRComponent> TransformChanged;
        public XREvent<(XRComponent, TransformBase)> LocalMatrixChanged;
        public XREvent<(XRComponent, TransformBase)> WorldMatrixChanged;

        //TODO: figure out how to disallow users from constructing xrcomponents
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        internal XRComponent() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        internal static T New<T>(SceneNode node) where T : XRComponent 
            => (T)New(node, typeof(T))!;

        internal static XRComponent? New(SceneNode node, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type t)
        {
            if (t is null || !t.IsSubclassOf(typeof(XRComponent)))
                return null;

#pragma warning disable SYSLIB0050 // Type or member is obsolete
            object obj = FormatterServices.GetUninitializedObject(t);
#pragma warning restore SYSLIB0050 // Type or member is obsolete
            Type t2 = obj!.GetType();
            var method = typeof(XRComponent).GetMethod(nameof(ConstructionSetSceneNode), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            method!.Invoke(obj, [node]);
            t2.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy, Type.EmptyTypes)?.Invoke(obj, null);
            
            var component = (XRComponent)obj;
            component.Constructing();

            ComponentCreated.Invoke(component);

            return component;
        }

        /// <summary>
        /// Retrieves a component also located on the same parent scene node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public bool TryGetSiblingComponent<T>(out T? component) where T : XRComponent
        {
            var comp = SceneNode.GetComponent<T>();
            if (comp == this)
            {
                component = null;
                return false;
            }
            return (component = comp) != null;
        }
        /// <summary>
        /// Retrieves a component also located on the same parent scene node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? GetSiblingComponent<T>(bool createIfNotExist = false) where T : XRComponent
        {
            var comp = SceneNode.GetComponent<T>();
            if (comp is null && createIfNotExist)
                comp = New<T>(SceneNode);
            return comp == this ? null : comp;
        }
        
#pragma warning disable IDE0051 // Remove unused private members
        private void ConstructionSetSceneNode(SceneNode node)
#pragma warning restore IDE0051 // Remove unused private members
        {
            _sceneNode = node;
            World = _sceneNode.World;
            _sceneNode.PropertyChanging += SceneNodePropertyChanging;
            _sceneNode.PropertyChanged += SceneNodePropertyChanged;
            Transform.LocalMatrixChanged += OnTransformLocalMatrixChanged;
            Transform.WorldMatrixChanged += OnTransformWorldMatrixChanged;
        }

        private SceneNode _sceneNode;
        private bool _clearTicksOnStop = true;

        /// <summary>
        /// Scene node refers to the node that this component is attached to.
        /// It will be set automatically when the component is added to a scene node, and never change.
        /// If you set any events on the scene node from a component, make sure to unregister them by overriding OnDestroying().
        /// </summary>
        public SceneNode SceneNode
        {
            get => _sceneNode;
            private set
            {
                _sceneNode.PropertyChanging -= SceneNodePropertyChanging;
                _sceneNode.PropertyChanged -= SceneNodePropertyChanged;
                OnTransformChanging();

                SetField(ref _sceneNode, value);

                World = _sceneNode.World;

                _sceneNode.PropertyChanging += SceneNodePropertyChanging;
                _sceneNode.PropertyChanged += SceneNodePropertyChanged;
                OnTransformChanged();
            }
        }

        private void SceneNodePropertyChanging(object? sender, PropertyChangingEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(XREngine.Scene.SceneNode.Transform):
                    OnTransformChanging();
                    break;
            }
        }

        /// <summary>
        /// The transform of the scene node this component is attached to.
        /// Will never be null, because components always have to exist attached to a scene node.
        /// </summary>
        public TransformBase Transform => SceneNode.Transform;

        public T TransformAs<T>() where T : TransformBase
            => (T)Transform;
        public bool TransformIs<T>(out T? transform) where T : TransformBase
        {
            if (Transform is T t)
            {
                transform = t;
                return true;
            }
            transform = null;
            return false;
        }

        private void SceneNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(XREngine.Scene.SceneNode.Transform):
                    OnTransformChanged();
                    break;
            }
        }

        private void OnTransformChanging()
        {
            Transform.LocalMatrixChanged -= OnTransformLocalMatrixChanged;
            Transform.WorldMatrixChanged -= OnTransformWorldMatrixChanged;
        }

        protected virtual void OnTransformChanged()
        {
            TransformChanged.Invoke(this);
            Transform.LocalMatrixChanged += OnTransformLocalMatrixChanged;
            Transform.WorldMatrixChanged += OnTransformWorldMatrixChanged;
        }

        protected virtual void OnTransformLocalMatrixChanged(TransformBase transform)
            => LocalMatrixChanged.Invoke((this, transform));
        protected virtual void OnTransformWorldMatrixChanged(TransformBase transform)
            => WorldMatrixChanged.Invoke((this, transform));

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);

            switch (propName)
            {
                case nameof(IsActive):
                    if (IsActive)
                        Start();
                    else
                        Stop();
                    break;
            }
        }

        /// <summary>
        /// Called when the component is first created and attached to this scene node.
        /// </summary>
        protected virtual void Constructing() { }
        /// <summary>
        /// Called when the component is made active.
        /// This is where ticks should register and connections to the world should be established.
        /// </summary>
        protected internal virtual void Start()
            => VerifyInterfacesOnStart();

        public bool ClearTicksOnStop
        {
            get => _clearTicksOnStop;
            set => SetField(ref _clearTicksOnStop, value);
        }

        /// <summary>
        /// Called when the component is made inactive.
        /// </summary>
        protected internal virtual void Stop()
        {
            VerifyInterfacesOnStop();
            if (ClearTicksOnStop)
                ClearTicks();
        }

        /// <summary>
        /// This method is called when the component is set to active in the world.
        /// It will check for known engine interfaces set by the user and apply engine data to them.
        /// </summary>
        private void VerifyInterfacesOnStart()
        {
            if (this is IRenderable rend)
            {
                foreach (var obj in rend.RenderedObjects)
                {
                    obj.WorldInstance = SceneNode?.World;
                }
            }
        }

        /// <summary>
        /// This method is called when the component is set to inactive in the world.
        /// It will check for known engine interfaces set by the user and clear engine data from them.
        /// </summary>
        private void VerifyInterfacesOnStop()
        {
            if (this is IRenderable rend)
            {
                foreach (var obj in rend.RenderedObjects)
                {
                    obj.WorldInstance = null;
                }
            }
        }

        /// <summary>
        /// Called when the component is ticked in the scene.
        /// </summary>
        protected internal virtual void Update() { }

        protected override void OnDestroying()
        {
            base.OnDestroying();
            ComponentDestroyed.Invoke(this);
        }
    }
    public enum ETickGroup
    {
        /// <summary>
        /// Variable update tick, occurs every frame.
        /// </summary>
        Normal,
        /// <summary>
        /// Variable update tick, occurs after the normal tick.
        /// </summary>
        Late,
        /// <summary>
        /// Fixed update tick, occurs before physics calculations.
        /// </summary>
        PrePhysics,
        /// <summary>
        /// Fixed update tick, occurs after physics calculations.
        /// </summary>
        PostPhysics,
    }
    /// <summary>
    /// Cast to an int and add any value to change the order of ticks within a group.
    /// These are default ticking groups for the default render pipeline, but you may use any values you wish that correspond to the render pipeline.
    /// </summary>
    public enum ETickOrder
    {
        /// <summary>
        /// Timing events
        /// </summary>
        Timers = 0,
        /// <summary>
        /// Input consumption events
        /// </summary>
        Input = 200000,
        /// <summary>
        /// Animation evaluation events
        /// </summary>
        Animation = 400000,
        /// <summary>
        /// Gameplay logic events
        /// </summary>
        Logic = 600000,
        /// <summary>
        /// Scene hierarchy events
        /// </summary>
        Scene = 800000,
    }
}
