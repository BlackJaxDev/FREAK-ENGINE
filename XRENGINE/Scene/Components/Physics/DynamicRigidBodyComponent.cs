using XREngine.Components;
using XREngine.Core.Attributes;
using XREngine.Scene.Transforms;

namespace XREngine.Scene.Components.Physics
{
    [RequiresTransform(typeof(RigidBodyTransform))]
    public class DynamicRigidBodyComponent : XRComponent
    {
        private IAbstractDynamicRigidBody? _rigidBody;
        public IAbstractDynamicRigidBody? RigidBody
        {
            get => _rigidBody;
            private set => SetField(ref _rigidBody, value);
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            RigidBody = World?.PhysicsScene?.NewDynamicRigidBody();
        }
        protected internal override void OnComponentDeactivated()
        {
            RigidBody?.Destroy();
            RigidBody = null;

            base.OnComponentDeactivated();
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(RigidBody):
                    var tfm = SceneNode.GetTransformAs<RigidBodyTransform>(true)!;
                    tfm.RigidBody = RigidBody;
                    break;
            }
        }
    }
}
