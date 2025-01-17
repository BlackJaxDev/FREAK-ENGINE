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
    public class CharacterComponent : PawnComponent
    {
        private CharacterMovement3DComponent Movement => GetSiblingComponent<CharacterMovement3DComponent>(true)!;
        
        //private readonly GameTimer _respawnTimer = new();
        private Rotator _viewRotation = Rotator.GetZero(ERotationOrder.YPR);
        private float _gamePadMovementInputMultiplier = 200.0f;
        private float _keyboardMovementInputMultiplier = 200.0f;
        private float _mouseXLookInputMultiplier = 0.3f;
        private float _mouseYLookInputMultiplier = 0.3f;
        private float _gamePadXLookInputMultiplier = 2.0f;
        private float _gamePadYLookInputMultiplier = 2.0f;

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

        //public virtual void Kill(PawnComponent instigator, PawnComponent killer)
        //{

        //}

        //public void QueueRespawn(float respawnTimeInSeconds = 0)
        //    => _respawnTimer.StartSingleFire(WantsRespawn, respawnTimeInSeconds);
        //protected virtual void WantsRespawn()
        //    => _respawnTimer.StartMultiFire(AttemptSpawn, 0.1f);
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
        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            RegisterTick(ETickGroup.Normal, ETickOrder.Logic, TickMovementInput);
        }
        protected virtual void TickMovementInput()
        {
            var cam = GetCamera();

            Vector3 forward, right;
            if (cam is not null)
            {
                forward = cam.Transform.WorldForward;
                float dot = forward.Dot(Globals.Up);
                if (Math.Abs(dot) >= 0.99f)
                {
                    //if dot is 1, looking straight up. need to use camera down for forward
                    //if dot is -1, looking straight down. need to use camera up for forward
                    forward = dot > 0.0f
                        ? -cam.Transform.WorldUp
                        : cam.Transform.WorldUp;
                }
                forward.Y = 0.0f;
                forward = Vector3.Normalize(forward);
                right = cam.Transform.WorldRight;
            }
            else
            {
                forward = Transform.WorldForward;
                right = Transform.WorldRight;
            }

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

            //if (gamepadMovement || keyboardMovement)
            //    _meshComp.Rotation.Yaw = _movement.TargetFrameInputDirection.LookatAngles().Yaw + 180.0f;

            if (cam is not null)
                cam.SceneNode.GetTransformAs<Transform>(true)!.Rotator = _viewRotation;
        }
        public override void RegisterInput(InputInterface input)
        {
            input.HideCursor = !input.Unregister;

            input.RegisterMouseMove(Look, EMouseMoveType.Relative);

            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickX, MoveRight, true);
            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickY, MoveForward, true);

            input.RegisterAxisUpdate(EGamePadAxis.RightThumbstickX, LookRight, true);
            input.RegisterAxisUpdate(EGamePadAxis.RightThumbstickY, LookUp, true);

            input.RegisterButtonEvent(EGamePadButton.FaceDown, EButtonInputType.Pressed, Jump);

            input.RegisterKeyStateChange(EKey.W, MoveForward);
            input.RegisterKeyStateChange(EKey.A, MoveLeft);
            input.RegisterKeyStateChange(EKey.S, MoveBackward);
            input.RegisterKeyStateChange(EKey.D, MoveRight);

            input.RegisterKeyEvent(EKey.Space, EButtonInputType.Pressed, Jump);
            input.RegisterKeyEvent(EKey.C, EButtonInputType.Pressed, ToggleCrouch);
            input.RegisterKeyEvent(EKey.Z, EButtonInputType.Pressed, ToggleProne);

            input.RegisterKeyEvent(EKey.Escape, EButtonInputType.Pressed, Quit);
            input.RegisterKeyEvent(EKey.Backspace, EButtonInputType.Pressed, ToggleMouseCapture);
        }

        private void ToggleMouseCapture()
        {
            if (LocalInput is null)
                return;

            LocalInput.HideCursor = !LocalInput.HideCursor;
        }

        private void Quit()
            => Engine.ShutDown();

        protected virtual void MoveForward(bool pressed)
            => _keyboardMovementInput.Y += pressed ? 1.0f : -1.0f;
        protected virtual void MoveLeft(bool pressed)
            => _keyboardMovementInput.X += pressed ? -1.0f : 1.0f;
        protected virtual void MoveRight(bool pressed)
            => _keyboardMovementInput.X += pressed ? 1.0f : -1.0f;
        protected virtual void MoveBackward(bool pressed)
            => _keyboardMovementInput.Y += pressed ? -1.0f : 1.0f;

        protected virtual void Jump()
            => Movement.Jump();

        protected virtual void ToggleCrouch()
            => Movement.CrouchState = Movement.CrouchState == CharacterMovement3DComponent.ECrouchState.Crouched 
                ? CharacterMovement3DComponent.ECrouchState.Standing
                : CharacterMovement3DComponent.ECrouchState.Crouched;

        protected virtual void ToggleProne()
            => Movement.CrouchState = Movement.CrouchState == CharacterMovement3DComponent.ECrouchState.Prone
                ? CharacterMovement3DComponent.ECrouchState.Standing
                : CharacterMovement3DComponent.ECrouchState.Prone;

        protected virtual void MoveRight(float value)
            => _gamepadMovementInput.X = value;
        protected virtual void MoveForward(float value)
            => _gamepadMovementInput.Y = value;

        protected virtual void Look(float x, float y)
        {
            _viewRotation.Pitch += y * MouseYLookInputMultiplier;
            _viewRotation.Yaw -= x * MouseXLookInputMultiplier;
            ClampPitch();
            RemapYaw();
        }
        protected virtual void LookRight(float value)
        {
            _viewRotation.Yaw -= value * GamePadXLookInputMultiplier;
            RemapYaw();
        }
        protected virtual void LookUp(float value)
        {
            _viewRotation.Pitch += value * GamePadYLookInputMultiplier;
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
    }
}
