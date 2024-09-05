using XREngine.Components;
using XREngine.Input.Devices;
using XREngine.Rendering;

namespace XREngine.Input
{
    //TODO: handle sending controller input packets to the server
    public class LocalPlayerController : PlayerController<LocalInputInterface>
    {
        public LocalPlayerController(ELocalPlayerIndex index) : base(new LocalInputInterface((int)index))
        {
            _localPlayerIndex = index;

            _pawnPossessionQueue = new Queue<PawnComponent>();
            if (_pawnPossessionQueue.Count != 0 && ControlledPawn is null)
                ControlledPawn = _pawnPossessionQueue.Dequeue();
        }

        private readonly ELocalPlayerIndex _localPlayerIndex;
        public ELocalPlayerIndex LocalPlayerIndex => _localPlayerIndex;

        private XRViewport? _viewport = null;
        public XRViewport? Viewport
        {
            get => _viewport;
            internal set
            {
                //if (_viewport != null && _viewport.OwningPanel.GlobalHud != null)
                //    _input.WantsInputsRegistered -= _viewport.OwningPanel.GlobalHud.RegisterInput;
                _viewport = value;
                UpdateViewportCamera();
                //if (_viewport.OwningPanel.GlobalHud != null)
                //    _input.WantsInputsRegistered += _viewport.OwningPanel.GlobalHud.RegisterInput;

            }
        }

        public override PawnComponent? ControlledPawn
        {
            get => base.ControlledPawn;
            set
            {
                base.ControlledPawn = value;
                UpdateViewportCamera();
            }
        }

        /// <summary>
        /// Updates the viewport with the HUD and/or camera from the controlled pawn.
        /// Called when the viewport, controlled pawn, or the  changes.
        /// </summary>
        private void UpdateViewportCamera()
        {
            if (_viewport is not null)
                _viewport.CameraComponent = _controlledPawn?.CurrentCameraComponent;
        }
        protected override void RegisterInput(InputInterface input)
        {
            //input.RegisterButtonEvent(EKey.Escape, ButtonInputType.Pressed, OnTogglePause);
            //input.RegisterButtonEvent(GamePadButton.SpecialRight, ButtonInputType.Pressed, OnTogglePause);
        }
        protected override void OnDestroying()
        {
            base.OnDestroying();
            Viewport = null;
        }
    }
}
