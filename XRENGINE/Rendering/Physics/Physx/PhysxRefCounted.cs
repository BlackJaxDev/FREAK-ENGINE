using MagicPhysX;
using static MagicPhysX.NativeMethods;

namespace XREngine.Rendering.Physics.Physx
{
    public abstract unsafe class PhysxRefCounted : PhysxBase
    {
        public abstract PxRefCounted* RefCounted { get; }

        public uint ReferenceCount => PxRefCounted_getReferenceCount(RefCounted);

        public void Aquire() => PxRefCounted_acquireReference_mut(RefCounted);
        public void Release() => PxRefCounted_release_mut(RefCounted);

        public override unsafe PxBase* Base => (PxBase*)RefCounted;
    }
}