namespace XREngine.Input.Devices
{
    public delegate void DelWantsInputsRegistered(InputInterface input);
    /// <summary>
    /// Handles input from keyboards, mice, gamepads, etc.
    /// </summary>
    [Serializable]
    public abstract class InputInterface(int serverIndex)
    {
        public event DelWantsInputsRegistered? InputRegistration;
        protected void OnInputRegistration()
            => InputRegistration?.Invoke(this);

        public int ServerIndex { get; } = serverIndex;

        /// <summary>
        /// Unregister is false when the controller has gained focus and is currently adding inputs to handle.
        /// Unregister is true when the controller has lost focus and inputs are being removed.
        /// </summary>
        public bool Unregister { get; set; } = false;

        public abstract void TryRegisterInput();
        public abstract void TryUnregisterInput();

        public abstract void RegisterAxisButtonPressedAction(string actionName, DelButtonState func);
        public abstract void RegisterButtonPressedAction(string actionName, DelButtonState func);
        public abstract void RegisterAxisButtonEventAction(string actionName, Action func);
        public abstract void RegisterButtonEventAction(string actionName, Action func);
        public abstract void RegisterAxisUpdateAction(string actionName, DelAxisValue func, bool continuousUpdate);

        /// <summary>
        /// The function provided will be called every frame with the current state of the mouse button.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="func"></param>
        public abstract void RegisterMouseButtonContinuousState(EMouseButton button, DelButtonState func);
        /// <summary>
        /// The function provided will be called when the mouse button is pressed, released, held, or double pressed.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="type"></param>
        /// <param name="func"></param>
        public abstract void RegisterMouseButtonEvent(EMouseButton button, EButtonInputType type, Action func);
        public abstract void RegisterMouseScroll(DelMouseScroll func);
        public abstract void RegisterMouseMove(DelCursorUpdate func, EMouseMoveType type);

        /// <summary>
        /// The function provided will be called every frame with the current state of the key.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="func"></param>
        public abstract void RegisterKeyContinuousState(EKey button, DelButtonState func);
        public abstract void RegisterKeyEvent(EKey button, EButtonInputType type, Action func);

        public abstract void RegisterAxisButtonPressed(EGamePadAxis axis, DelButtonState func);
        public abstract void RegisterButtonPressed(EGamePadButton button, DelButtonState func);
        public abstract void RegisterButtonEvent(EGamePadButton button, EButtonInputType type, Action func);
        public abstract void RegisterAxisButtonEvent(EGamePadAxis button, EButtonInputType type, Action func);
        public abstract void RegisterAxisUpdate(EGamePadAxis axis, DelAxisValue func, bool continuousUpdate);

        /// <summary>
        /// Retrieves the state of the requested mouse button: 
        /// pressed, released, held, or double pressed.
        /// </summary>
        /// <param name="button">The button to read the state of.</param>
        /// <param name="type">The type of state to observe.</param>
        /// <returns>True if the state is current.</returns>
        public abstract bool GetMouseButtonState(EMouseButton button, EButtonInputType type);
        /// <summary>
        /// Retrieves the state of the requested keyboard key: 
        /// pressed, released, held, or double pressed.
        /// </summary>
        /// <param name="key">The button to read the state of.</param>
        /// <param name="type">The type of state to observe.</param>
        /// <returns></returns>
        public abstract bool GetKeyState(EKey key, EButtonInputType type);
        /// <summary>
        /// Retrieves the state of the requested gamepad button: 
        /// pressed, released, held, or double pressed.
        /// </summary>
        /// <param name="button">The button to read the state of.</param>
        /// <param name="type">The type of state to observe.</param>
        /// <returns></returns>
        public abstract bool GetButtonState(EGamePadButton button, EButtonInputType type);
        /// <summary>
        /// Retrieves the state of the requested axis button: 
        /// pressed, released, held, or double pressed.
        /// </summary>
        /// <param name="axis">The axis button to read the state of.</param>
        /// <param name="type">The type of state to observe.</param>
        /// <returns></returns>
        public abstract bool GetAxisState(EGamePadAxis axis, EButtonInputType type);
        /// <summary>
        /// Retrieves the value of the requested axis in the range 0.0f to 1.0f 
        /// or -1.0f to 1.0f for control sticks.
        /// </summary>
        /// <param name="axis">The axis to read the value of.</param>
        /// <returns>The magnitude of the given axis.</returns>
        public abstract float GetAxisValue(EGamePadAxis axis);
    }
}
