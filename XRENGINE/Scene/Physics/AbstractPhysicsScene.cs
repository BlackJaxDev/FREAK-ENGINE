﻿using System.Numerics;
using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Rendering.Physics.Physx;
using XREngine.Scene.Components.Physics;

namespace XREngine.Scene
{
    public struct RaycastHit
    {
        public Vector3 Position;
        public Vector3 Normal;
        public float Distance;
        public uint FaceIndex;
        public Vector2 UV;
    }

    public abstract class AbstractPhysicsScene : XRBase
    {
        public event Action? OnSimulationStep;

        protected virtual void NotifySimulationStepped()
            => OnSimulationStep?.Invoke();

        public abstract Vector3 Gravity { get; set; }

        public abstract void Initialize();
        public abstract void Destroy();
        public abstract void StepSimulation();

        public bool RaycastAny(Segment worldSegment)
            => RaycastAny(worldSegment, out _);

        /// <summary>
        /// Raycasts the physics scene and returns true if anything was hit.
        /// </summary>
        /// <param name="worldSegment"></param>
        /// <param name="hitFaceIndex"></param>
        /// <returns></returns>
        public abstract bool RaycastAny(Segment worldSegment, out uint hitFaceIndex);
        /// <summary>
        /// Raycasts the physics scene and returns the first (nearest) hit item.
        /// </summary>
        /// <param name="worldSegment"></param>
        /// <param name="items"></param>
        public abstract void RaycastSingle(Segment worldSegment, SortedDictionary<float, List<(XRComponent? item, object? data)>> items);
        /// <summary>
        /// Raycasts the physics scene and returns all hit items.
        /// </summary>
        /// <param name="worldSegment"></param>
        /// <param name="results"></param>
        public abstract void RaycastMultiple(Segment worldSegment, SortedDictionary<float, List<(XRComponent? item, object? data)>> results);

        public abstract bool SweepAny(IPhysicsGeometry geometry, (Vector3 position, Quaternion rotation) pose, Vector3 unitDir, float distance, out uint hitFaceIndex);
        public abstract void SweepSingle(IPhysicsGeometry geometry, (Vector3 position, Quaternion rotation) pose, Vector3 unitDir, float distance, SortedDictionary<float, List<(XRComponent? item, object? data)>> items);
        public abstract void SweepMultiple(IPhysicsGeometry geometry, (Vector3 position, Quaternion rotation) pose, Vector3 unitDir, float distance, SortedDictionary<float, List<(XRComponent? item, object? data)>> results);

        public abstract bool OverlapAny(IPhysicsGeometry geometry, (Vector3 position, Quaternion rotation) pose, SortedDictionary<float, List<(XRComponent? item, object? data)>> results);
        public abstract void OverlapMultiple(IPhysicsGeometry geometry, (Vector3 position, Quaternion rotation) pose, SortedDictionary<float, List<(XRComponent? item, object? data)>> results);

        public virtual void DebugRender() { }
        public virtual void SwapDebugBuffers(){ }
        public virtual void DebugRenderCollect() { }

        public abstract void AddActor(IAbstractPhysicsActor actor);
        public abstract void RemoveActor(IAbstractPhysicsActor actor);

        public abstract void NotifyShapeChanged(IAbstractPhysicsActor actor);
    }
    public interface IAbstractPhysicsActor
    {
        void Destroy(bool wakeOnLostTouch = false);
    }
    public interface IAbstractStaticRigidBody : IAbstractRigidPhysicsActor
    {
        StaticRigidBodyComponent? OwningComponent { get; set; }
    }
    public interface IAbstractDynamicRigidBody : IAbstractRigidBody
    {
        DynamicRigidBodyComponent? OwningComponent { get; set; }
    }
    public interface IAbstractRigidPhysicsActor : IAbstractPhysicsActor
    {
        (Vector3 position, Quaternion rotation) Transform { get; }
        Vector3 LinearVelocity { get; }
        Vector3 AngularVelocity { get; }
    }
    public interface IAbstractRigidBody : IAbstractRigidPhysicsActor
    {

    }
}