using System.Numerics;
using XREngine.Physics;
using XREngine.Scene.Transforms;

namespace XREngine.Data.Components
{
    public abstract class CollidableShape3DComponent : Shape3DComponent, ICollidable
    {
        public CollidableShape3DComponent() : base() { }
        
        public void RigidBodyUpdated()
        {
            if (World is null || _collisionObject == null)
                return;

            //Fixes bug where the collision object is not updated in the scene
            //TODO: verify if this is necessary with physx
            World.PhysicsScene.RemoveCollisionObject(_collisionObject);
            World.PhysicsScene.AddCollisionObject(_collisionObject);
        }

        protected XRCollisionObject? _collisionObject;
        public virtual XRCollisionObject? CollisionObject
        {
            get => _collisionObject;
            set
            {
                if (_collisionObject == value)
                    return;

                DeinitCollision();
                SetField(ref _collisionObject, value);
                InitCollision();
            }
        }

        private void InitCollision()
        {
            if (_collisionObject is null)
                return;
            
            _collisionObject.Owner = this;
            _collisionObject.TransformChanged += BodyMoved;

            Transform.WorldMatrixChanged += ThisMoved;

            if (IsActive)
                World?.PhysicsScene.AddCollisionObject(_collisionObject);
        }

        private void DeinitCollision()
        {
            if (_collisionObject is null)
                return;

            if (IsActive)
                World?.PhysicsScene.RemoveCollisionObject(_collisionObject);

            Transform.WorldMatrixChanged -= ThisMoved;

            _collisionObject.Owner = null;
            _collisionObject.TransformChanged -= BodyMoved;
        }

        public Matrix4x4 CollidableWorldMatrix { get; set; }

        public abstract XRCollisionShape GetCollisionShape();

        public void GenerateCollisionObject(ICollisionObjectConstructionInfo? info)
        {
            if (info is null)
            {
                //Engine.LogWarning("A rigid body could not be generated for collidable shape component; construction info is null.");
                CollisionObject = null;
                return;
            }

            info.CollisionShape = GetCollisionShape();
            info.InitialWorldTransform = SceneNode.Transform.WorldMatrix;

            if (info.CollisionShape != null)
            {
                switch (info)
                {
                    case RigidBodyConstructionInfo r:
                        CollisionObject = XRRigidBody.New(r);
                        break;
                    case SoftBodyConstructionInfo s:
                        CollisionObject = XRSoftBody.New(s);
                        break;
                    //case TGhostBodyConstructionInfo g:
                    //    CollisionObject = TGhostBody.New(g);
                    //    break;
                }
            }
            else
            {
                Debug.LogWarning("A rigid body could not be generated for collidable shape component; collision shape is null.");
                CollisionObject = null;
            }
        }

        private void BodyMoved(Matrix4x4 transform)
        {
            if (_collisionObject is null)
                return;

            Transform.DeriveWorldMatrix(_collisionObject.WorldTransform);
        }

        private void ThisMoved(TransformBase comp)
        {
            if (_collisionObject is null)
                return;

            _collisionObject.WorldTransform = Transform.WorldMatrix;
        }

        private void PhysicsSimulationStateChanged(bool isSimulating)
        {
            if (isSimulating)
                PhysicsSimulationStarted();
            else
                StopSimulatingPhysics();
        }

        private void StopSimulatingPhysics()
        {

        }

        private void PhysicsSimulationStarted()
        {

        }

        protected internal override void Start()
        {
            base.Start();

            if (_collisionObject is not null)
                World?.PhysicsScene?.AddCollisionObject(_collisionObject);
        }
        protected internal override void Stop()
        {
            base.Stop();

            if (_collisionObject is not null)
                World?.PhysicsScene?.RemoveCollisionObject(_collisionObject);
        }
    }
}
