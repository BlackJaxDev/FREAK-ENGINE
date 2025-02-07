using Extensions;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Transforms.Rotations;
using XREngine.Input.Devices;
using XREngine.Scene.Transforms;

namespace XREngine.Components
{
    /// <summary>
    /// Pawn used for moveable player characters.
    /// Converts inputs into kinematic rigid body movements and provides inputs for a camera view.
    /// </summary>
    [RequireComponents(typeof(CharacterMovement3DComponent))]
    public class CharacterPawnComponent : PawnComponent
    {
        private CharacterMovement3DComponent Movement => GetSiblingComponent<CharacterMovement3DComponent>(true)!;
        
        //private readonly GameTimer _respawnTimer = new();
        private Rotator _viewRotation = Rotator.GetZero(ERotationOrder.YPR);
        private float _gamePadMovementInputMultiplier = 200.0f;
        private float _keyboardMovementInputMultiplier = 200.0f;
        private float _keyboardLookXInputMultiplier = 200.0f;
        private float _keyboardLookYInputMultiplier = 200.0f;
        private float _mouseXLookInputMultiplier = 10.0f;
        private float _mouseYLookInputMultiplier = 10.0f;
        private float _gamePadXLookInputMultiplier = 200.0f;
        private float _gamePadYLookInputMultiplier = 200.0f;

        //5'8" in m = 1.72f
        //characterHeight = new FeetInches(5, 8.0f).ToMeters();

        private float _characterHeightMeters = 1.72f;
        public float CharacterHeightMeters
        {
            get => _characterHeightMeters;
            set => SetField(ref _characterHeightMeters, value);
        }

        private float _characterWidthMeters = 0.344f;
        public float CharacterWidthMeters
        {
            get => _characterWidthMeters;
            set => SetField(ref _characterWidthMeters, value);
        }

        protected Vector2 _keyboardMovementInput = Vector2.Zero;
        protected Vector2 _gamepadMovementInput = Vector2.Zero;
        protected Vector2 _keyboardLookInput = Vector2.Zero;

        public float KeyboardMovementInputMultiplier
        {
            get => _keyboardMovementInputMultiplier;
            set => SetField(ref _keyboardMovementInputMultiplier, value);
        }
        public float KeyboardLookXInputMultiplier
        {
            get => _keyboardLookXInputMultiplier;
            set => SetField(ref _keyboardLookXInputMultiplier, value);
        }
        public float KeyboardLookYInputMultiplier
        {
            get => _keyboardLookYInputMultiplier;
            set => SetField(ref _keyboardLookYInputMultiplier, value);
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

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            RegisterTick(ETickGroup.Normal, ETickOrder.Logic, TickMovementInput);
        }

        private TransformBase? _viewTransform;
        public TransformBase? ViewTransform
        {
            get => _viewTransform;
            set => SetField(ref _viewTransform, value);
        }

        private Transform? _rotationTransform;
        public Transform? RotationTransform
        {
            get => _rotationTransform;
            set => SetField(ref _rotationTransform, value);
        }

        private bool _ignoreViewTransformPitch = false;
        public bool IgnoreViewTransformPitch
        {
            get => _ignoreViewTransformPitch;
            set => SetField(ref _ignoreViewTransformPitch, value);
        }

        private bool _ignoreViewTransformYaw = false;
        public bool IgnoreViewTransformYaw
        {
            get => _ignoreViewTransformYaw;
            set => SetField(ref _ignoreViewTransformYaw, value);
        }

        protected virtual void TickMovementInput()
        {
            var cam = GetCamera();
            var viewTransform = ViewTransform ?? cam?.Transform ?? Transform;

            GetDirectionalVectorsFromView(viewTransform, out Vector3 forward, out Vector3 right);

            bool keyboardMovement = _keyboardMovementInput.X != 0.0f || _keyboardMovementInput.Y != 0.0f;
            bool gamepadMovement = _gamepadMovementInput.X != 0.0f || _gamepadMovementInput.Y != 0.0f;

            if (keyboardMovement)
            {
                Vector3 input = forward * _keyboardMovementInput.Y + right * _keyboardMovementInput.X;
                Movement.AddMovementInput(Engine.Delta * KeyboardMovementInputMultiplier * Vector3.Normalize(input));
            }

            if (gamepadMovement)
            {
                Vector3 input = forward * _gamepadMovementInput.Y + right * _gamepadMovementInput.X;
                Movement.AddMovementInput(Engine.Delta * GamePadMovementInputMultiplier * Vector3.Normalize(input));
            }

            if (_keyboardLookInput != Vector2.Zero)
                KeyboardLook(_keyboardLookInput.X, _keyboardLookInput.Y);

            var rotTfm = RotationTransform ?? cam?.SceneNode.GetTransformAs<Transform>(false);
            if (rotTfm != null)
            {
                if (_ignoreViewTransformPitch)
                    _viewRotation.Pitch = 0.0f;
                if (_ignoreViewTransformYaw)
                    _viewRotation.Yaw = 0.0f;
                rotTfm.Rotator = _viewRotation;
            }
        }

