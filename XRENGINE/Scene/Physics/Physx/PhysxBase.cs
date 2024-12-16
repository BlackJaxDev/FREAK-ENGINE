using MagicPhysX;
using XREngine.Components;
using XREngine.Data.Core;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxBase : XRBase
    {
        private XRComponent? _owningComponent;

        public abstract PxBase* BasePtr { get; }

        public XRComponent? OwningComponent
        {
            get => _owningComponent;
            set => SetField(ref _owningComponent, value);
        }
    }
}