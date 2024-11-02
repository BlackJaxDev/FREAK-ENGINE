using XREngine.Data.Core;
using XREngine.Input;
using XREngine.Input.Devices;
using XREngine.Rendering;

namespace XREngine.Components
{
    /// <summary>
    /// A pawn is an actor that can be controlled by either a local player, remote player, or AI.
    /// This serves as only way for a player to perceive and interact with the game world.
    /// </summary>
    public class PawnComponent : XRComponent
    {
        public XREvent<PawnComponent> PrePossessed;
        public XREvent<PawnComponent> PostPossessed;
        public XREvent<PawnComponent> PreUnpossessed;
        public XREvent<PawnComponent> PostUnpossessed;

        private PawnController? _controller;
        /// <summary>
        /// The interface that is managing and providing input to this pawn.
        /// </summary>
        public PawnController? Controller
        {
            get => _controller;
            set => SetField(ref _controller, value);
        }

        protected virtual void PostPossess()
            => PostPossessed.Invoke(this);
        protected virtual void PrePossess()
            => PrePossessed.Invoke(this);
        protected virtual void PostUnpossess()
            => PostUnpossessed.Invoke(this);
        protected virtual void PreUnpossess()
            => PreUnpossessed.Invoke(this);

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(Controller):
                        //Call before unregistering anything
                        PreUnpossess();
                        if (Controller?.ControlledPawn == this)
                            Controller.ControlledPawn = null;
                        if (Controller is LocalPlayerController localPlayerController)
                        {
                            UnregisterTick(ETickGroup.PrePhysics, ETickOrder.Input, TickInput);
                        }
                        //Call after unregistering controller
                        PostUnpossess();
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
                case nameof(Controller):
                    //Call before possessing the new pawn
                    PrePossess();
                    if (Controller is not null)
                        Controller.ControlledPawn = this;
                    if (Controller is LocalPlayerController localPlayerController)
                    {
                        RegisterTick(ETickGroup.PrePhysics, ETickOrder.Input, TickInput);
                    }
                    //Call after possessing the new pawn
                    PostPossess();
                    break;
            }
        }

        private void TickInput()
        {
            if (Controller is not LocalPlayerController localPlayerController ||
                localPlayerController.Input is not LocalInputInterface localInput)
                return;
            
            localInput.Gamepad?.TickStates(Engine.Time.Timer.FixedUpdateDelta);
            localInput.Mouse?.TickStates(Engine.Time.Timer.FixedUpdateDelta);
            localInput.Keyboard?.TickStates(Engine.Time.Timer.FixedUpdateDelta);
        }

        /// <summary>
        /// Casts the controller to a server player controller.
        /// </summary>
        public RemotePlayerController? ServerPlayerController => Controller as RemotePlayerController;

        /// <summary>
        /// Casts the controller to a local player controller.
        /// </summary>
        public LocalPlayerController? LocalPlayerController => Controller as LocalPlayerController;

        /// <summary>
        /// Casts the controller to a generic player controller.
        /// </summary>
        public PlayerControllerBase? PlayerController => Controller as PlayerControllerBase;

        /// <summary>
        /// The viewport of the local player controller that is controlling this pawn.
        /// </summary>
        public XRViewport? Viewport => LocalPlayerController?.Viewport;

        private CameraComponent? _camera;
        /// <summary>
        /// Dictates the component controlling the view of this pawn's controller.
        /// </summary>
        public CameraComponent? Camera
        {
            get => _camera;
            set => SetField(ref _camera, value);
        }

        private UIInputComponent? _userInterfaceInput = null;
        public UIInputComponent? UserInterfaceInput
        {
            get => _userInterfaceInput;
            set => SetField(ref _userInterfaceInput, value);
        }

        public virtual void RegisterInput(InputInterface input) { }

        /// <summary>
        /// Enqueues this pawn for possession by the local player with the given index.
        /// If the currently possessed pawn is null, possesses this pawn immediately.
        /// </summary>
        /// <param name="one"></param>
        public void EnqueuePossessionByLocalPlayer(ELocalPlayerIndex one)
            => Engine.State.GetOrCreateLocalPlayer(one).EnqueuePosession(this);
        /// <summary>
        /// Sets the controlled pawn of the local player with the given index to this pawn.
        /// </summary>
        /// <param name="one"></param>
        public void PossessByLocalPlayer(ELocalPlayerIndex one)
            => Engine.State.GetOrCreateLocalPlayer(one).ControlledPawn = this;

        public CameraComponent? GetCamera()
            => Camera is not null ? Camera : GetSiblingComponent<CameraComponent>();
    }
}
