using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Trees;
using XREngine.Physics.ShapeTracing;

namespace XREngine.Scene
{
    public abstract class AbstractPhysicsScene : XRBase
    {
        public event Action? OnSimulationStep;

        protected void NotifySimulationStepped()
            => OnSimulationStep?.Invoke();

        public ManualResetEventSlim SimulationRunning { get; } = new ManualResetEventSlim();

        public abstract void Initialize();
        public abstract void Destroy();
        public abstract void StepSimulation();

        public abstract void Raycast(Segment worldSegment, SortedDictionary<float, List<(ITreeItem item, object? data)>> items, out Vector3 hitNormalWorld, out Vector3 hitPositionWorld, out float hitDistance);
        public bool Trace(ShapeTraceClosest closestTrace)
        {
            return false;
        }

        public abstract void AddActor(IAbstractPhysicsActor actor);
        public abstract void RemoveActor(IAbstractPhysicsActor actor);

        public abstract void DebugRender();
        public abstract void NotifyShapeChanged(IAbstractPhysicsActor actor);
    }
    public interface IAbstractPhysicsActor
    {
        void Destroy(bool wakeOnLostTouch = false);
    }
    public interface IAbstractStaticRigidBody : IAbstractPhysicsActor
    {

    }
    public interface IAbstractDynamicRigidBody : IAbstractPhysicsActor
    {
        (Vector3 position, Quaternion rotation) Transform { get; }
    }
}