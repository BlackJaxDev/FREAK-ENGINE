using System.Numerics;

namespace XREngine.Rendering.UI
{
    public delegate void DelMouseMove(float x, float y, UIInteractableComponent comp);
    /// <summary>
    /// UI component that can be interacted with by the player.
    /// </summary>
    public abstract class UIInteractableComponent : UIComponent
    {
        public event Action<UIInteractableComponent>? GotFocus;
        public event Action<UIInteractableComponent>? LostFocus;
        public event DelMouseMove? MouseMove;
        public event Action<UIInteractableComponent>? MouseOverlapEnter;
        public event Action<UIInteractableComponent>? MouseOverlapLeave;
        public event Action<UIInteractableComponent>? GamepadNavigateEnter;
        public event Action<UIInteractableComponent>? GamepadNavigateLeave;

        private bool _registerInputsOnFocus = true;
        public bool RegisterInputsOnFocus
        {
            get => _registerInputsOnFocus;
            set => SetField(ref _registerInputsOnFocus, value);
        }

        private bool _isFocused = false;
        /// <summary>
        /// Set when this component has focus.
        /// Focus is set after the mouse clicks on this component or when gamepad navigates to it.
        /// </summary>
        public bool IsFocused
        {
            get => _isFocused;
            set => SetField(ref _isFocused, value);
        }

        private bool _isMouseOver = false;
        /// <summary>
        /// Set when the mouse is over this component.
        /// </summary>
        public bool IsMouseOver
        {
            get => _isMouseOver;
            set => SetField(ref _isMouseOver, value);
        }

        private bool _isMouseDirectlyOver = false;
        /// <summary>
        /// Set when the mouse is directly over this component.
        /// "Directly over" means this component is the top-most component under the mouse.
        /// </summary>
        public bool IsMouseDirectlyOver
        {
            get => _isMouseDirectlyOver;
            set => SetField(ref _isMouseDirectlyOver, value);
        }

        public UIInteractableComponent? GamepadUpComponent { get; set; }
        public UIInteractableComponent? GamepadDownComponent { get; set; }
        public UIInteractableComponent? GamepadLeftComponent { get; set; }
        public UIInteractableComponent? GamepadRightComponent { get; set; }

        protected virtual void OnMouseMove(float x, float y)
            => MouseMove?.Invoke(x, y, this);
        protected virtual void OnMouseOverlapEnter()
            => MouseOverlapEnter?.Invoke(this);
        protected virtual void OnMouseOverlapLeave()
            => MouseOverlapLeave?.Invoke(this);
        protected virtual void OnGamepadNavigateEnter()
            => GamepadNavigateEnter?.Invoke(this);
        protected virtual void OnGamepadNavigateLeave()
            => GamepadNavigateLeave?.Invoke(this);

        /// <summary>
        /// Called when this component gains focus.
        /// Focus is gained after the mouse clicks on this component or the gamepad navigates to it.
        /// </summary>
        protected virtual void OnGotFocus()
        {
            //if (RegisterInputsOnFocus)
            //{
            //    var input = OwningUserInterface?.LocalPlayerController?.Input;
            //    if (input != null)
            //    {
            //        input.Unregister = false;
            //        RegisterInputs(input);
            //    }
            //}

            GotFocus?.Invoke(this);
        }

        /// <summary>
        /// Called when this component loses focus.
        /// Focus is lost when the mouse clicks off this component or the gamepad navigates away from it.
        /// </summary>
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

            LostFocus?.Invoke(this);
        }

        public event Action<UIInteractableComponent>? BackAction;

        /// <summary>
        /// Typically mapped to the "B" button on a gamepad to go back or cancel an action, etc.
        /// </summary>
        public void OnBack()
        {
            BackAction?.Invoke(this);
            IsFocused = false;
        }

        public event Action<UIInteractableComponent>? InteractAction;
        
        /// <summary>
        /// Typically mapped to the "A" button on a gamepad to click buttons, etc.
        /// </summary>
        public void OnInteract()
        {
            InteractAction?.Invoke(this);
            IsFocused = true;
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(IsMouseOver):
                    if (IsMouseOver)
                        OnMouseOverlapEnter();
                    else
                        OnMouseOverlapLeave();
                    break;
                case nameof(IsFocused):
                    if (IsFocused)
                        OnGotFocus();
                    else
                        OnLostFocus();
                    break;
            }
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(IsFocused):
                        if (IsFocused)
                            OnLostFocus();
                        break;
                }
            }
            return change;
        }

        internal void DoMouseMove(object v1, object v2)
        {
            throw new NotImplementedException();
        }
    }
}
