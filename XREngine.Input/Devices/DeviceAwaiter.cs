namespace XREngine.Input.Devices
{
    public delegate void DelFoundDevice(InputDevice device);
    [Serializable]
    public abstract class DeviceAwaiter
    {
        public event DelFoundDevice? FoundDevice;
        public abstract void Tick(float delta);

        public abstract BaseGamePad CreateGamepad(int index);
        public abstract BaseKeyboard CreateKeyboard(int index);
        public abstract BaseMouse CreateMouse(int index);

        protected void OnFoundGamepad(int index)
        {
            InputDevice device = CreateGamepad(index);
            InputDevice.CurrentDevices[EInputDeviceType.Gamepad][index] = device;
            FoundDevice?.Invoke(device);
        }
        protected void OnFoundKeyboard(int index)
        {
            InputDevice device = CreateKeyboard(index);
            InputDevice.CurrentDevices[EInputDeviceType.Keyboard][index] = device;
            FoundDevice?.Invoke(device);
        }
        protected void OnFoundMouse(int index)
        {
            InputDevice device = CreateMouse(index);
            InputDevice.CurrentDevices[EInputDeviceType.Mouse][index] = device;
            FoundDevice?.Invoke(device);
        }
    }
}
