using Silk.NET.Input;
using XREngine.Input.Devices.Glfw;

namespace XREngine.Input.Devices
{
    public class LocalInputInterface : InputInterface
    {
        /// <summary>
        /// Global registration methods found here are called to register input for any and all controllers,
        /// regardless of the pawn they control or the type of controller they are.
        /// </summary>
        public static List<DelWantsInputsRegistered> GlobalRegisters { get; } = [];

        public BaseGamePad? Gamepad { get; private set; }
        public BaseKeyboard? Keyboard { get; private set; }
        public BaseMouse? Mouse { get; private set; }

        private int _localPlayerIndex;
        public int LocalPlayerIndex
        {
            get => _localPlayerIndex;
            set => SetField(ref _localPlayerIndex, value);
        }
        public override bool HideCursor
        {
            get => Mouse?.HideCursor ?? false;
            set
            {
                if (Mouse is not null)
                    Mouse.HideCursor = value;
            }
        }

        public LocalInputInterface(int localPlayerIndex) : base(localPlayerIndex)
        {
            LocalPlayerIndex = localPlayerIndex;
        }

        public override void TryRegisterInput()
        {
            if (Gamepad is null && Keyboard is null && Mouse is null)
                return;
            
            TryUnregisterInput();

            Unregister = false;

            //Interface gets input from pawn, hud, local controller, and global list
            OnInputRegistration();

            foreach (DelWantsInputsRegistered register in GlobalRegisters)
                register(this);
        }

        public override void TryUnregisterInput()
        {
            if (Gamepad is null && Keyboard is null && Mouse is null)
                return;
            
            //Call for regular old input registration, but in the backend,
            //unregister all calls instead of registering them.
            //This way the user doesn't have to do any extra work
            //other than just registering the inputs.
            Unregister = true;
            OnInputRegistration();
            foreach (DelWantsInputsRegistered register in GlobalRegisters)
                register(this);
            Unregister = false;
        }

        public override void RegisterAxisButtonPressedAction(string actionName, DelButtonState func)
        {
            //Gamepad?.TryRegisterAxisButtonPressedAction(actionName, func);
            //Keyboard?.TryRegisterAxisButtonPressedAction(actionName, func);
            //Mouse?.TryRegisterAxisButtonPressedAction(actionName, func);
        }
        public override void RegisterButtonPressedAction(string actionName, DelButtonState func)
        {

        }
        public override void RegisterAxisButtonEventAction(string actionName, Action func)
        {

        }
        public override void RegisterButtonEventAction(string actionName, Action func)
        {

        }
        public override void RegisterAxisUpdateAction(string actionName, DelAxisValue func, bool continuousUpdate)
        {

        }

        #region Mouse input registration
        public override void RegisterMouseButtonContinuousState(EMouseButton button, DelButtonState func)
            => Mouse?.RegisterButtonPressed(button, func, Unregister);
        public override void RegisterMouseButtonEvent(EMouseButton button, EButtonInputType type, Action func)
            => Mouse?.RegisterButtonEvent(button, type, func, Unregister);
        public override void RegisterMouseScroll(DelMouseScroll func)
            => Mouse?.RegisterScroll(func, Unregister);
        public override void RegisterMouseMove(DelCursorUpdate func, EMouseMoveType type)
            => Mouse?.RegisterMouseMove(func, type, Unregister);
        #endregion

        #region Keyboard input registration
        public override void RegisterKeyStateChange(EKey button, DelButtonState func)
            => Keyboard?.RegisterKeyPressed(button, func, Unregister);
        public override void RegisterKeyEvent(EKey button, EButtonInputType type, Action func)
            => Keyboard?.RegisterKeyEvent(button, type, func, Unregister);
        #endregion

        #region Gamepad input registration
        public override void RegisterAxisButtonPressed(EGamePadAxis axis, DelButtonState func)
            => Gamepad?.RegisterButtonState(axis, func, Unregister);
        public override void RegisterButtonPressed(EGamePadButton button, DelButtonState func)
            => Gamepad?.RegisterButtonState(button, func, Unregister);
        public override void RegisterButtonEvent(EGamePadButton button, EButtonInputType type, Action func)
            => Gamepad?.RegisterButtonEvent(button, type, func, Unregister);
        public override void RegisterAxisButtonEvent(EGamePadAxis button, EButtonInputType type, Action func)
            => Gamepad?.RegisterButtonEvent(button, type, func, Unregister);
        public override void RegisterAxisUpdate(EGamePadAxis axis, DelAxisValue func, bool continuousUpdate)
            => Gamepad?.RegisterAxisUpdate(axis, func, continuousUpdate, Unregister);
        #endregion

