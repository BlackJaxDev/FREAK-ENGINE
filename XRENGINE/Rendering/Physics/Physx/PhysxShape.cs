using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxShape(PxShape* shape) : PhysxRefCounted, IAbstractPhysicsShape
    {
        public PxShape* ShapePtr => shape;

        public override unsafe PxBase* BasePtr => (PxBase*)shape;
        public override unsafe PxRefCounted* RefCountedPtr => (PxRefCounted*)shape;
    }
}