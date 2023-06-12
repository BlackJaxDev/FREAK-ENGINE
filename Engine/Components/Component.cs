using XREngine.Components.Camera;
using XREngine.Scenes;
using XREngine.Scenes.Transforms;

namespace XREngine.Components
{
    public class Component
    {
        public Component(SceneNode node) { OwningNode = node; }

        public SceneNode OwningNode;

        public TransformBase Transform => OwningNode.Transform;

        public virtual void PreRender(CameraComponent camera) { }
        public virtual void Awake() { }
        public virtual void Start() { }
        public virtual void Update() { }
    }
}
