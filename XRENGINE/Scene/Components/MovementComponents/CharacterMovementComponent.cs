﻿using Extensions;
using MagicPhysX;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Core;
using XREngine.Rendering.Physics.Physx;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Components
{
    [OneComponentAllowed]
    [RequiresTransform(typeof(RigidBodyTransform))]
    public class CharacterMovement3DComponent : PlayerMovementComponentBase
    {
        public RigidBodyTransform RigidBodyTransform
            => SceneNode.GetTransformAs<RigidBodyTransform>(true)!;
        //public Transform ControllerTransform
        //    => SceneNode.GetTransformAs<Transform>(true)!;

        private float _stepOffset = 0.0f;
        private float _slopeLimitCosine = 0.707f;
        private float _walkingMovementSpeed = 0.07f;
        private float _airMovementAcceleration = 0.5f;
        private float _jumpSpeed = 20.0f;
        private Func<Vector3, Vector3>? _subUpdateTick;
        private ECrouchState _crouchState = ECrouchState.Standing;
        private float _invisibleWallHeight = 0.0f;
        private float _density = 0.5f;
        private float _scaleCoeff = 0.8f;
        private float _volumeGrowth = 1.5f;
        private bool _slideOnSteepSlopes = true;
        private PhysxMaterial _material = new(0.9f, 0.9f, 0.1f);
        private float _radius = 0.1f;
        private float _standingHeight = 0.5f;
        private float _crouchedHeight = 0.2f;
        private float _proneHeight = 0.1f;
        private bool _constrainedClimbing = false;
        private CapsuleController? _controller;
        private float _minMoveDistance = 0.001f;
        private float _contactOffset = 0.01f;
        private Vector3 _upDirection = Globals.Up;
        private Vector3 _spawnPosition = Vector3.Zero;
        private Vector3 _velocity = Vector3.Zero;
        private Vector3? _gravityOverride = null;

        public float AirMovementAcceleration
        {
            get => _airMovementAcceleration;
            set => SetField(ref _airMovementAcceleration, value);
        }

        public Vector3 FootPosition
        {
            get => Controller?.FootPosition ?? (Transform.WorldTranslation - UpDirection * (CurrentHeight + Radius + ContactOffset));
            set
            {
                if (Controller is not null)
                    Controller.FootPosition = value;
            }
        }
        public Vector3 Position
        {
            get => Controller?.Position ?? Transform.WorldTranslation;
            set
            {
                if (Controller is not null)
                    Controller.Position = value;
            }
        }
        public Vector3 UpDirection
        { 
            get => _upDirection;
            set => SetField(ref _upDirection, value);
        }
        public float StepOffset
        {
            get => _stepOffset;
            set => SetField(ref _stepOffset, value);
        }
        /// <summary>
        /// The maximum slope which the character can walk up.
        /// In general it is desirable to limit where the character can walk, in particular it is unrealistic for the character to be able to climb arbitary slopes.
        /// The limit is expressed as the cosine of desired limit angle.
        /// A value of 0 disables this feature.
        /// </summary>
        public float SlopeLimitCosine
        {
            get => _slopeLimitCosine;
            set => SetField(ref _slopeLimitCosine, value);
        }
        public float SlopeLimitAngleRad
        {
            get => (float)Math.Acos(SlopeLimitCosine);
            set => SlopeLimitCosine = (float)Math.Cos(value);
        }
        public float SlopeLimitAngleDeg
        {
            get => XRMath.RadToDeg(SlopeLimitAngleRad);
            set => SlopeLimitAngleRad = XRMath.DegToRad(value);
        }
        public float WalkingMovementSpeed
        {
            get => _walkingMovementSpeed;
            set => SetField(ref _walkingMovementSpeed, value);
        }
        /// <summary>
        /// Maximum height a jumping character can reach.
        /// This is only used if invisible walls are created(‘invisibleWallHeight’ is non zero).
        /// When a character jumps, the non-walkable triangles he might fly over are not found by the collision queries
        /// (since the character’s bounding volume does not touch them).
        /// Thus those non-walkable triangles do not create invisible walls, and it is possible for a jumping character to land on a non-walkable triangle,
        /// while he wouldn’t have reached that place by just walking.
        /// The ‘maxJumpHeight’ variable is used to extend the size of the collision volume downward.
        /// This way, all the non-walkable triangles are properly found by the collision queries and it becomes impossible to ‘jump over’ invisible walls.
        /// If the character in your game can not jump, it is safe to use 0.0 here.Otherwise it is best to keep this value as small as possible, 
        /// since a larger collision volume means more triangles to process.
        /// </summary>
        public float MaxJumpHeight
        {
            get => _jumpSpeed;
            set => SetField(ref _jumpSpeed, value);
        }
        /// <summary>
        /// The contact offset used by the controller.
        /// Specifies a skin around the object within which contacts will be generated.
        /// Use it to avoid numerical precision issues.
        /// This is dependant on the scale of the users world, but should be a small, positive non zero value.
        /// </summary>
        public float ContactOffset
        {
            get => _contactOffset;
            set => SetField(ref _contactOffset, value);
        }
        public enum ECrouchState
        {
            Standing,
            Crouched,
            Prone
        }
        public ECrouchState CrouchState
        {
            get => _crouchState;
            set => SetField(ref _crouchState, value);
        }
        /// <summary>
        /// Height of invisible walls created around non-walkable triangles.
        /// The library can automatically create invisible walls around non-walkable triangles defined by the ‘slopeLimit’ parameter.
        /// This defines the height of those walls.
        /// If it is 0.0, then no extra triangles are created.
        /// </summary>
        public float InvisibleWallHeight
        {
            get => _invisibleWallHeight;
            set => SetField(ref _invisibleWallHeight, value);
        }
        /// <summary>
        /// Density of underlying kinematic actor.
        /// The CCT creates a PhysX’s kinematic actor under the hood.This controls its density.
        /// </summary>
        public float Density
        {
            get => _density;
            set => SetField(ref _density, value);
        }
        /// <summary>
        /// Scale coefficient for underlying kinematic actor.
        /// The CCT creates a PhysX’s kinematic actor under the hood.
        /// This controls its scale factor.
        /// This should be a number a bit smaller than 1.0.
        /// This scale factor affects how the character interacts with dynamic rigid bodies around it (e.g.pushing them, etc).
        /// With a scale factor < 1, the underlying kinematic actor will not touch surrounding rigid bodies - they will only interact with the character controller’s shapes (capsules or boxes),
        /// and users will have full control over the interactions(i.e.they will have to push the objects with explicit forces themselves).
        /// With a scale factor >=1, the underlying kinematic actor will touch and push surrounding rigid bodies based on PhysX’s computations, 
        /// as if there would be no character controller involved.This works fine except when you push objects into a wall.
        /// PhysX has no control over kinematic actors(since they are kinematic) so they would freely push dynamic objects into walls, and make them tunnel / explode / behave badly.
        /// With a smaller kinematic actor however, the character controller’s swept shape touches dynamic rigid bodies first, 
        /// and can apply forces to them to move them away (or not, depending on what the gameplay needs).
        /// Meanwhile the character controller’s swept shape itself is stopped by these dynamic bodies.
        /// Setting the scale factor to 1 could still work, but it is unreliable.
        /// Depending on FPU accuracy you could end up with either the CCT’s volume or the underlying kinematic actor touching the dynamic bodies first, and this could change from one moment to the next.
        /// </summary>
        public float ScaleCoeff
        {
            get => _scaleCoeff;
            set => SetField(ref _scaleCoeff, value);
        }
        /// <summary>
        /// Cached volume growth.
        /// Amount of space around the controller we cache to improve performance.
        /// This is a scale factor that should be higher than 1.0f but not too big, ideally lower than 2.0f.
        /// </summary>
        public float VolumeGrowth
        {
            get => _volumeGrowth;
            set => SetField(ref _volumeGrowth, value);
        }
        /// <summary>
        /// The non-walkable mode controls if a character controller slides or not on a non-walkable part.
        /// This is only used when slopeLimit is non zero.
        /// </summary>
        public bool SlideOnSteepSlopes
        {
            get => _slideOnSteepSlopes;
            set => SetField(ref _slideOnSteepSlopes, value);
        }
        public PhysxMaterial Material
        {
            get => _material;
            set => SetField(ref _material, value);
        }
        public float Radius
        {
            get => _radius;
            set => SetField(ref _radius, value);
        }
        public float StandingHeight
        {
            get => _standingHeight;
            set => SetField(ref _standingHeight, value);
        }
        public float ProneHeight
        {
            get => _proneHeight;
            set => SetField(ref _proneHeight, value);
        }
        public float CrouchedHeight
        {
            get => _crouchedHeight;
            set => SetField(ref _crouchedHeight, value);
        }

        public float CurrentHeight => Controller?.Height ?? GetCurrentHeight();

        private float GetCurrentHeight()
            => CrouchState switch
            {
                ECrouchState.Standing => StandingHeight,
                ECrouchState.Crouched => CrouchedHeight,
                ECrouchState.Prone => ProneHeight,
                _ => 0.0f,
            };

        public bool ConstrainedClimbing
        {
            get => _constrainedClimbing;
            set => SetField(ref _constrainedClimbing, value);
        }
        /// <summary>
        /// The minimum travelled distance to consider.
        /// If travelled distance is smaller, the character doesn’t move.
        /// This is used to stop the recursive motion algorithm when remaining distance to travel is small.
        /// </summary>
        public float MinMoveDistance
        {
            get => _minMoveDistance;
            set => SetField(ref _minMoveDistance, value);
        }
        public CapsuleController? Controller
        {
            get => _controller;
            private set => SetField(ref _controller, value);
        }
        public float JumpSpeed
        {
            get => _jumpSpeed;
            set => SetField(ref _jumpSpeed, value);
        }

        public PhysxDynamicRigidBody? RigidBody => Controller?.Actor;

        public void GetState(
            out Vector3 deltaXP,
            out PhysxShape? touchedShape,
            out PhysxRigidActor? touchedActor,
            out uint touchedObstacleHandle,
            out uint collisionFlags,
            out bool standOnAnotherCCT,
            out bool standOnObstacle,
            out bool isMovingUp)
        {
            if (Controller is null)
            {
                deltaXP = Vector3.Zero;
                touchedShape = null;
                touchedActor = null;
                touchedObstacleHandle = 0;
                collisionFlags = 0;
                standOnAnotherCCT = false;
                standOnObstacle = false;
                isMovingUp = false;
                return;
            }

            var state = Controller.State;
            deltaXP = state.deltaXP;
            touchedShape = state.touchedShape;
            touchedActor = state.touchedActor;
            touchedObstacleHandle = state.touchedObstacleHandle;
            collisionFlags = state.collisionFlags;
            standOnAnotherCCT = state.standOnAnotherCCT;
            standOnObstacle = state.standOnObstacle;
            isMovingUp = state.isMovingUp;
            return;
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(StandingHeight):
                    if (CrouchState == ECrouchState.Standing)
                        Controller?.Resize(StandingHeight);
                    break;
                case nameof(CrouchedHeight):
                    if (CrouchState == ECrouchState.Crouched)
                        Controller?.Resize(CrouchedHeight);
                    break;
                case nameof(ProneHeight):
                    if (CrouchState == ECrouchState.Prone)
                        Controller?.Resize(ProneHeight);
                    break;
                case nameof(CrouchState):
                    Controller?.Resize(GetCurrentHeight());
                    break;
                case nameof(Radius):
                    if (Controller is not null)
                        Controller.Radius = Radius;
                    break;
                case nameof(SlopeLimitCosine):
                    if (Controller is not null)
                        Controller.SlopeLimit = SlopeLimitCosine;
                    break;
                case nameof(StepOffset):
                    if (Controller is not null)
                        Controller.StepOffset = StepOffset;
                    break;
                case nameof(ContactOffset):
                    if (Controller is not null)
                        Controller.ContactOffset = ContactOffset;
                    break;
                case nameof(UpDirection):
                    if (Controller is not null)
                        Controller.UpDirection = UpDirection;
                    break;
                case nameof(SlideOnSteepSlopes):
                    if (Controller is not null)
                        Controller.ClimbingMode = ConstrainedClimbing 
                            ? PxCapsuleClimbingMode.Constrained
                            : PxCapsuleClimbingMode.Easy;
                    break;
            }
        }

        public Vector3 SpawnPosition
        {
            get => _spawnPosition;
            set => SetField(ref _spawnPosition, value);
        }

        protected internal unsafe override void OnComponentActivated()
        {
            _subUpdateTick = GroundMovementTick;
            RegisterTick(ETickGroup.Normal, (int)ETickOrder.Scene, MainUpdateTick);

            var scene = World?.PhysicsScene as PhysxScene;
            var manager = scene?.CreateOrCreateControllerManager();
            if (manager is null)
                return;

            Vector3 pos = SpawnPosition;
            Vector3 up = Globals.Up;
            Controller = manager.CreateCapsuleController(
                pos,
                up,
                SlopeLimitCosine,
                InvisibleWallHeight,
                MaxJumpHeight,
                ContactOffset,
                StepOffset,
                Density,
                ScaleCoeff,
                VolumeGrowth,
                SlideOnSteepSlopes 
                    ? PxControllerNonWalkableMode.PreventClimbingAndForceSliding
                    : PxControllerNonWalkableMode.PreventClimbing,
                Material,
                0,
                null,
                Radius,
                StandingHeight,
                ConstrainedClimbing 
                    ? PxCapsuleClimbingMode.Constrained
                    : PxCapsuleClimbingMode.Easy);

            //Wrap the hidden actor and apply to the transform
            //The constructor automatically caches the actor
            var rb = new PhysxDynamicRigidBody(Controller.ControllerPtr->GetActor());
            RigidBodyTransform.RigidBody = rb;
            //World.PhysicsScene.AddActor(rb);
        }

        protected internal override void OnComponentDeactivated()
        {
            _subUpdateTick = null;

            if (World is not null && RigidBody is not null)
                World.PhysicsScene.RemoveActor(RigidBody);
            RigidBodyTransform.RigidBody = null;

            Controller?.Release();
            Controller = null;
        }

        private unsafe void MainUpdateTick()
        {
            if (Controller is null)
                return;

            var scene = World?.PhysicsScene as PhysxScene;
            var manager = scene?.CreateOrCreateControllerManager();
            if (manager is null)
                return;

            var moveDelta = _subUpdateTick?.Invoke(ConsumeInput()) ?? Vector3.Zero;
            Controller.Move(moveDelta, MinMoveDistance, Engine.Delta, manager.ControllerFilters, null);
            if (Controller.CollidingDown)
            {
                if (_subUpdateTick == AirMovementTick)
                    _subUpdateTick = GroundMovementTick;
            }
            else
            {
                if (_subUpdateTick == GroundMovementTick)
                    _subUpdateTick = AirMovementTick;
            }
            LastVelocity = Velocity;
            Velocity = RigidBodyTransform.RigidBody?.LinearVelocity ?? Vector3.Zero;
            Acceleration = (Velocity - LastVelocity) / Engine.Delta;
        }

        private Vector3 _acceleration;
        public Vector3 Acceleration
        {
            get => _acceleration;
            private set => SetField(ref _acceleration, value);
        }

        private Vector3 _lastVelocity;
        public Vector3 LastVelocity
        {
            get => _lastVelocity;
            private set => SetField(ref _lastVelocity, value);
        }

        public Vector3 Velocity
        {
            get => _velocity;
            set => SetField(ref _velocity, value);
        }

        public float Friction
        {
            get => Material.DynamicFriction;
            set => Material.DynamicFriction = value;
        }

        //public float GroundFriction
        //{
        //    get => Material.StaticFriction;
        //    set => Material.StaticFriction = value;
        //}

        // TODO: calculate friction based on this character's material and the current surface
        private float _walkingFriction = 0.5f;
        public float WalkingFriction
        {
            get => _walkingFriction;
            set => SetField(ref _walkingFriction, value.Clamp(0.0f, 1.0f));
        }

        public Vector3? GravityOverride
        {
            get => _gravityOverride;
            set => SetField(ref _gravityOverride, value);
        }

        public void AddForce(Vector3 force)
        {
            //Calculate acceleration from force
            float mass = RigidBody?.Mass ?? 0.0f;
            if (mass > 0.0f)
                Velocity += force / mass;
        }

        private bool _isJumping = false;
        protected virtual unsafe Vector3 GroundMovementTick(Vector3 movementInput)
        {
            if (Controller is null || World?.PhysicsScene is not PhysxScene scene)
                return Vector3.Zero;

            Vector3 delta = Velocity * Engine.Delta * (1.0f - WalkingFriction);
            delta += movementInput * WalkingMovementSpeed;
            delta += (GravityOverride ?? scene.Gravity) * Engine.Delta;
            if (_isJumping)
            {
                _isJumping = false;
                delta.Y = JumpSpeed;
            }

            return delta;
        }

        protected virtual unsafe Vector3 AirMovementTick(Vector3 movementInput)
        {
            if (Controller is null || World?.PhysicsScene is not PhysxScene scene)
                return Vector3.Zero;

            Vector3 delta = Velocity * Engine.Delta; //No friction in the air
            delta.X += movementInput.X * AirMovementAcceleration * Engine.Delta;
            delta.Z += movementInput.Z * AirMovementAcceleration * Engine.Delta;
            delta += (GravityOverride ?? scene.Gravity) * Engine.Delta;

            return delta;
        }

        public void Jump(bool pressed)
            => _isJumping = pressed;
    }
}
