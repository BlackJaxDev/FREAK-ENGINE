using System.Numerics;

namespace XREngine.Scene.Transforms
{
    public class RigidBodyTransform : TransformBase
    {
        private IAbstractRigidPhysicsActor? _rigidBody;
        public IAbstractRigidPhysicsActor? RigidBody
        {
            get => _rigidBody;
            set => SetField(ref _rigidBody, value);
        }

        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            private set => SetField(ref _position, value);
        }

        private Quaternion _rotation;

        public Quaternion Rotation
        {
            get => _rotation;
            private set => SetField(ref _rotation, value);
        }

        private Quaternion _rotationOffset = Quaternion.Identity;
        public Quaternion RotationOffset
        {
            get => _rotationOffset;
            set => SetField(ref _rotationOffset, value);
        }

        private Vector3 _positionOffset = Vector3.Zero;
        public Vector3 PositionOffset
        {
            get => _positionOffset;
            set => SetField(ref _positionOffset, value);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(World):
                        if (World is not null)
                            World.PhysicsScene.OnSimulationStep -= OnPhysicsStepped;
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
                case nameof(Position):
                case nameof(Rotation):
                    MarkWorldModified();
                    break;
                case nameof(RigidBody):
                    if (RigidBody is not null)
                        OnPhysicsStepped();
                    break;
                case nameof(World):
                    if (World is not null)
                        World.PhysicsScene.OnSimulationStep += OnPhysicsStepped;
                    break;
            }
        }

        private void OnPhysicsStepped()
        {
            if (RigidBody is null)
                return;

            var (position, rotation) = RigidBody.Transform;
            Position = PositionOffset + position;
            Rotation = RotationOffset * rotation;
        }

        protected override Matrix4x4 CreateLocalMatrix()
            => Matrix4x4.Identity;
        protected override Matrix4x4 CreateWorldMatrix()
            => Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
    }
}