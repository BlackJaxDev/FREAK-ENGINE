using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx
{
    public interface IAbstractPhysicsShape
    {

    }
    public unsafe abstract class PhysxShape : PhysxRefCounted, IAbstractPhysicsShape
    {
        private readonly unsafe PxShape* _obj;
        public PxShape* ShapePtr => _obj;

        public PhysxShape(PxShape* shape)
        {
            _obj = shape;
        }

        public override unsafe PxBase* BasePtr => (PxBase*)_obj;
        public override unsafe PxRefCounted* RefCountedPtr => (PxRefCounted*)_obj;

        public abstract PxGeometry* GeometryPtr { get; }
    }
}