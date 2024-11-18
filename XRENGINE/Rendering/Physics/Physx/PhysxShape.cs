using MagicPhysX;

namespace XREngine.Rendering.Physics.Physx
{
    public unsafe abstract class PhysxShape : PhysxRefCounted
    {
        private readonly unsafe PxShape* _obj;

        public PhysxShape(PxShape* shape)
        {
            _obj = shape;
        }

        public override unsafe PxBase* Base => (PxBase*)_obj;

        public override unsafe PxRefCounted* RefCounted => (PxRefCounted*)_obj;

        public abstract PxGeometry* Geometry { get; }
    }
}