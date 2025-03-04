using MagicPhysX;
using XREngine.Components;
using XREngine.Data.Core;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxBase : XRBase
    {
        public abstract PxBase* BasePtr { get; }
    }
}