        private static void GetDirectionalVectorsFromView(TransformBase viewTransform, out Vector3 forward, out Vector3 right)
        {
            forward = viewTransform.WorldForward;
            float dot = forward.Dot(Globals.Up);
            if (Math.Abs(dot) >= 0.99f)
            {
                //if dot is 1, looking straight up. need to use camera down for forward
                //if dot is -1, looking straight down. need to use camera up for forward
                forward = dot > 0.0f
                    ? -viewTransform.WorldUp
                    : viewTransform.WorldUp;
            }
            forward.Y = 0.0f;
            forward = Vector3.Normalize(forward);
            right = viewTransform.WorldRight;
        }

        public override void RegisterInput(InputInterface input)
        {
            input.HideCursor = !input.Unregister;

            input.RegisterMouseMove(MouseLook, EMouseMoveType.Relative);

            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickX, MoveRight, true);
            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickY, MoveForward, true);

            input.RegisterAxisUpdate(EGamePadAxis.RightThumbstickX, LookRight, true);
            input.RegisterAxisUpdate(EGamePadAxis.RightThumbstickY, LookUp, true);

            input.RegisterButtonEvent(EGamePadButton.FaceDown, EButtonInputType.Pressed, Jump);

            input.RegisterKeyStateChange(EKey.W, MoveForward);
            input.RegisterKeyStateChange(EKey.A, MoveLeft);
            input.RegisterKeyStateChange(EKey.S, MoveBackward);
            input.RegisterKeyStateChange(EKey.D, MoveRight);

            input.RegisterKeyStateChange(EKey.Right, LookRight);
            input.RegisterKeyStateChange(EKey.Left, LookLeft);
            input.RegisterKeyStateChange(EKey.Up, LookUp);
            input.RegisterKeyStateChange(EKey.Down, LookDown);

            input.RegisterKeyEvent(EKey.Space, EButtonInputType.Pressed, Jump);
            input.RegisterKeyEvent(EKey.C, EButtonInputType.Pressed, ToggleCrouch);
            input.RegisterKeyEvent(EKey.Z, EButtonInputType.Pressed, ToggleProne);

            input.RegisterKeyEvent(EKey.Escape, EButtonInputType.Pressed, Quit);
            input.RegisterKeyEvent(EKey.Backspace, EButtonInputType.Pressed, ToggleMouseCapture);

