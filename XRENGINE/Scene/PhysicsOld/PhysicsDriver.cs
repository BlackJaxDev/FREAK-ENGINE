using System.ComponentModel;
using System.Numerics;
using XREngine.Components;

namespace XREngine.Physics
{
    public delegate void DelMatrixUpdate(Matrix4x4 worldMatrix);
    public delegate void SimulationUpdate(bool isSimulating);
    public delegate void PhysicsEndContact(ICollidable me, ICollidable other);
    //public delegate void PhysicsOverlap(ICollidable me, ICollidable other, ManifoldPoint point);
    
    public class PhysicsDriver : XRWorldObjectBase
    {
        //public event PhysicsOverlap BeginOverlap, EndOverlap, OnHit;
        public event PhysicsEndContact OnContactEnded;
        public event DelMatrixUpdate TransformChanged;
        public event SimulationUpdate SimulationStateChanged;

        public PhysicsDriver(ICollidable owner, RigidBodyConstructionInfo info)
        {
            _owner = owner;
            _collisionEnabled = info.CollisionEnabled;
            _simulatingPhysics = info.SimulatePhysics;
            _group = info.CollisionGroup;
            _collidesWith = info.CollidesWith;
            CollisionObject = Engine.Physics.NewRigidBody(info);
        }
        public PhysicsDriver(ICollidable owner, RigidBodyConstructionInfo info, DelMatrixUpdate func)
            : this(owner, info)
            => TransformChanged += func;
        public PhysicsDriver(ICollidable owner, RigidBodyConstructionInfo info, DelMatrixUpdate mtxFunc, SimulationUpdate simFunc)
            : this(owner, info, mtxFunc)
            => SimulationStateChanged += simFunc;

        public PhysicsDriver(ICollidable owner, SoftBodyConstructionInfo info)
        {
            _owner = owner;
            _collisionEnabled = info.CollisionEnabled;
            _simulatingPhysics = info.SimulatePhysics;
            _group = (uint)info.CollisionGroup;
            _collidesWith = (uint)info.CollidesWith;
            CollisionObject = Engine.Physics.NewSoftBody(info);
        }
        public PhysicsDriver(ICollidable owner, SoftBodyConstructionInfo info, DelMatrixUpdate func)
            : this(owner, info)
            => TransformChanged += func;
        public PhysicsDriver(ICollidable owner, SoftBodyConstructionInfo info, DelMatrixUpdate mtxFunc, SimulationUpdate simFunc)
            : this(owner, info, mtxFunc)
            => SimulationStateChanged += simFunc;

        private Vector3
            _previousLinearFactor = Vector3.One,
            _previousAngularFactor = Vector3.One;
        bool _sleepingEnabled = true;

        private bool _collisionEnabled;
        private bool _simulatingPhysics;
        private bool _isSpawned;
        private uint _group;
        private uint _collidesWith;

        private ICollidable _owner;
        private XRCollisionObject _collision;
        private ThreadSafeList<PhysicsDriver> _overlapping = [];

