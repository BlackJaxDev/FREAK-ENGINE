﻿using Extensions;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Core;
using YamlDotNet.Serialization;

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
            /// <summary>
            /// No interpolation or extrapolation. Stays still until next physics update.
            /// </summary>
            Discrete,
            /// <summary>
            /// Smoothly interpolate between physics the last physics update and the most recent physics update. Looks smooth, but 1-frame delay.
            /// </summary>
            Interpolate,
            /// <summary>
            /// Predict the next physics update based on the current velocity. Keeps up, but looks inaccurate on sharp velocity changes.
            /// </summary>
            Extrapolate
        }

        private EInterpolationMode _interpolationMode = EInterpolationMode.Discrete;
        /// <summary>
        /// Determines how this transform should update between physics updates.
        /// Discrete: No interpolation or extrapolation. Stays still until next physics update.
        /// Interpolate: Smoothly interpolate between physics the last physics update and the most recent physics update. Looks smooth, but 1-frame delay.
        /// Extrapolate: Predict the next physics update based on the current velocity. Keeps up, but looks inaccurate on sharp velocity changes.
        /// </summary>
        public EInterpolationMode InterpolationMode
        {
            get => _interpolationMode;
            set => SetField(ref _interpolationMode, value);
        }

        private Vector3 _position;
        /// <summary>
        /// The position of this transform in *world* space.
        /// Set by the physics engine.
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            private set => SetField(ref _position, value);
        }

        private Quaternion _rotation;
        /// <summary>
        /// The rotation of this transform in *world* space.
        /// Set by the physics engine.
        /// </summary>
        public Quaternion Rotation
        {
            get => _rotation;
            private set => SetField(ref _rotation, value);
        }

        private Quaternion _preRotationOffset = Quaternion.Identity;
        /// <summary>
        /// The rotation offset to apply to the rotation of this transform *before* the physics engine sets it.
        /// Use-case would be uncommon, and is probably not needed.
        /// </summary>
        public Quaternion PreRotationOffset
        {
            get => _preRotationOffset;
            set => SetField(ref _preRotationOffset, value);
        }

        //TODO: why does physx init 90 degrees on Z?
        private Quaternion _postRotationOffset = Quaternion.CreateFromAxisAngle(Globals.Backward, XRMath.DegToRad(-90.0f));
        /// <summary>
        /// The rotation offset to apply to the rotation of this transform *after* the physics engine sets it.
        /// </summary>
        public Quaternion PostRotationOffset
        {
            get => _postRotationOffset;
            set => SetField(ref _postRotationOffset, value);
        }

        private Vector3 _positionOffset = Vector3.Zero;
        /// <summary>
        /// The position offset to apply to the position of this transform.
        /// </summary>
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

        private float _accumulatedTime = 0.0f;
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

        private (Vector3 position, Quaternion rotation) _lastPhysicsTransform;
        /// <summary>
        /// The transform of this object at the last physics update.
        /// </summary>
        [YamlIgnore]
        public (Vector3 position, Quaternion rotation) LastPhysicsTransform
        {
            get => _lastPhysicsTransform;
            private set => SetField(ref _lastPhysicsTransform, value);
        }

        private Vector3 _lastPhysicsLinearVelocity;
        /// <summary>
        /// The linear velocity of this transform at the last physics update.
        /// </summary>
        [YamlIgnore]
        public Vector3 LastPhysicsLinearVelocity
        {
            get => _lastPhysicsLinearVelocity;
            private set => SetField(ref _lastPhysicsLinearVelocity, value);
        }

        private Vector3 _lastPhysicsAngularVelocity;
        /// <summary>
        /// The angular velocity of this transform at the last physics update.
        /// </summary>
        [YamlIgnore]
        public Vector3 LastPhysicsAngularVelocity
        {
            get => _lastPhysicsAngularVelocity;
            private set => SetField(ref _lastPhysicsAngularVelocity, value);
        }

        private Vector3 _lastPosition;
        /// <summary>
        /// The position of this transform at the last physics update.
        /// Not used when InterpolationMode is set to Discrete, or when normal update ticks are running slower than the fixed update ticks.
        /// </summary>
        [YamlIgnore]
        public Vector3 LastPosition
        {
            get => _lastPosition;
            private set => SetField(ref _lastPosition, value);
        }

        private Quaternion _lastRotation;
        /// <summary>
        /// The rotation of this transform at the last physics update.
        /// Not used when InterpolationMode is set to Discrete, or when normal update ticks are running slower than the fixed update ticks.
        /// </summary>
        [YamlIgnore]
        public Quaternion LastRotation
        {
            get => _lastRotation;
            private set => SetField(ref _lastRotation, value);
        }

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
            => Matrix4x4.CreateFromQuaternion(PostRotationOffset * Rotation * PreRotationOffset) * Matrix4x4.CreateTranslation(PositionOffset + Position);
    }
}