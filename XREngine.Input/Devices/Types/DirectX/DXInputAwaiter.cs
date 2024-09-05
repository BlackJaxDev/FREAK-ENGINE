namespace XREngine.Input.Devices.DirectX
{
    [Serializable]
    public class DXInputAwaiter : DeviceAwaiter
    {
        public const int MaxControllers = 4;

        //Controller[] _controllers = new Controller[]
        //{
        //    new Controller(UserIndex.One),
        //    new Controller(UserIndex.Two),
        //    new Controller(UserIndex.Three),
        //    new Controller(UserIndex.Four),
        //};

        public override BaseGamePad CreateGamepad(int controllerIndex)
            => new DXGamepad(controllerIndex);
        public override BaseKeyboard CreateKeyboard(int index)
            => new DXKeyboard(index);
        public override BaseMouse CreateMouse(int index)
            => new DXMouse(index);

        public override void Tick(float delta)
        {
            //var gamepads = InputDevice.CurrentDevices[InputDeviceType.Gamepad];
            //var keyboards = InputDevice.CurrentDevices[InputDeviceType.Keyboard];
            //var mice = InputDevice.CurrentDevices[InputDeviceType.Mouse];
            //for (int i = 0; i < MaxControllers; ++i)
            //    if (gamepads[i] is null)
            //    {

            //        GamePadState gamepadState = GamePad.GetState(i);
            //        if (gamepadState.IsConnected)
            //        {
            //            GamePadCapabilities c = GamePad.GetCapabilities(i);
            //            OnFoundGamepad(i);
            //        }
            //    }

            //if (keyboards[0] is null)
            //{
            //    KeyboardState keyboardState = Keyboard.GetState();
            //    if (keyboardState.IsConnected)
            //        OnFoundKeyboard(0);
            //}
            //if (mice[0] is null)
            //{
            //    MouseState mouseState = Mouse.GetState();
            //    if (mouseState.IsConnected)
            //        OnFoundMouse(0);
            //}
        }
    }
}