            input.RegisterVRBoolAction(EVRActionCategory.Global, EVRGameAction.Jump, Jump);
            input.RegisterVRVector2Action(EVRActionCategory.Global, EVRGameAction.Locomote, Locomote);
            input.RegisterVRVector2Action(EVRActionCategory.Global, EVRGameAction.Turn, Turn);
        }

        private void Turn(Vector2 oldValue, Vector2 newValue)
        {
            LookRight(newValue.X);
        }

        private void Locomote(Vector2 oldValue, Vector2 newValue)
        {
            MoveRight(newValue.X);
            MoveForward(newValue.Y);
        }

        public enum EVRActionCategory
        {
            /// <summary>
            /// Global actions are always available.
            /// </summary>
            Global,
            /// <summary>
            /// Actions that are only available when one controller is off.
            /// </summary>
            OneHanded,
            /// <summary>
            /// Actions that are enabled when the quick menu (the menu on the controller) is open.
            /// </summary>
            QuickMenu,
            /// <summary>
            /// Actions that are enabled when the main menu is fully open.
            /// </summary>
            Menu,
            /// <summary>
            /// Actions that are enabled when the avatar's menu is open.
            /// </summary>
            AvatarMenu,
        }
        public enum EVRGameAction
        {
            Interact,
            Jump,
            ToggleMute,
            Grab,
            PlayspaceDragLeft,
            PlayspaceDragRight,
            ToggleQuickMenu,
            ToggleMenu,
            ToggleAvatarMenu,
            LeftHandPose,
            RightHandPose,
            Locomote,
            Turn,
        }
        private void ToggleMouseCapture()
        {
            if (LocalInput is null)
                return;

            LocalInput.HideCursor = !LocalInput.HideCursor;
        }

        protected virtual void Quit()
            => Engine.ShutDown();

        protected virtual void Jump()
            => Movement.Jump();

        private void Jump(bool pressed)
        {
            if (pressed)
                Jump();
        }

        protected virtual void ToggleCrouch()
            => Movement.CrouchState = Movement.CrouchState == CharacterMovement3DComponent.ECrouchState.Crouched
                ? CharacterMovement3DComponent.ECrouchState.Standing
                : CharacterMovement3DComponent.ECrouchState.Crouched;

        protected virtual void ToggleProne()
            => Movement.CrouchState = Movement.CrouchState == CharacterMovement3DComponent.ECrouchState.Prone
                ? CharacterMovement3DComponent.ECrouchState.Standing
                : CharacterMovement3DComponent.ECrouchState.Prone;

        private void LookLeft(bool pressed)
            => _keyboardLookInput.X += pressed ? -1.0f : 1.0f;
        private void LookRight(bool pressed)
            => _keyboardLookInput.X += pressed ? 1.0f : -1.0f;
        private void LookDown(bool pressed)
            => _keyboardLookInput.Y += pressed ? -1.0f : 1.0f;
        private void LookUp(bool pressed)
            => _keyboardLookInput.Y += pressed ? 1.0f : -1.0f;

        protected virtual void MoveForward(bool pressed)
            => _keyboardMovementInput.Y += pressed ? 1.0f : -1.0f;
        protected virtual void MoveLeft(bool pressed)
            => _keyboardMovementInput.X += pressed ? -1.0f : 1.0f;
        protected virtual void MoveRight(bool pressed)
            => _keyboardMovementInput.X += pressed ? 1.0f : -1.0f;
        protected virtual void MoveBackward(bool pressed)
            => _keyboardMovementInput.Y += pressed ? -1.0f : 1.0f;

        protected virtual void MoveRight(float value)
            => _gamepadMovementInput.X = value;
        protected virtual void MoveForward(float value)
            => _gamepadMovementInput.Y = value;

        protected virtual void MouseLook(float x, float y)
        {
            _viewRotation.Pitch += Engine.Delta * y * MouseYLookInputMultiplier;
            _viewRotation.Yaw -= Engine.Delta * x * MouseXLookInputMultiplier;
            ClampPitch();
            RemapYaw();
        }
        protected virtual void KeyboardLook(float x, float y)
        {
            _viewRotation.Pitch += Engine.Delta * y * KeyboardLookYInputMultiplier;
            _viewRotation.Yaw -= Engine.Delta * x * KeyboardLookXInputMultiplier;
            ClampPitch();
            RemapYaw();
        }

        protected virtual void LookRight(float value)
        {
            _viewRotation.Yaw -= Engine.Delta * value * GamePadXLookInputMultiplier;
            RemapYaw();
        }
        protected virtual void LookUp(float value)
        {
            _viewRotation.Pitch += Engine.Delta * value * GamePadYLookInputMultiplier;
            ClampPitch();
        }

        private void ClampPitch()
        {
            if (_viewRotation.Pitch > 89.0f)
                _viewRotation.Pitch = 89.0f;
            else if (_viewRotation.Pitch < -89.0f)
                _viewRotation.Pitch = -89.0f;
        }
        private void RemapYaw()
        {
            //don't let the yaw or pitch exceed 180 or -180
            if (_viewRotation.Yaw > 180.0f)
                _viewRotation.Yaw -= 360.0f;
            else if (_viewRotation.Yaw < -180.0f)
                _viewRotation.Yaw += 360.0f;
        }

        //public virtual void Kill(PawnComponent instigator, PawnComponent killer) { }
        //public void QueueRespawn(float respawnTimeInSeconds = 0)
        //    => _respawnTimer.StartSingleFire(WantsRespawn, respawnTimeInSeconds);
        //protected virtual void WantsRespawn()
        //    => _respawnTimer.StartMultiFire(AttemptSpawn, 0.1f);

        //private void AttemptSpawn(float totalElapsed, int fireNumber)
        //{
        //    ICharacterGameMode mode = World?.GameMode as ICharacterGameMode;
        //    if (!mode.FindSpawnPoint(Controller, out Matrix4 transform))
        //        return;

        //    _respawnTimer.Stop();

        //    if (IsSpawned)
        //        Engine.World.DespawnActor(this);

        //    RootComponent.WorldMatrix.Value = transform;
        //    World.SpawnActor(this);
        //}
    }
}
