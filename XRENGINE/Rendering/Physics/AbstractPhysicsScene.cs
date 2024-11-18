using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Trees;
using XREngine.Physics;
using XREngine.Physics.ShapeTracing;

namespace XREngine.Scene
{
    public abstract class AbstractPhysicsScene : XRBase
    {
        public event Action? OnSimulationStep;

        protected void NotifySimulationStepped()
            => OnSimulationStep?.Invoke();

        public abstract void Initialize();
        public abstract void Destroy();
        public abstract void StepSimulation();
        public abstract IAbstractDynamicRigidBody? NewDynamicRigidBody();
        public abstract IAbstractStaticRigidBody? NewStaticRigidBody();

        public void Raycast(Segment worldSegment, SortedDictionary<float, List<(ITreeItem item, object? data)>> items)
        {

        }

        public bool Trace(ShapeTraceClosest closestTrace)
        {
            return false;
        }

        public void AddCollisionObject(XRCollisionObject collisionObject)
        {

        }

        public void RemoveCollisionObject(XRCollisionObject collisionObject)
        {

        }
    }
    public interface IAbstractStaticRigidBody
    {
        void Destroy();
    }
    public interface IAbstractDynamicRigidBody
    {
        void Destroy();
        (Vector3 position, Quaternion rotation) Transform { get; }
    }
}