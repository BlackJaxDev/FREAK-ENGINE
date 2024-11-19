using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Trees;
using XREngine.Physics;
using XREngine.Physics.ShapeTracing;
using XREngine.Rendering.Physics.Physx;

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

        public abstract IAbstractDynamicRigidBody? NewDynamicRigidBody(
            AbstractPhysicsMaterial material,
            IAbstractPhysicsShape shape,
            float density,
            Vector3? position = null,
            Quaternion? rotation = null,
            Vector3? shapeOffsetTranslation = null,
            Quaternion? shapeOffsetRotation = null);
        public abstract IAbstractDynamicRigidBody? NewDynamicRigidBody(
            IAbstractPhysicsShape shape,
            float density,
            Vector3? position = null,
            Quaternion? rotation = null);
        public abstract IAbstractDynamicRigidBody? NewDynamicRigidBody(
            Vector3? position = null,
            Quaternion? rotation = null);

        public abstract IAbstractStaticRigidBody? NewStaticRigidBody(
            Vector3? position = null,
            Quaternion? rotation = null);
        public abstract IAbstractStaticRigidBody? NewStaticRigidBody(
            IAbstractPhysicsShape shape,
            Vector3? position = null,
            Quaternion? rotation = null);
        public abstract IAbstractStaticRigidBody? NewStaticRigidBody(
            AbstractPhysicsMaterial material,
            IAbstractPhysicsShape shape,
            Vector3? position = null,
            Quaternion? rotation = null,
            Vector3? shapeOffsetTranslation = null,
            Quaternion? shapeOffsetRotation = null);

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