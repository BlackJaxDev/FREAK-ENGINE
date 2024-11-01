using XREngine.Data.Core;

namespace XREngine.Input.Devices
{
    public class InputManagerBase : XRBase
    {
        public InputManagerBase() { }

        private bool _isPaused;
        public bool IsPaused
        {
            get => _isPaused;
            set => SetField(ref _isPaused, value);
        }
    }
}