        [Browsable(false)]
        public ICollidable Owner => _owner;
        [Browsable(false)]
        public ThreadSafeList<PhysicsDriver> Overlapping => _overlapping;
        [Browsable(false)]
        public XRCollisionObject CollisionObject
        {
            get => _collision;
            set
            {
                if (_collision == value)
                    return;
                bool wasInWorld = false;
                if (_collision != null)
                {
                    //wasInWorld = _collision.IsInWorld;
                    //if (wasInWorld && World != null)
                    //{
                    //    World.PhysicsScene.RemoveCollisionObject(_collision);
                    //    if (_simulatingPhysics)
                    //        UnregisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
                    //}
                    //_collision.UserObject = null;
                }
                _collision = value;
                if (_collision != null)
                {
                    //_collision.UserObject = this;

                    //if (_collisionEnabled)
                    //    _collision.CollisionFlags &= ~CollisionFlags.NoContactResponse;
                    //else
                    //    _collision.CollisionFlags |= CollisionFlags.NoContactResponse;

                    //_collision.CollisionFlags |= CollisionFlags.CustomMaterialCallback;

                    if (!_simulatingPhysics)
                    {
                        //_collision.LinearFactor = new Vector3(0.0f);
                        //_collision.AngularFactor = new Vector3(0.0f);
                        _collision.ForceActivationState(EBodyActivationState.DisableSimulation);
                    }
                    else
                    {
                        //_collision.LinearFactor = _previousLinearFactor;
                        //_collision.AngularFactor = _previousAngularFactor;
                        _collision.ForceActivationState(_sleepingEnabled ? EBodyActivationState.Active : EBodyActivationState.DisableSleep);
                    }

                    if (wasInWorld && _isSpawned && World != null)
                    {
                        //World.PhysicsScene.AddRigidBody(_collision, (short)CollisionGroup, (short)CollidesWith);
                        if (_simulatingPhysics)
                            RegisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
                    }
                }
            }
        }
        [Category("Physics Driver")]
        public bool SimulatingPhysics
        {
            get => _simulatingPhysics;
            set
            {
                if (_simulatingPhysics == value)
                    return;
                _simulatingPhysics = value;
                if (_collision != null)
                {
                    if (!_simulatingPhysics)
                    {
                        //_collision.LinearFactor = new Vector3(0.0f);
                        //_collision.AngularFactor = new Vector3(0.0f);
                        _collision.ForceActivationState(EBodyActivationState.DisableSimulation);
                        if (_isSpawned)
                            UnregisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
                    }
                    else
                    {
                        //_collision.LinearFactor = _previousLinearFactor;
                        //_collision.AngularFactor = _previousAngularFactor;
                        SetPhysicsTransform(_owner.CollidableWorldMatrix);
                        _collision.ForceActivationState(_sleepingEnabled ? EBodyActivationState.Active : EBodyActivationState.DisableSleep);
                        if (_isSpawned)
                            RegisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
                    }
                }
                SimulationStateChanged?.Invoke(_simulatingPhysics);
            }
        }
        //[Category("Physics Driver")]
        //public bool CollisionEnabled
        //{
        //    get => _collisionEnabled;
        //    set
        //    {
        //        if (_collisionEnabled == value)
        //            return;
        //        _collisionEnabled = value;

        //        if (_collisionEnabled)
        //            _collision.CollisionFlags &= ~CollisionFlags.NoContactResponse;
        //        else
        //            _collision.CollisionFlags |= CollisionFlags.NoContactResponse;

        //        if (_collision != null && _collision.IsInWorld)
        //            _collision.BroadphaseProxy.CollisionFilterMask = (CollisionFilterGroups)(short)(_collisionEnabled ? _collidesWith : (ushort)ECollisionGroup.None);
        //    }
        //}
        //[Category("Physics Driver")]
        //public bool Kinematic
        //{
        //    get => _collision is null ? false : _collision.CollisionFlags.HasFlag(CollisionFlags.KinematicObject);
        //    set
        //    {
        //        if (_collision is null)
        //            return;

        //        if (value)
        //            _collision.CollisionFlags |= CollisionFlags.KinematicObject;
        //        else
        //            _collision.CollisionFlags &= ~CollisionFlags.KinematicObject;
        //    }
        //}
        //[Category("Physics Driver")]
        //public bool Static
        //{
        //    get => _collision is null ? false : _collision.CollisionFlags.HasFlag(CollisionFlags.StaticObject);
        //    set
        //    {
        //        if (_collision is null)
        //            return;

