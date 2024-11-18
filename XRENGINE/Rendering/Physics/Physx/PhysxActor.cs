using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxActor : PhysxBase
    {
        public abstract PxActor* Actor { get; }
        public override unsafe PxBase* Base => (PxBase*)Actor;
    }
}