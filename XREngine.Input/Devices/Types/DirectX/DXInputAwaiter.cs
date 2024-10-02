//using DXNET.XInput;
//using Silk.NET.Input.Extensions;

//namespace XREngine.Input.Devices.DirectX
//{
//    [Serializable]
//    public class DXInputAwaiter : DeviceAwaiter
//    {
//        public const int MaxControllers = 4;

//        private readonly Controller[] _controllers =
//        [
//            new(UserIndex.One),
//            new(UserIndex.Two),
//            new(UserIndex.Three),
//            new(UserIndex.Four),
//        ];

//        public override BaseGamePad CreateGamepad(int controllerIndex)
//            => new DXGamepad(controllerIndex);
//        public override BaseKeyboard CreateKeyboard(int index)
//            => new DXKeyboard(index);
//        public override BaseMouse CreateMouse(int index)
//            => new DXMouse(index);

//        public override void Tick(float delta)
//        {
//            var gamepads = InputDevice.CurrentDevices[EInputDeviceType.Gamepad];
//            var keyboards = InputDevice.CurrentDevices[EInputDeviceType.Keyboard];
//            var mice = InputDevice.CurrentDevices[EInputDeviceType.Mouse];
//            for (int i = 0; i < MaxControllers; ++i)
//                if (gamepads[i] is null)
//                {
//                    GamepadState gamepadState = Silk.NET.Input.Glfw.GlfwInput.GetState(_controllers[i]);
//                    if (gamepadState.IsConnected)
//                    {
//                        GamePadCapabilities c = GamePad.GetCapabilities(i);
//                        OnFoundGamepad(i);
//                    }
//                }

//            if (keyboards[0] is null)
//            {
//                KeyboardState keyboardState = Keyboard.GetState();
//                if (keyboardState.IsConnected)
//                    OnFoundKeyboard(0);
//            }
//            if (mice[0] is null)
//            {
//                MouseState mouseState = Mouse.GetState();
//                if (mouseState.IsConnected)
//                    OnFoundMouse(0);
//            }
//        }
//    }
//}
