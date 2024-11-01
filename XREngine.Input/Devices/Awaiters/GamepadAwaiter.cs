namespace XREngine.Input.Devices
{
    public abstract class GamepadAwaiter : DeviceAwaiter
    {
        public abstract BaseGamePad CreateGamepad(int index);
        protected void OnFoundGamepad(int index)
        {
            InputDevice device = CreateGamepad(index);
            InputDevice.CurrentDevices[EInputDeviceType.Gamepad][index] = device;
            OnFoundDevice(device);
        }
    }
}
