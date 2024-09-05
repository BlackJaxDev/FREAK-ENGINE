using System.Numerics;
using XREngine.Components.Scene.Transforms;
using XREngine.Core.Attributes;
using XREngine.Data.Components;
using XREngine.Data.Transforms.Rotations;
using XREngine.Input.Devices;
using XREngine.Physics;
using XREngine.Scene.Transforms;
using XREngine.Timers;

namespace XREngine.Components
{
    /// <summary>
    /// Use this character pawn type to specify your own derivation of the CharacterMovementComponent.
    /// </summary>
    /// <typeparam name="MovementClass"></typeparam>
    [RequireComponents(typeof(CapsuleYComponent), typeof(CharacterMovement3DComponent))]
    public class CharacterPawnComponent : PawnComponent
    {
        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(FirstPerson):
                    CurrentCameraComponent = FirstPerson ? _fpCameraComponent : _tpCameraComponent;
                    break;
            }
        }
        
        //Component references
        //protected SkeletalMeshComponent? _meshComp;
        protected CharacterMovement3DComponent? _movement;
        //protected AnimStateMachineComponent _animationStateMachine;
        protected CameraComponent _fpCameraComponent, _tpCameraComponent;

        protected BoomTransform _tpCameraBoom;

        private GameTimerComponent _respawnTimer = new();
        private bool _firstPerson = false;
        private Rotator _viewRotation = Rotator.GetZero(ERotationOrder.YPR);
        float _gamePadMovementInputMultiplier = 51.0f;
        float _keyboardMovementInputMultiplier = 51.0f;
        float _mouseXLookInputMultiplier = 0.5f;
        float _mouseYLookInputMultiplier = 0.5f;
        float _gamePadXLookInputMultiplier = 1.0f;
        float _gamePadYLookInputMultiplier = 1.0f;

        protected Vector2 _keyboardMovementInput = Vector2.Zero;
        protected Vector2 _gamepadMovementInput = Vector2.Zero;

        public bool FirstPerson
        {
            get => _firstPerson;
            set => SetField(ref _firstPerson, value);
        }
        public float KeyboardMovementInputMultiplier
        {
            get => _keyboardMovementInputMultiplier;
            set => SetField(ref _keyboardMovementInputMultiplier, value);
        }
        public float GamePadMovementInputMultiplier
        {
            get => _gamePadMovementInputMultiplier;
            set => SetField(ref _gamePadMovementInputMultiplier, value);
        }
        public float MouseXLookInputMultiplier
        {
            get => _mouseXLookInputMultiplier;
            set => SetField(ref _mouseXLookInputMultiplier, value);
        }
        public float MouseYLookInputMultiplier
        {
            get => _mouseYLookInputMultiplier;
            set => SetField(ref _mouseYLookInputMultiplier, value);
        }
        public float GamePadXLookInputMultiplier
        {
            get => _gamePadXLookInputMultiplier;
            set => SetField(ref _gamePadXLookInputMultiplier, value);
        }
        public float GamePadYLookInputMultiplier
        {
            get => _gamePadYLookInputMultiplier;
            set => SetField(ref _gamePadYLookInputMultiplier, value);
        }

        public virtual void Kill(CharacterPawnComponent instigator, PawnComponent killer)
        {

        }

        public void QueueRespawn(float respawnTimeInSeconds = 0)
            => _respawnTimer.StartSingleFire(WantsRespawn, respawnTimeInSeconds);
        protected virtual void WantsRespawn()
            => _respawnTimer.StartMultiFire(AttemptSpawn, 0.1f);
        private void AttemptSpawn(float totalElapsed, int fireNumber)
        {
            //ICharacterGameMode mode = World?.GameMode as ICharacterGameMode;
            //if (!mode.FindSpawnPoint(Controller, out Matrix4 transform))
            //    return;

            //_respawnTimer.Stop();

            //if (IsSpawned)
            //    Engine.World.DespawnActor(this);

            //RootComponent.WorldMatrix.Value = transform;
            //Engine.World.SpawnActor(this);
        }
        //protected override void OnSpawnedPostComponentSpawn()
        //{
        //    RegisterTick(ETickGroup.PrePhysics, (int)ETickOrder.Logic, TickMovementInput);
        //    //RootComponent.PhysicsDriver.SimulatingPhysics = true;
        //}
        //protected override void OnDespawned()
        //{
        //    UnregisterTick(ETickGroup.PrePhysics, (int)ETickOrder.Logic, TickMovementInput);
        //    base.OnDespawned();
        //}
        protected virtual void TickMovementInput()
        {
            Vector3 forward = _tpCameraBoom.WorldForward;
            Vector3 right = Vector3.Cross(forward, Globals.Up);

            bool keyboardMovement = _keyboardMovementInput.X != 0.0f || _keyboardMovementInput.Y != 0.0f;
            bool gamepadMovement = _gamepadMovementInput.X != 0.0f || _gamepadMovementInput.Y != 0.0f;

            Vector3 input;
            if (keyboardMovement)
            {
                input = forward * _keyboardMovementInput.Y + right * _keyboardMovementInput.X;
                input = Vector3.Normalize(input);
                _movement.AddMovementInput(input * Engine.Delta * KeyboardMovementInputMultiplier);
            }
            if (gamepadMovement)
            {
                input = forward * _gamepadMovementInput.Y + right * _gamepadMovementInput.X;
                _movement.AddMovementInput(input * Engine.Delta * GamePadMovementInputMultiplier);
            }
            //if (gamepadMovement || keyboardMovement)
            //    _meshComp.Rotation.Yaw = _movement.TargetFrameInputDirection.LookatAngles().Yaw + 180.0f;
        }
        public override void RegisterInput(InputInterface input)
        {
            //input.Mouse.WrapCursorWithinClip = input.IsRegistering;
            input.RegisterMouseMove(Look, EMouseMoveType.Relative);

            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickX, MoveRight, true);
            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickY, MoveForward, true);

            input.RegisterAxisUpdate(EGamePadAxis.RightThumbstickX, LookRight, true);
            input.RegisterAxisUpdate(EGamePadAxis.RightThumbstickY, LookUp, true);

            input.RegisterButtonEvent(EGamePadButton.FaceDown, EButtonInputType.Pressed, Jump);

            input.RegisterKeyContinuousState(EKey.W, MoveForward);
            input.RegisterKeyContinuousState(EKey.A, MoveLeft);
            input.RegisterKeyContinuousState(EKey.S, MoveBackward);
            input.RegisterKeyContinuousState(EKey.D, MoveRight);

            input.RegisterKeyEvent(EKey.Space, EButtonInputType.Pressed, Jump);
        }

        protected virtual void MoveForward(bool pressed)
            => _keyboardMovementInput.Y += pressed ? 1.0f : -1.0f;
        protected virtual void MoveLeft(bool pressed)
            => _keyboardMovementInput.X += pressed ? -1.0f : 1.0f;
        protected virtual void MoveRight(bool pressed)
            => _keyboardMovementInput.X += pressed ? 1.0f : -1.0f;
        protected virtual void MoveBackward(bool pressed)
            => _keyboardMovementInput.Y += pressed ? -1.0f : 1.0f;

        protected virtual void Jump()
            => _movement.Jump();
        protected virtual void MoveRight(float value)
            => _gamepadMovementInput.X = value;
        protected virtual void MoveForward(float value)
            => _gamepadMovementInput.Y = value;

        protected virtual void Look(float x, float y)
        {
            _viewRotation.Pitch += y * MouseYLookInputMultiplier;
            _viewRotation.Yaw -= x * MouseXLookInputMultiplier;

            //float yaw = _viewRotation.Yaw.RemapToRange(0.0f, 360.0f);
            //if (yaw < 45.0f || yaw >= 315.0f)
            //{
            //    _meshComp.Rotation.Yaw = 180.0f;
            //}
            //else if (yaw < 135.0f)
            //{
            //    _meshComp.Rotation.Yaw = 270.0f;
            //}
            //else if (yaw < 225.0f)
            //{
            //    _meshComp.Rotation.Yaw = 0.0f;
            //}
            //else if (yaw < 315.0f)
            //{
            //    _meshComp.Rotation.Yaw = 90.0f;
            //}

            //_fpCameraComponent.Camera.AddRotation(y, 0.0f);
        }

        protected virtual void LookRight(float value)
            => _viewRotation.Yaw -= value * GamePadXLookInputMultiplier;

        protected virtual void LookUp(float value)
            => _viewRotation.Pitch += value * GamePadYLookInputMultiplier;

        //protected override void PreConstruct()
        //{
        //    _animationStateMachine = new AnimStateMachineComponent();
        //    LogicComponents.Clear();
        //    LogicComponents.Add(_movement);
        //    LogicComponents.Add(_animationStateMachine);
        //}

        protected internal override void Start()
        {
            base.Start();

            _movement ??= GetSiblingComponent<CharacterMovement3DComponent>();

            //5'8" in m = 1.72f
            float characterHeight = new FeetInches(5, 8.0f).ToMeters();

            float radius = 0.172f;
            float capsuleTotalHalfHeight = characterHeight / 2.0f;
            float halfHeight = capsuleTotalHalfHeight - radius;

            RigidBodyConstructionInfo info = new()
            {
                Mass = 59.0f,
                AdditionalDamping = false,
                AngularDamping = 0.0f,
                LinearDamping = 0.0f,
                Restitution = 0.0f,
                Friction = 1.0f,
                RollingFriction = 0.01f,
                CollisionEnabled = true,
                SimulatePhysics = true,
                SleepingEnabled = false,
                CollisionGroup = (ushort)Physics.ECollisionGroup.Characters,
                CollidesWith = (ushort)(Physics.ECollisionGroup.StaticWorld | Physics.ECollisionGroup.DynamicWorld),
            };

            CapsuleYComponent rootCapsule = new(radius, halfHeight, info);
            Physics.XRRigidBody body = rootCapsule.CollisionObject as Physics.XRRigidBody;
            body.Collided += RigidBodyCollision_Collided;
            body.AngularFactor = Vector3.Zero;

            if (rootCapsule.TransformIs<Transform>(out var tfm))
                tfm!.Translation = new Vector3(0.0f, capsuleTotalHalfHeight + 11.0f, 0.0f);

            //PerspectiveCamera FPCam = new PerspectiveCamera()
            //{
            //    VerticalFieldOfView = 30.0f,
            //    FarZ = 50.0f
            //};
            //FPCam.LocalRotation.SyncFrom(_viewRotation);
            //_fpCameraComponent = new CameraComponent(FPCam);
            //_fpCameraComponent.AttachTo(_meshComp, "Head");

            //WorldTranslationLaggedTransform lagComp = new(50.0f, 0.0f);
            //rootCapsule.ChildSockets.Add(lagComp);

            //_tpCameraBoom = new BoomComponent() { IgnoreCast = rootCapsule.CollisionObject };
            //_tpCameraBoom.Translation = new Vector3(0.0f, 0.3f, 0.0f);
            ////_tpCameraBoom.Rotation.SyncFrom(_viewRotation);
            //_tpCameraBoom.MaxLength = 5.0f;
            //lagComp.ChildSockets.Add(_tpCameraBoom);

            //PerspectiveCamera TPCam = new PerspectiveCamera()
            //{
            //    NearZ = 0.1f,
            //    HorizontalFieldOfView = 90.0f,
            //    //FarZ = 100.0f
            //};

            //_tpCameraComponent = new CameraComponent(TPCam);
            //_tpCameraBoom.ChildSockets.Add(_tpCameraComponent);

            //CurrentCameraComponent = _tpCameraComponent;

            //_viewRotation.Yaw = 180.0f;
        }

        private void RigidBodyCollision_Collided(XRCollisionObject @this, XRCollisionObject other, XRContactInfo info, bool thisIsA)
        {
            //Engine.DebugPrint(((ObjectBase)other).Name + " collided with " + Name);
            _movement.OnHit(other, info, thisIsA);
        }
    }
}
