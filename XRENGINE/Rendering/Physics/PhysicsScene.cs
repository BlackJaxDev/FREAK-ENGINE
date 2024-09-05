using System.Collections;
using System.Numerics;
using XREngine.Data.Core;
using XREngine.Physics;
using XREngine.Physics.ShapeTracing;
using XREngine.Rendering;

namespace XREngine.Scene
{
    public abstract class PhysicsScene : XRBase, IEnumerable<IRigidBodyActor>
    {
        private readonly List<IRigidBodyActor> _components = [];
        public IReadOnlyList<IRigidBodyActor> Components => _components;

        public abstract void Initialize();
        public abstract void Destroy();
        public abstract void StepSimulation();
        public virtual void AddPhysicsObject(IRigidBodyActor phys)
        {
            _components.Add(phys);
        }
        public virtual void RemovePhysicsObject(IRigidBodyActor phys)
        {
            _components.Remove(phys);
        }

        public IEnumerator<IRigidBodyActor> GetEnumerator()
            => ((IEnumerable<IRigidBodyActor>)_components).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)_components).GetEnumerator();

        internal void AddCollisionObject(XRCollisionObject collisionObject)
        {

        }

        internal void RemoveCollisionObject(XRCollisionObject collisionObject)
        {

        }

        internal void RemoveRigidBody(XRRigidBody collision)
        {

        }

        internal void AddRigidBody(XRRigidBody collision, short group, short collidesWith)
        {

        }

        internal bool Trace(ShapeTraceClosest closestTrace)
        {
            return false;
        }
    }
    public abstract class AbstractRigidBody : XRBase
    {
        public abstract void SetTransform(Vector3 position, Quaternion rotation);
        public abstract void GetTransform(out Vector3 position, out Quaternion rotation);
    }
}