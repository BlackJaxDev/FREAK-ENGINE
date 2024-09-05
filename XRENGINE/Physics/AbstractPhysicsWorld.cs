using System.Numerics;
using XREngine.Data.Core;
using XREngine.Physics.ContactTesting;
using XREngine.Physics.RayTracing;
using XREngine.Physics.ShapeTracing;

namespace XREngine.Physics
{
    public abstract class AbstractPhysicsWorld : XRBase, IDisposable
    {
        public virtual Vector3 Gravity { get; set; } = new Vector3(0.0f, -9.81f, 0.0f);
        public bool AllowIndividualAabbUpdates { get; set; } = true;

        //internal ConcurrentQueue<RayTrace> PopulatingRayTraces = new ConcurrentQueue<RayTrace>();
        //internal ConcurrentQueue<ShapeTrace> PopulatingShapeTraces = new ConcurrentQueue<ShapeTrace>();
        //internal ConcurrentQueue<RayTrace> ConsumingRayTraces = new ConcurrentQueue<RayTrace>();
        //internal ConcurrentQueue<ShapeTrace> ConsumingShapeTraces = new ConcurrentQueue<ShapeTrace>();

        public abstract bool DrawConstraints { get; set; }
        public abstract bool DrawConstraintLimits { get; set; }
        public abstract bool DrawCollisionAABBs { get; set; }

        /// <summary>
        /// Renders all debug objects. Only to be called in the render pass.
        /// </summary>
        public abstract void DrawDebugWorld();
        /// <summary>
        /// Moves the physics simulation forward by the specified amount of seconds.
        /// </summary>
        /// <param name="delta">How many seconds to add.</param>
        public abstract void StepSimulation(float delta);
        /// <summary>
        /// Adds a collision object to the physics world.
        /// </summary>
        /// <param name="collision">The collision object to add.</param>
        public abstract void AddCollisionObject(XRCollisionObject collision);
        /// <summary>
        /// Removes a collision object from the physics world.
        /// </summary>
        /// <param name="collision">The collision object to remove.</param>
        public abstract void RemoveCollisionObject(XRCollisionObject collision);
        /// <summary>
        /// Add a simulation constraint to the physics world.
        /// </summary>
        /// <param name="constraint">The constraint to add.</param>
        public abstract void AddConstraint(XRPhysicsConstraint constraint);
        /// <summary>
        /// Removes a simulation constraint from the physics world.
        /// </summary>
        /// <param name="constraint">The constraint to remove.</param>
        public abstract void RemoveConstraint(XRPhysicsConstraint constraint);
        /// <summary>
        /// Shoots a ray using a start and end point and determines collisions with the physics world.
        /// </summary>
        /// <param name="result">Contains parameters and also the results of the trace once this method returns.</param>
        /// <returns>True if any hits occurred.</returns>
        public abstract bool RayTrace(RayTrace result);
        /// <summary>
        /// Moves a shape through the physics world using a start and end transformations and determines collisions.
        /// </summary>
        /// <param name="result">Contains parameters and also the results of the trace once this method returns.</param>
        /// <returns>True if any hits occurred.</returns>
        public abstract bool ShapeTrace(ShapeTrace result);
        public abstract bool ContactTest(ContactTest result);
        /// <summary>
        /// Recalculates the AABBs of all collision objects in the physics world.
        /// </summary>
        public abstract void UpdateAabbs();
        /// <summary>
        /// Recalculates a specific collision object's AABB.
        /// </summary>
        /// <param name="collision">The object whose AABB to recalculate.</param>
        public void UpdateSingleAabb(XRCollisionObject collision)
        {
            if (AllowIndividualAabbUpdates)
                OnUpdateSingleAabb(collision);
        }

        protected abstract void OnUpdateSingleAabb(XRCollisionObject collision);
        public abstract void Dispose();

        internal void Swap()
        {
            //THelpers.Swap(ref PopulatingRayTraces, ref ConsumingRayTraces);
            //THelpers.Swap(ref PopulatingShapeTraces, ref ConsumingShapeTraces);
        }
    }
}
