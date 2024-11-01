namespace XREngine.Input.Devices
{
    public abstract class MouseAwaiter : DeviceAwaiter
    {
        public abstract BaseMouse CreateMouse(int index);
        protected void OnFoundMouse(int index)
        {
            InputDevice device = CreateMouse(index);
            InputDevice.CurrentDevices[EInputDeviceType.Mouse][index] = device;
            OnFoundDevice(device);
        }
    }
}