        /// <summary>
        /// Retrieves the state of the requested mouse button: 
        /// pressed, released, held, or double pressed.
        /// </summary>
        /// <param name="button">The button to read the state of.</param>
        /// <param name="type">The type of state to observe.</param>
        /// <returns>True if the state is current.</returns>
        public override bool GetMouseButtonState(EMouseButton button, EButtonInputType type)
            => Mouse?.GetButtonState(button, type) ?? false;
        /// <summary>
        /// Retrieves the state of the requested keyboard key: 
        /// pressed, released, held, or double pressed.
        /// </summary>
        /// <param name="key">The button to read the state of.</param>
        /// <param name="type">The type of state to observe.</param>
        /// <returns></returns>
        public override bool GetKeyState(EKey key, EButtonInputType type)
            => Keyboard?.GetKeyState(key, type) ?? false;
        /// <summary>
        /// Retrieves the state of the requested gamepad button: 
        /// pressed, released, held, or double pressed.
        /// </summary>
        /// <param name="button">The button to read the state of.</param>
        /// <param name="type">The type of state to observe.</param>
        /// <returns></returns>
        public override bool GetButtonState(EGamePadButton button, EButtonInputType type)
            => Gamepad?.GetButtonState(button, type) ?? false;
        /// <summary>
        /// Retrieves the state of the requested axis button: 
        /// pressed, released, held, or double pressed.
        /// </summary>
        /// <param name="axis">The axis button to read the state of.</param>
        /// <param name="type">The type of state to observe.</param>
        /// <returns></returns>
        public override bool GetAxisState(EGamePadAxis axis, EButtonInputType type)
            => Gamepad?.GetAxisState(axis, type) ?? false;
        /// <summary>
        /// Retrieves the value of the requested axis in the range 0.0f to 1.0f 
        /// or -1.0f to 1.0f for control sticks.
        /// </summary>
        /// <param name="axis">The axis to read the value of.</param>
        /// <returns>The magnitude of the given axis.</returns>
        public override float GetAxisValue(EGamePadAxis axis)
            => Gamepad?.GetAxisValue(axis) ?? 0.0f;

        public void UpdateDevices(IInputContext? input)
        {
            TryUnregisterInput();
            GetDevices(input);
            TryRegisterInput();
        }
        private void GetDevices(IInputContext? context)
        {
            AttachInterfaceToDevices(false);

            if (context is null)
                return;

            context.ConnectionChanged += ConnectionChanged;

            //var gamepads = InputDevice.CurrentDevices[EInputDeviceType.Gamepad];
            //var keyboards = InputDevice.CurrentDevices[EInputDeviceType.Keyboard];
            //var mice = InputDevice.CurrentDevices[EInputDeviceType.Mouse];

            var gamepads = context.Gamepads;
            var keyboards = context.Keyboards;
            var mice = context.Mice;

            if (_localPlayerIndex >= 0 && _localPlayerIndex < gamepads.Count)
                Gamepad = new GlfwGamepad(gamepads[_localPlayerIndex]);

            //Keyboard and mouse are reserved for the first player only
            //TODO: support multiple mice and keyboard? Could get difficult with laptops and trackpads and whatnot. Probably no-go.
            //TODO: support input from ALL keyboards and mice for first player. Not just the first found keyboard and mouse.

            if (keyboards.Count > 0 && _localPlayerIndex == 0)
                Keyboard = new GlfwKeyboard(keyboards[0]);

            if (mice.Count > 0 && _localPlayerIndex == 0)
                Mouse = new GlfwMouse(mice[0]);

            AttachInterfaceToDevices(true);
        }

        private void ConnectionChanged(IInputDevice device, bool connected)
        {

        }

        private void AttachInterfaceToDevices(bool attach)
        {
            if (attach)
            {
                Gamepad?.InputInterfaces.Add(this);
                Keyboard?.InputInterfaces.Add(this);
                Mouse?.InputInterfaces.Add(this);
            }
            else
            {
                Gamepad?.InputInterfaces.Remove(this);
                Keyboard?.InputInterfaces.Remove(this);
                Mouse?.InputInterfaces.Remove(this);
            }
        }
    }
}
