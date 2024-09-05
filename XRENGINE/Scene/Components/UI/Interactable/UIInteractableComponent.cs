using System.ComponentModel;

namespace XREngine.Rendering.UI
{
    public delegate void DelMouseMove(float x, float y);
    /// <summary>
    /// UI component that can be interacted with by the player.
    /// </summary>
    public abstract class UIInteractableComponent : UIMaterialComponent
    {
        public UIInteractableComponent()
            : base() { }

        public UIInteractableComponent(XRMaterial material, bool flipVerticalUVCoord = false)
            : base(material, flipVerticalUVCoord) { }

        [Category("Events")]
        public event Action? GotFocus;
        [Category("Events")]
        public event Action? LostFocus;
        [Category("Events")]
        public event DelMouseMove? MouseMove;
        [Category("Events")]
        public event Action? MouseEnter;
        [Category("Events")]
        public event Action? MouseLeave;

        private bool _registerInputsOnFocus = true;
        [Category("Interactable")]
        public bool RegisterInputsOnFocus
        {
            get => _registerInputsOnFocus;
            set => SetField(ref _registerInputsOnFocus, value);
        }

        private bool _isFocused = false;
        [Category("State")]
        public bool IsFocused
        {
            get => _isFocused;
            set
            {
                if (SetField(ref _isFocused, value))
                {
                    if (_isFocused)
                        OnGotFocus();
                    else
                        OnLostFocus();
                }
            }
        }
        private bool _isMouseOver = false;
        [Category("State")]
        public bool IsMouseOver
        {
            get => _isMouseOver;
            set
            {
                if (SetField(ref _isMouseOver, value))
                {
                    if (_isMouseOver)
                        OnMouseEnter();
                    else
                        OnMouseLeave();
                }
            }
        }
        private bool _isMouseDirectlyOver = false;
        [Category("State")]
        public bool IsMouseDirectlyOver
        {
            get => _isMouseDirectlyOver;
            set => SetField(ref _isMouseDirectlyOver, value);
        }

        public UIInteractableComponent? GamepadUpComponent { get; set; }
        public UIInteractableComponent? GamepadDownComponent { get; set; }
        public UIInteractableComponent? GamepadLeftComponent { get; set; }
        public UIInteractableComponent? GamepadRightComponent { get; set; }

        protected virtual void OnMouseMove(float x, float y) { }
        protected virtual void OnMouseEnter() => MouseEnter?.Invoke();
        protected virtual void OnMouseLeave() => MouseLeave?.Invoke();

        protected virtual void OnGamepadEnter() { }
        protected virtual void OnGamepadLeave() { }

        protected virtual void OnGotFocus()
        {
            if (RegisterInputsOnFocus)
            {
                //var input = OwningUserInterface?.LocalPlayerController?.Input;
                //if (input != null)
                //{
                //    input.Unregister = false;
                //    RegisterInputs(input);
                //}
            }

            GotFocus?.Invoke();
        }
        protected virtual void OnLostFocus()
        {
            if (RegisterInputsOnFocus)
            {
                //var input = OwningUserInterface?.LocalPlayerController?.Input;
                //if (input != null)
                //{
                //    input.Unregister = true;
                //    RegisterInputs(input);
                //    input.Unregister = false;
                //}
            }

            LostFocus?.Invoke();
        }
    }
}
