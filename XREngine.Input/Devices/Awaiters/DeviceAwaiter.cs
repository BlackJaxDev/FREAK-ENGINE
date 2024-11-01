namespace XREngine.Input.Devices
{
    public delegate void DelFoundDevice(InputDevice device);
    [Serializable]
    public abstract class DeviceAwaiter
    {
        public event DelFoundDevice? FoundDevice;
        protected void OnFoundDevice(InputDevice device)
            => FoundDevice?.Invoke(device);
        public abstract void Tick();
    }
}
