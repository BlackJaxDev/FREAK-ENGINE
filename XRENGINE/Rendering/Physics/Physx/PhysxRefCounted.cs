using MagicPhysX;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public abstract unsafe class PhysxRefCounted : PhysxBase
    {
        public abstract PxRefCounted* RefCountedPtr { get; }

        public uint ReferenceCount => PxRefCounted_getReferenceCount(RefCountedPtr);

        public void Aquire() => PxRefCounted_acquireReference_mut(RefCountedPtr);
        public void Release() => PxRefCounted_release_mut(RefCountedPtr);

        public override unsafe PxBase* BasePtr => (PxBase*)RefCountedPtr;
    }
}