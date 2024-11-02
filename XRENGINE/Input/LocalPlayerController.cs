using XREngine.Components;
using XREngine.Input.Devices;
using XREngine.Rendering;

namespace XREngine.Input
{
    //TODO: handle sending controller input packets to the server
    public class LocalPlayerController(ELocalPlayerIndex index) : PlayerController<LocalInputInterface>(new LocalInputInterface((int)index))
    {
        public ELocalPlayerIndex LocalPlayerIndex => index;

        private XRViewport? _viewport = null;
        public XRViewport? Viewport
        {
            get => _viewport;
            internal set => SetField(ref _viewport, value);
        }

        protected override void OnPropertyChanged<T2>(string? propName, T2 prev, T2 field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Viewport):
                case nameof(ControlledPawn):
                    UpdateViewportCamera();
                    break;
            }
        }

        /// <summary>
        /// Updates the viewport with the HUD and/or camera from the controlled pawn.
        /// Called when the viewport, controlled pawn, or the  changes.
        /// </summary>
        private void UpdateViewportCamera()
        {
            if (_viewport is not null)
            {
                _viewport.CameraComponent = _controlledPawn?.GetCamera();
                Input.UpdateDevices(_viewport.Window?.Input);
            }
            else
                Input.UpdateDevices(null);
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
