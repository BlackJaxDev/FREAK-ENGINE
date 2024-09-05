namespace XREngine.Input.Devices
{
    public abstract class BaseHMD : InputDevice
    {
        public BaseHMD() : base(0) { }
        
        protected override int GetAxisCount() => 0; 
        protected override int GetButtonCount() => 1;
        public override EInputDeviceType DeviceType => EInputDeviceType.XRHMD;
    }
}
