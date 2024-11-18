using XREngine.Components;

namespace XREngine.Scene.Components.Physics
{
    public class StaticRigidBodyComponent : XRComponent
    {
        private IAbstractStaticRigidBody? _rigidBody;
        public IAbstractStaticRigidBody? RigidBody
        {
            get => _rigidBody;
            private set => SetField(ref _rigidBody, value);
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            RigidBody = World?.PhysicsScene?.NewStaticRigidBody();
        }
        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();

            RigidBody?.Destroy();
            RigidBody = null;
        }
    }
}
