using XREngine.Core.Attributes;
using XREngine.Scene.Transforms;

namespace XREngine.Scene.Components.Physics
{
    [RequiresTransform(typeof(RigidBodyTransform))]
    public class StaticRigidBodyComponent : PhysicsActorComponent
    {
        public RigidBodyTransform RigidBodyTransform => SceneNode.GetTransformAs<RigidBodyTransform>(true)!;

        private IAbstractStaticRigidBody? _rigidBody;
        public IAbstractStaticRigidBody? RigidBody
        {
            get => _rigidBody;
            set => SetField(ref _rigidBody, value);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(RigidBody):
                        if (World is not null && RigidBody is not null && IsActive)
                            World.PhysicsScene.RemoveActor(RigidBody);
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
                case nameof(RigidBody):
                    if (World is not null && RigidBody is not null && IsActive)
                        World.PhysicsScene.AddActor(RigidBody);
                    break;
            }
        }
    }
}
