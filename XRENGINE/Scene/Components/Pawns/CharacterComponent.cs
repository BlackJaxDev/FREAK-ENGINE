using System.Numerics;
using XREngine.Components.Scene.Transforms;
using XREngine.Core.Attributes;
using XREngine.Data.Components;
using XREngine.Data.Transforms.Rotations;
using XREngine.Input.Devices;
using XREngine.Physics;
using XREngine.Scene;
using XREngine.Scene.Transforms;
using XREngine.Timers;

namespace XREngine.Components
{
    [RequireComponents(typeof(CharacterMovement3DComponent), typeof(CapsuleYComponent))]
    public class CharacterComponent : PawnComponent
    {
        public CapsuleYComponent? RootCapsule => GetSiblingComponent<CapsuleYComponent>(true);
        private CharacterMovement3DComponent Movement => GetSiblingComponent<CharacterMovement3DComponent>(true)!;
        public SceneNode? ViewSceneNode { get; set; }
        
        private readonly GameTimerComponent _respawnTimer = new();
        private Rotator _viewRotation = Rotator.GetZero(ERotationOrder.YPR);
        private float _gamePadMovementInputMultiplier = 51.0f;
        private float _keyboardMovementInputMultiplier = 51.0f;
        private float _mouseXLookInputMultiplier = 0.5f;
        private float _mouseYLookInputMultiplier = 0.5f;
        private float _gamePadXLookInputMultiplier = 1.0f;
        private float _gamePadYLookInputMultiplier = 1.0f;

        //5'8" in m = 1.72f
        //characterHeight = new FeetInches(5, 8.0f).ToMeters();
        public float CharacterHeightMeters { get; set; } = 1.72f;
        public float CharacterWidthMeters { get; set; } = 0.344f;

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

        public virtual void Kill(PawnComponent instigator, PawnComponent killer)
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
            if (ViewSceneNode is null)
                return;

            Vector3 forward = ViewSceneNode.Transform.WorldForward;
            Vector3 right = Vector3.Cross(forward, Globals.Up);

            bool keyboardMovement = _keyboardMovementInput.X != 0.0f || _keyboardMovementInput.Y != 0.0f;
            bool gamepadMovement = _gamepadMovementInput.X != 0.0f || _gamepadMovementInput.Y != 0.0f;

            Vector3 input;
            if (keyboardMovement)
            {
                input = forward * _keyboardMovementInput.Y + right * _keyboardMovementInput.X;
                input = Vector3.Normalize(input);
                Movement.AddMovementInput(input * Engine.UndilatedDelta * KeyboardMovementInputMultiplier);
            }
            if (gamepadMovement)
            {
                input = forward * _gamepadMovementInput.Y + right * _gamepadMovementInput.X;
                Movement.AddMovementInput(input * Engine.UndilatedDelta * GamePadMovementInputMultiplier);
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
            => Movement.Jump();
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

        private void RigidBodyCollision_Collided(XRCollisionObject @this, XRCollisionObject other, XRContactInfo info, bool thisIsA)
            => Movement.OnHit(other, info, thisIsA);

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();

            float radius = CharacterWidthMeters / 2.0f;
            float capsuleTotalHalfHeight = CharacterHeightMeters / 2.0f;
            float halfHeight = capsuleTotalHalfHeight - CharacterWidthMeters; //subtract radius from the top and bottom, aka just the width (diameter)

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
                CollisionGroup = (ushort)ECollisionGroup.Characters,
                CollidesWith = (ushort)(ECollisionGroup.StaticWorld | ECollisionGroup.DynamicWorld),
            };

            CapsuleYComponent rootCapsule = new(radius, halfHeight, info);
            XRRigidBody? body = rootCapsule.CollisionObject as XRRigidBody;
            if (body is not null)
            {
                body.Collided += RigidBodyCollision_Collided;
                body.AngularFactor = Vector3.Zero;
            }

            if (rootCapsule.TransformIs<Transform>(out var tfm))
                tfm!.Translation = new Vector3(0.0f, capsuleTotalHalfHeight + 10.0f, 0.0f);
        }
    }
}
