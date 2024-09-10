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

        private UIInputComponent? _hud = null;

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

                Unpossess();
                Possess(value);
            }
        }

        private void Possess(PawnController? value)
        {
            if (value is null)
                return;

            //Call before possessing the new pawn
            PrePossess();
            SetField(ref _controller, value);
            value.ControlledPawn = this;
            //Call after possessing the new pawn
            PostPossess();
        }

        private void Unpossess()
        {
            if (Controller is null)
                return;
            
            //Call before unregistering anything
            PreUnpossess();
            if (Controller.ControlledPawn == this)
                Controller.ControlledPawn = null;
            SetField(ref _controller, null);
            //Call after unregistering controller
            PostUnpossess();
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

        public UIInputComponent? HUD
        {
            get => _hud;
            set => SetField(ref _hud, value);
        }

        public virtual void RegisterInput(InputInterface input) { }
    }
}
