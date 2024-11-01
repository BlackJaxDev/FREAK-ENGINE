namespace XREngine.Input.Devices
{
    public abstract class KeyboardAwaiter : DeviceAwaiter
    {
        public abstract BaseKeyboard CreateKeyboard(int index);
        protected void OnFoundKeyboard(int index)
        {
            InputDevice device = CreateKeyboard(index);
            InputDevice.CurrentDevices[EInputDeviceType.Keyboard][index] = device;
            OnFoundDevice(device);
        }
    }
}