        //        if (value)
        //            _collision.CollisionFlags |= CollisionFlags.StaticObject;
        //        else
        //            _collision.CollisionFlags &= ~CollisionFlags.StaticObject;
        //    }
        //}
        //[Category("Physics Driver")]
        //public uint CollisionGroup
        //{
        //    get => _group;
        //    set
        //    {
        //        if (_group == value)
        //            return;
        //        _group = value;
        //        if (_collision != null && _collision.IsInWorld)
        //            _collision.BroadphaseProxy.CollisionFilterGroup = (CollisionFilterGroups)(short)_group;
        //    }
        //}
        //[Category("Physics Driver")]
        //public uint CollidesWith
        //{
        //    get => _collidesWith;
        //    set
        //    {
        //        if (_collidesWith == value)
        //            return;
        //        _collidesWith = value;
        //        if (_collision != null && _collision.IsInWorld)
        //            _collision.BroadphaseProxy.CollisionFilterMask = (CollisionFilterGroups)(short)(_collisionEnabled ? _collidesWith : TCollisionGroup.None);
        //    }
        //}
        [Category("Physics Driver")]
        public Vector3 LinearFactor
        {
            get => _previousLinearFactor;
            set => _previousLinearFactor = value;
        }
        [Category("Physics Driver")]
        public Vector3 AngularFactor
        {
            get => _previousAngularFactor;
            set => _previousAngularFactor = value;
        }
        [Category("Physics Driver")]
        public bool SleepingEnabled
        {
            get => _sleepingEnabled;
            set
            {
                _sleepingEnabled = value;
                if (_collision.ActivationState != EBodyActivationState.DisableSimulation)
                {
                    if (_sleepingEnabled)
                    {
                        if (_collision.ActivationState == EBodyActivationState.DisableSleep)
                        {
                            _collision.ActivationState = EBodyActivationState.Active;
                        }
                    }
                    else
                    {
                        _collision.ActivationState = EBodyActivationState.DisableSleep;
                    }
                }
            }
        }

        public void OnSpawned()
        {
            _isSpawned = true;
            if (_collision is null)
                return;

            //World?.PhysicsScene.AddCollisionObject(_collision, (short)_group, _collisionEnabled ? (short)_collidesWith : (short)ECollisionGroup.None);

            if (_simulatingPhysics)
                RegisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
        }
        public void OnDespawned()
        {
            _isSpawned = false;
            if (_collision is null)
                return;

            //if (_collision.IsInWorld && World != null)
                World?.PhysicsScene?.RemoveActor(_collision);

            if (_simulatingPhysics)
                UnregisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
        }
        //internal void AddToWorld()
        //{
        //    Engine.World.PhysicsScene.AddRigidBody(
        //        _collision, 
        //        (short)_group,
        //        _collisionEnabled ? (short)_collidesWith : (short)CustomCollisionGroup.None);
        //}
        internal virtual void SetPhysicsTransform(Matrix4x4 newTransform)
        {
            //_collision.ProceedToTransform(newTransform);

            //Vector3 vel = _collision.LinearVelocity;
            //_collision.LinearVelocity = new Vector3(0.0f);
            //_collision.WorldTransform = newTransform;
            //_collision.CenterOfMassTransform = newTransform;
            //_collision.MotionState.WorldTransform = newTransform;
            //_collision.ClearForces();
            //_collision.LinearVelocity = vel;
        }
        protected internal /*async*/ void Tick()
        {
            _collision.GetWorldTransform(out Matrix4x4 transform);
            /*await Task.Run(() => */TransformChanged?.Invoke(transform)/*)*/;
        }

        //internal void InvokeHit(ICollidable other, ManifoldPoint cp)
        //{
        //    OnHit?.Invoke(Owner, other, cp);
        //}
        //internal void InvokeBeginOverlap(ICollidable other, ManifoldPoint cp)
        //{
        //    BeginOverlap?.Invoke(Owner, other, cp);
        //}
        //internal void InvokeEndOverlap(ICollidable other, ManifoldPoint cp)
        //{
        //    EndOverlap?.Invoke(Owner, other, cp);
        //}

        //internal void ContactStarted(PhysicsDriver other, ManifoldPoint cp)
        //{
        //    bool thisCollides = (CollidesWith & other.CollisionGroup) == other.CollisionGroup;
        //    bool thatCollides = (other.CollidesWith & CollisionGroup) == CollisionGroup;
        //    if (thisCollides || thatCollides)
        //    {
        //        InvokeHit(other.Owner, cp);
        //        other.InvokeHit(Owner, cp);
        //    }
        //    else
        //    {
        //        _overlapping.Add(other);
        //        other._overlapping.Add(this);

        //        InvokeBeginOverlap(other.Owner, cp);
        //        other.InvokeBeginOverlap(Owner, cp);
        //    }
        //}

        internal void ContactEnded(PhysicsDriver other)
        {
            OnContactEnded?.Invoke(Owner, other.Owner);
            _overlapping.Remove(other);
            other._overlapping.Remove(this);
        }
    }
}
