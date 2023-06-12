using System.Collections;
using XREngine.Components;
using XREngine.Scenes.Transforms;

namespace XREngine.Scenes
{
    public class SceneNode : IEnumerable<Component>
    {
        private readonly List<Component> _components = new();
        public IReadOnlyList<Component> Components => _components;

        private TransformBase _transform = new Transform();
        public TransformBase Transform
        {
            get => _transform;
            set => _transform = value;
        }

        public SceneNode? Parent => Transform.Parent?.SceneNode;

        public void AddComponent<T>() where T : Component, new()
        {
            _components.Add(new T());
        }
        public void AddComponent(Type type)
        {
            if (type is null || !type.IsSubclassOf(typeof(Component)) || Activator.CreateInstance(type) is not Component item)
                return;

            _components.Add(item);
        }

        public virtual void Awake() { }
        public virtual void Start() { }
        public virtual void Update() { }

        public IEnumerator<Component> GetEnumerator()
            => ((IEnumerable<Component>)_components).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)_components).GetEnumerator();
    }
}
