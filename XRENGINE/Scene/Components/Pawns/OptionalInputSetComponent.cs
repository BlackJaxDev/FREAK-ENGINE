using XREngine.Input.Devices;

namespace XREngine.Components
{
    public abstract class OptionalInputSetComponent : XRComponent
    {
        public abstract void RegisterInput(InputInterface input);
    }
}
