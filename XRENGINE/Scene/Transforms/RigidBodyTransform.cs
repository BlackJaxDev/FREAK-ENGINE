using Extensions;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Core;

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

        public enum EInterpolationMode
        {
            Discrete,
            Interpolate,
            Extrapolate
        }

        private EInterpolationMode _interpolationMode = EInterpolationMode.Discrete;
        public EInterpolationMode InterpolationMode
        {
            get => _interpolationMode;
            set => SetField(ref _interpolationMode, value);
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

        private Quaternion _rotationOffset = Quaternion.CreateFromAxisAngle(Globals.Backward, XRMath.DegToRad(-90.0f));
        public Quaternion PostRotationOffset
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
                        {
                            World.PhysicsScene.OnSimulationStep -= OnPhysicsStepped;
                            World.UnregisterTick(ETickGroup.Late, (int)ETickOrder.Scene, OnUpdate);
                        }
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
                    {
                        World.PhysicsScene.OnSimulationStep += OnPhysicsStepped;
                        World.RegisterTick(ETickGroup.Late, (int)ETickOrder.Scene, OnUpdate);
                    }
                    break;
            }
        }

        private float _accumulatedTime;
        private void OnUpdate()
        {
            float updateDelta = Engine.Delta;
            float fixedDelta = Engine.Time.Timer.FixedUpdateDelta;
            if (InterpolationMode == EInterpolationMode.Discrete || updateDelta > fixedDelta)
                return;

            _accumulatedTime += Engine.Delta;
            float alpha = _accumulatedTime / Engine.Time.Timer.FixedUpdateDelta;

            var (lastPosUpdate, lastRotUpdate) = LastPhysicsTransform;
            switch (InterpolationMode)
            {
                case EInterpolationMode.Interpolate:
                    {
                        Position = Vector3.Lerp(LastPosition, lastPosUpdate, alpha);
                        Rotation = Quaternion.Slerp(LastRotation, lastRotUpdate, alpha);
                        break;
                    }
                case EInterpolationMode.Extrapolate:
                    {
                        Vector3 posDelta = LastPhysicsLinearVelocity * _accumulatedTime;
                        Position = posDelta.Length() > float.Epsilon ? lastPosUpdate + posDelta : lastPosUpdate;

                        float angle = LastPhysicsAngularVelocity.Length() * _accumulatedTime;
                        Rotation = angle > float.Epsilon
                            ? Quaternion.CreateFromAxisAngle(LastPhysicsAngularVelocity.Normalized(), angle) * lastRotUpdate
                            : lastRotUpdate;

                        break;
                    }
            }
        }

        public (Vector3 position, Quaternion rotation) LastPhysicsTransform { get; set; }
        public Vector3 LastPhysicsLinearVelocity { get; private set; }
        public Vector3 LastPhysicsAngularVelocity { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public Quaternion LastRotation { get; private set; }

        private void OnPhysicsStepped()
        {
            if (RigidBody is null)
                return;

            LastPhysicsTransform = RigidBody.Transform;
            LastPhysicsLinearVelocity = RigidBody.LinearVelocity;
            LastPhysicsAngularVelocity = RigidBody.AngularVelocity;

            float updateDelta = Engine.Delta;
            float fixedDelta = Engine.Time.Timer.FixedUpdateDelta;
            if (InterpolationMode == EInterpolationMode.Discrete || updateDelta > fixedDelta)
            {
                Position = LastPhysicsTransform.position;
                Rotation = LastPhysicsTransform.rotation;
            }
            else
            {
                LastPosition = Position;
                LastRotation = Rotation;
                _accumulatedTime = 0;
            }
        }

        protected override Matrix4x4 CreateLocalMatrix()
            => Matrix4x4.Identity;
        protected override Matrix4x4 CreateWorldMatrix()
            => Matrix4x4.CreateFromQuaternion(PostRotationOffset * Rotation) * Matrix4x4.CreateTranslation(PositionOffset + Position);
    }
}