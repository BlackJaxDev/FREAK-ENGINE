using XREngine.Data.Core;
using XREngine.Input;
using XREngine.Input.Devices;
using XREngine.Rendering;

namespace XREngine.Components
{
    /// <summary>
    /// A pawn is an actor that can be controlled by either a player or AI.
    /// </summary>
    public class PawnComponent : XRComponent
    {
        public XREvent<PawnComponent> PrePossessed;
        public XREvent<PawnComponent> PostPossessed;
        public XREvent<PawnComponent> PreUnpossessed;
        public XREvent<PawnComponent> PostUnpossessed;

        private UserInterfaceInputComponent? _hud = null;

        /// <summary>
        /// The interface that is managing and providing input to this pawn.
        /// </summary>
        public PawnController? Controller
        {
            get => _controller;
            set
            {
                if (Controller == value)
                    return;

                if (Controller is not null)
                {
                    //Call before unregistering anything
                    PreUnpossess();
                    Controller.ControlledPawn = null;
                    SetField(ref _controller, null);
                    //Call after unregistering controller
                    PostUnpossess();
                }

                //If the controller is not null, we are possessing a new pawn
                if (value is not null)
                {
                    //Call before possessing the new pawn
                    PrePossess();
                    SetField(ref _controller, value);
                    value.ControlledPawn = this;
                    //Call after possessing the new pawn
                    PostPossess();
                }
            }
        }

        protected virtual void PostPossess()
            => PostPossessed.Invoke(this);

        protected virtual void PrePossess()
            => PrePossessed.Invoke(this);

        protected virtual void PostUnpossess()
            => PostUnpossessed.Invoke(this);

        protected virtual void PreUnpossess()
            => PreUnpossessed.Invoke(this);

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

        private CameraComponent? _currentCameraComponent;
        private PawnController? _controller;

        /// <summary>
        /// Dictates the component controlling the view of this pawn's controller.
        /// </summary>
        public CameraComponent? CurrentCameraComponent
        {
            get => _currentCameraComponent;
            set => SetField(ref _currentCameraComponent, value);
        }

        public UserInterfaceInputComponent? HUD
        {
            get => _hud;
            set => SetField(ref _hud, value);
        }

        public virtual void RegisterInput(InputInterface input) { }
    }
}
