using Extensions;
using JoltPhysicsSharp;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using XREngine.Data;
using XREngine.Data.Colors;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Data.Rendering;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene.Components.Animation;
using XREngine.Scene.Transforms;
using static XREngine.Engine;

namespace XREngine.Components;

public class PhysicsChainComponent : XRComponent, IRenderable
{
    public Transform? _root = null;
    public List<Transform>? _roots = null;
    public float _updateRate = 60.0f;

    public enum EUpdateMode
    {
        Normal,
        FixedUpdate,
        Undilated,
        Default
    }

    public EUpdateMode _updateMode = EUpdateMode.Default;

    [Range(0, 1)]
    public float _damping = 0.1f;
    public AnimationCurve? _dampingDistrib = null;

    [Range(0, 1)]
    public float _elasticity = 0.1f;
    public AnimationCurve? _elasticityDistrib = null;

    [Range(0, 1)]
    public float _stiffness = 0.1f;
    public AnimationCurve? _stiffnessDistrib = null;

    [Range(0, 1)]
    public float _inert = 0.0f;
    public AnimationCurve? _inertDistrib = null;

    public float _friction = 0;
    public AnimationCurve? _frictionDistrib = null;

    public float _radius = 0.01f;
    public AnimationCurve? _radiusDistrib = null;

    public float _endLength = 0.0f;

    public Vector3 _endOffset = new(0.0f, 0.0f, 0.0f);
    public Vector3 _gravity = new(0.0f, 0.0f, 0.0f);
    public Vector3 _force = Vector3.Zero;

    [Range(0, 1)]
    public float _blendWeight = 1.0f;

    public List<PhysicsChainColliderBase>? _colliders = null;
    public List<TransformBase>? _exclusions = null;

    public enum EFreezeAxis
    {
        None,
        X,
        Y,
        Z
    }

    public EFreezeAxis _freezeAxis = EFreezeAxis.None;

    public bool _distantDisable = false;
    public Transform? _referenceObject = null;
    public float _distanceToObject = 20;

    [HideInInspector]
    public bool _multithread = true;

    private Vector3 _objectMove;
    private Vector3 _objectPrevPosition;
    private float _objectScale;

    private float _time = 0;
    private float _weight = 1.0f;
    private bool _distantDisabled = false;
    private int _preUpdateCount = 0;

    private class Particle(Transform? transform, int parentIndex)
    {
        public Transform? _transform = transform;
        public int _parentIndex = parentIndex;
        public int _childCount;
        public float _damping;
        public float _elasticity;
        public float _stiffness;
        public float _inert;
        public float _friction;
        public float _radius = 0.01f;
        public float _boneLength;
        public bool _isCollide;

        public Vector3 _position;
        public Vector3 _prevPosition;
        public Vector3 _endOffset;
        public Vector3 _initLocalPosition;
        public Quaternion _initLocalRotation;

        public Vector3 _transformPosition;
        public Vector3 _transformLocalPosition;
        public Matrix4x4 _transformLocalToWorldMatrix;
    }

    class ParticleTree(Transform root)
    {
        public Transform _root = root;
        public Vector3 _localGravity;
        public Matrix4x4 _rootWorldToLocalMatrix = root.InverseWorldMatrix;
        public float _boneTotalLength;
        public List<Particle> _particles = [];

        public Vector3 _restGravity;
    }

    private readonly List<ParticleTree> _particleTrees = [];

    // prepare data
    private float _deltaTime;
    private List<PhysicsChainColliderBase>? _effectiveColliders;

    // multithread
    private bool _workAdded = false;

    static List<PhysicsChainComponent> _pendingWorks = [];
    static List<PhysicsChainComponent> _effectiveWorks = [];
    static AutoResetEvent? _allWorksDoneEvent;
    static int _remainWorkCount;
    static Semaphore? _workQueueSemaphore;
    static int _workQueueIndex;

    static int _updateCount;
    static int _PrepareFrame;

    public PhysicsChainComponent()
    {
        RenderedObjects =
        [
            RenderInfo3D.New(this, new RenderCommandMethod3D((int)EDefaultRenderPass.OpaqueForward, RenderGizmos))
        ];
    }

    public RenderInfo[] RenderedObjects { get; }

    protected internal override void OnComponentActivated()
    {
        SetupParticles();
        RegisterTick(ETickGroup.PostPhysics, ETickOrder.Animation, FixedUpdate);
        RegisterTick(ETickGroup.Normal, ETickOrder.Animation, Update);
        RegisterTick(ETickGroup.Late, ETickOrder.Animation, LateUpdate);
        OnEnable();
        OnValidate();
    }
    protected internal override void OnComponentDeactivated()
    {
        base.OnComponentDeactivated();
        OnDisable();
    }

    void FixedUpdate()
    {
        if (_updateMode == EUpdateMode.FixedUpdate)
            PreUpdate();
    }

    void Update()
    {
        if (_updateMode != EUpdateMode.FixedUpdate)
            PreUpdate();
        
        if (_preUpdateCount > 0 && _multithread)
        {
            AddPendingWork(this);
            _workAdded = true;
        }

        ++_updateCount;
    }

    void LateUpdate()
    {
        if (_preUpdateCount == 0)
            return;

        if (_updateCount > 0)
        {
            _updateCount = 0;
            ++_PrepareFrame;
        }

        SetWeight(_blendWeight);

        if (_workAdded)
        {
            _workAdded = false;
            ExecuteWorks();
        }
        else
        {
            CheckDistance();
            if (IsNeedUpdate())
            {
                Prepare();
                UpdateParticles();
                ApplyParticlesToTransforms();
            }
        }

        _preUpdateCount = 0;
    }

    void Prepare()
    {
        _deltaTime = Delta;
        switch (_updateMode)
        {
            case EUpdateMode.Undilated:
                _deltaTime = UndilatedDelta;
                break;
            case EUpdateMode.FixedUpdate:
                _deltaTime = FixedDelta * _preUpdateCount;
                break;
        }

        _objectScale = MathF.Abs(Transform.LossyWorldScale.X);
        _objectMove = Transform.WorldTranslation - _objectPrevPosition;
        _objectPrevPosition = Transform.WorldTranslation;

        for (int i = 0; i < _particleTrees.Count; ++i)
        {
            ParticleTree pt = _particleTrees[i];
            pt._restGravity = pt._root.TransformDirection(pt._localGravity);

            for (int j = 0; j < pt._particles.Count; ++j)
            {
                Particle p = pt._particles[j];
                if (p._transform is not null)
                {
                    p._transformPosition = p._transform.WorldTranslation;
                    p._transformLocalPosition = p._transform.LocalTranslation;
                    p._transformLocalToWorldMatrix = p._transform.WorldMatrix;
                }
            }
        }

        _effectiveColliders?.Clear();

        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Count; ++i)
            {
                PhysicsChainColliderBase c = _colliders[i];
                if (c != null && c.IsActive)
                {
                    (_effectiveColliders ??= []).Add(c);
                    if (c.PrepareFrame != _PrepareFrame)
                    {
                        c.Prepare();
                        c.PrepareFrame = _PrepareFrame;
                    }
                }
            }
        }
    }

    bool IsNeedUpdate()
        => _weight > 0 && !(_distantDisable && _distantDisabled);

    void PreUpdate()
    {
        if (IsNeedUpdate())
            InitTransforms();
        
        ++_preUpdateCount;
    }

    void CheckDistance()
    {
        if (!_distantDisable)
            return;

        TransformBase? rt = _referenceObject;
        if (rt is null)
        {
            XRCamera? c = State.MainPlayer.ControlledPawn?.CameraComponent?.Camera;
            if (c != null)
                rt = c.Transform;
        }

        if (rt != null)
        {
            float d2 = (rt.WorldTranslation - Transform.LocalTranslation).LengthSquared();
            bool disable = d2 > _distanceToObject * _distanceToObject;
            if (disable != _distantDisabled)
            {
                if (!disable)
                    ResetParticlesPosition();
                _distantDisabled = disable;
            }
        }
    }

    void OnEnable()
    {
        ResetParticlesPosition();
    }

    void OnDisable()
    {
        InitTransforms();
    }

    protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
    {
        base.OnPropertyChanged(propName, prev, field);
        OnValidate();
    }

    void OnValidate()
    {
        _updateRate = MathF.Max(_updateRate, 0);
        _damping = _damping.Clamp(0, 1);
        _elasticity = _elasticity.Clamp(0, 1);
        _stiffness = _stiffness.Clamp(0, 1);
        _inert = _inert.Clamp(0, 1);
        _friction = _friction.Clamp(0, 1);
        _radius = MathF.Max(_radius, 0);

        if (IsEditor && IsPlaying)
        {
            if (IsRootChanged())
            {
                InitTransforms();
                SetupParticles();
            }
            else
                UpdateParameters();
        }
    }

    bool IsRootChanged()
    {
        var roots = new List<Transform>();
        if (_root != null)
            roots.Add(_root);
        
        if (_roots != null)
            foreach (var root in _roots)
                if (root != null && !roots.Contains(root))
                    roots.Add(root);

        if (roots.Count == 0)
            roots.Add(SceneNode.GetTransformAs<Transform>(true)!);

        if (roots.Count != _particleTrees.Count)
            return true;

        for (int i = 0; i < roots.Count; ++i)
            if (roots[i] != _particleTrees[i]._root)
                return true;
        
        return false;
    }

    void OnDidApplyAnimationProperties()
    {
        UpdateParameters();
    }

    void RenderGizmos(bool shadowPass)
    {
        if (!IsActive || shadowPass)
            return;

        if (IsEditor && !IsPlaying && Transform.HasChanged)
        {
            //InitTransforms();
            SetupParticles();
        }

        for (int i = 0; i < _particleTrees.Count; ++i)
            DrawGizmos(_particleTrees[i]);
    }

    void DrawGizmos(ParticleTree pt)
    {
        for (int i = 0; i < pt._particles.Count; ++i)
        {
            Particle p = pt._particles[i];
            if (p._parentIndex >= 0)
            {
                Particle p0 = pt._particles[p._parentIndex];
                Engine.Rendering.Debug.RenderLine(p._position, p0._position, ColorF4.Orange, false);
            }
            if (p._radius > 0)
            {
                float radius = p._radius * _objectScale;
                Engine.Rendering.Debug.RenderSphere(p._position, radius, false, ColorF4.Yellow, false);
            }
        }
    }

    public void SetWeight(float w)
    {
        if (_weight == w)
            return;
        
        if (w == 0)
            InitTransforms();
        else if (_weight == 0)
            ResetParticlesPosition();

        _weight = _blendWeight = w;
    }

    public float GetWeight()
        => _weight;

    void UpdateParticles()
    {
        if (_particleTrees.Count <= 0)
            return;

        int loop = 1;
        float timeVar = 1.0f;
        float dt = _deltaTime;

        if (_updateMode == EUpdateMode.Default)
        {
            if (_updateRate > 0.0f)
                timeVar = dt * _updateRate;
        }
        else if (_updateRate > 0.0f)
        {
            float frameTime = 1.0f / _updateRate;
            _time += dt;
            loop = 0;

            while (_time >= frameTime)
            {
                _time -= frameTime;
                if (++loop >= 3)
                {
                    _time = 0;
                    break;
                }
            }
        }
        
        if (loop > 0)
        {
            for (int i = 0; i < loop; ++i)
            {
                UpdateParticles1(timeVar, i);
                UpdateParticles2(timeVar);
            }
        }
        else
            SkipUpdateParticles();
    }

    public void SetupParticles()
    {
        _particleTrees.Clear();

        if (_root != null)
            AppendParticleTree(_root);
        
        if (_roots != null)
        {
            for (int i = 0; i < _roots.Count; ++i)
            {
                Transform root = _roots[i];
                if (root == null)
                    continue;

                if (_particleTrees.Exists(x => x._root == root))
                    continue;

                AppendParticleTree(root);
            }
        }

        if (_particleTrees.Count == 0)
            AppendParticleTree(SceneNode.GetTransformAs<Transform>(true)!);

        _objectScale = MathF.Abs(Transform.LossyWorldScale.X);
        _objectPrevPosition = Transform.WorldTranslation;
        _objectMove = Vector3.Zero;

        for (int i = 0; i < _particleTrees.Count; ++i)
        {
            ParticleTree pt = _particleTrees[i];
            AppendParticles(pt, pt._root, -1, 0);
        }

        UpdateParameters();
    }

    void AppendParticleTree(Transform root)
    {
        if (root is null)
            return;

        _particleTrees.Add(new ParticleTree(root));
    }

    void AppendParticles(ParticleTree pt, Transform? b, int parentIndex, float boneLength)
    {
        var p = new Particle(b, parentIndex);

        if (b != null)
        {
            p._position = p._prevPosition = b.WorldTranslation;
            p._initLocalPosition = b.LocalTranslation;
            p._initLocalRotation = b.LocalRotation;
        }
        else //end bone
        {
            TransformBase? pb = pt._particles[parentIndex]._transform;
            if (pb != null)
            {
                if (_endLength > 0)
                {
                    TransformBase? ppb = pb.Parent;
                    p._endOffset = ppb != null
                        ? pb.InverseTransformPoint((pb.WorldTranslation * 2 - ppb.WorldTranslation)) * _endLength
                        : new Vector3(_endLength, 0, 0);
                }
                else
                    p._endOffset = pb.InverseTransformPoint(Transform.TransformDirection(_endOffset) + pb.WorldTranslation);
                
                p._position = p._prevPosition = pb.TransformPoint(p._endOffset);
            }
            p._initLocalPosition = Vector3.Zero;
            p._initLocalRotation = Quaternion.Identity;
        }

        if (parentIndex >= 0 && pt._particles[parentIndex]._transform is not null)
        {
            boneLength += (pt._particles[parentIndex]._transform!.WorldTranslation - p._position).Length();
            p._boneLength = boneLength;
            pt._boneTotalLength = MathF.Max(pt._boneTotalLength, boneLength);
            ++pt._particles[parentIndex]._childCount;
        }

        int index = pt._particles.Count;
        pt._particles.Add(p);

        if (b != null)
        {
            for (int i = 0; i < b.Children.Count; ++i)
            {
                TransformBase child = b.Children[i];

                bool exclude = false;
                if (_exclusions != null)
                    exclude = _exclusions.Contains(child);
                
                if (!exclude)
                    AppendParticles(pt, child as Transform, index, boneLength);
                else if (_endLength > 0 || _endOffset != Vector3.Zero)
                    AppendParticles(pt, null, index, boneLength);
            }

            if (b.Children.Count == 0 && (_endLength > 0 || _endOffset != Vector3.Zero))
                AppendParticles(pt, null, index, boneLength);
        }
    }

    public void UpdateParameters()
    {
        SetWeight(_blendWeight);
        for (int i = 0; i < _particleTrees.Count; ++i)
            UpdateParameters(_particleTrees[i]);
    }

    void UpdateParameters(ParticleTree pt)
    {
        // m_LocalGravity = m_Root.InverseTransformDirection(m_Gravity);
        pt._localGravity = Vector3.Transform(_gravity, pt._rootWorldToLocalMatrix).Normalized() * _gravity.Length();

        for (int i = 0; i < pt._particles.Count; ++i)
        {
            Particle p = pt._particles[i];
            p._damping = _damping;
            p._elasticity = _elasticity;
            p._stiffness = _stiffness;
            p._inert = _inert;
            p._friction = _friction;
            p._radius = _radius;

            if (pt._boneTotalLength > 0)
            {
                float a = p._boneLength / pt._boneTotalLength;
                if (_dampingDistrib != null && _dampingDistrib.Keyframes.Count > 0)
                    p._damping *= _dampingDistrib.Evaluate(a);
                if (_elasticityDistrib != null && _elasticityDistrib.Keyframes.Count > 0)
                    p._elasticity *= _elasticityDistrib.Evaluate(a);
                if (_stiffnessDistrib != null && _stiffnessDistrib.Keyframes.Count > 0)
                    p._stiffness *= _stiffnessDistrib.Evaluate(a);
                if (_inertDistrib != null && _inertDistrib.Keyframes.Count > 0)
                    p._inert *= _inertDistrib.Evaluate(a);
                if (_frictionDistrib != null && _frictionDistrib.Keyframes.Count > 0)
                    p._friction *= _frictionDistrib.Evaluate(a);
                if (_radiusDistrib != null && _radiusDistrib.Keyframes.Count > 0)
                    p._radius *= _radiusDistrib.Evaluate(a);
            }

            p._damping = p._damping.Clamp(0, 1);
            p._elasticity = p._elasticity.Clamp(0, 1);
            p._stiffness = p._stiffness.Clamp(0, 1);
            p._inert = p._inert.Clamp(0, 1);
            p._friction = p._friction.Clamp(0, 1);
            p._radius = MathF.Max(p._radius, 0);
        }
    }

    void InitTransforms()
    {
        for (int i = 0; i < _particleTrees.Count; ++i)
            InitTransforms(_particleTrees[i]);
    }

    private static void InitTransforms(ParticleTree pt)
    {
        for (int i = 0; i < pt._particles.Count; ++i)
        {
            Particle p = pt._particles[i];
            if (p._transform is null)
                continue;
            
            p._transform.Translation = p._initLocalPosition;
            p._transform.Rotation = p._initLocalRotation;
        }
    }

    void ResetParticlesPosition()
    {
        for (int i = 0; i < _particleTrees.Count; ++i)
            ResetParticlesPosition(_particleTrees[i]);

        _objectPrevPosition = Transform.WorldTranslation;
    }

    static void ResetParticlesPosition(ParticleTree pt)
    {
        for (int i = 0; i < pt._particles.Count; ++i)
        {
            Particle p = pt._particles[i];
            if (p._transform is not null)
                p._position = p._prevPosition = p._transform.WorldTranslation;
            else // end bone
            {
                Transform? pb = pt._particles[p._parentIndex]._transform;
                if (pb is not null)
                    p._position = p._prevPosition = pb.TransformPoint(p._endOffset);
            }
            p._isCollide = false;
        }
    }

    void UpdateParticles1(float timeVar, int loopIndex)
    {
        for (int i = 0; i < _particleTrees.Count; ++i)
            UpdateParticles1(_particleTrees[i], timeVar, loopIndex);
    }

    void UpdateParticles1(ParticleTree pt, float timeVar, int loopIndex)
    {
        Vector3 force = _gravity;
        Vector3 fdir = _gravity.Normalized();
        Vector3 pf = fdir * MathF.Max(Vector3.Dot(pt._restGravity, fdir), 0); // project current gravity to rest gravity
        force -= pf; // remove projected gravity
        force = (force + _force) * (_objectScale * timeVar);

        Vector3 objectMove = loopIndex == 0 ? _objectMove : Vector3.Zero; // only first loop consider object move

        for (int i = 0; i < pt._particles.Count; ++i)
        {
            Particle p = pt._particles[i];
            if (p._parentIndex >= 0)
            {
                // verlet integration
                Vector3 v = p._position - p._prevPosition;
                Vector3 rmove = objectMove * p._inert;
                p._prevPosition = p._position + rmove;
                float damping = p._damping;
                if (p._isCollide)
                {
                    damping += p._friction;
                    if (damping > 1)
                        damping = 1;
                    p._isCollide = false;
                }
                p._position += v * (1 - damping) + force + rmove;
            }
            else
            {
                p._prevPosition = p._position;
                p._position = p._transformPosition;
            }
        }
    }

    void UpdateParticles2(float timeVar)
    {
        for (int i = 0; i < _particleTrees.Count; ++i)
            UpdateParticles2(_particleTrees[i], timeVar);
    }

    void UpdateParticles2(ParticleTree pt, float timeVar)
    {
        for (int i = 1; i < pt._particles.Count; ++i)
        {
            Particle p = pt._particles[i];
            Particle p0 = pt._particles[p._parentIndex];

            float restLen = p._transform is not null
                ? (p0._transformPosition - p._transformPosition).Length()
                : (Vector3.Transform(p._endOffset, p0._transformLocalToWorldMatrix) - p0._transformLocalToWorldMatrix.Translation).Length();

            // keep shape
            float stiffness = Interp.Lerp(1.0f, p._stiffness, _weight);
            if (stiffness > 0 || p._elasticity > 0)
            {
                Matrix4x4 m0 = p0._transformLocalToWorldMatrix;
                m0.Translation = p0._position;
                Vector3 restPos = p._transform is not null 
                    ? Vector3.Transform(p._transformLocalPosition, m0)
                    : Vector3.Transform(p._endOffset, m0);
                Vector3 d = restPos - p._position;
                p._position += d * (p._elasticity * timeVar);

                if (stiffness > 0)
                {
                    d = restPos - p._position;
                    float len = d.Length();
                    float maxlen = restLen * (1.0f - stiffness) * 2.0f;
                    if (len > maxlen && len > 0.0f)
                        p._position += d * ((len - maxlen) / len);
                }
            }

            // collide
            if (_effectiveColliders != null)
            {
                float particleRadius = p._radius * _objectScale;
                for (int j = 0; j < _effectiveColliders.Count; ++j)
                {
                    PhysicsChainColliderBase c = _effectiveColliders[j];
                    p._isCollide |= c.Collide(ref p._position, particleRadius);
                }
            }

            // freeze axis, project to plane 
            if (_freezeAxis != EFreezeAxis.None)
            {
                Vector4 planeNormal = p0._transformLocalToWorldMatrix.GetColumn((int)_freezeAxis - 1).Normalized();
                Plane movePlane = XRMath.CreatePlaneFromPointAndNormal(p0._position, planeNormal.XYZ());
                p._position -= movePlane.Normal * GeoUtil.DistancePlanePoint(movePlane, p._position);
            }

            // keep length
            Vector3 dd = p0._position - p._position;
            float leng = dd.Length();
            if (leng > 0)
                p._position += dd * ((leng - restLen) / leng);
        }
    }

    void SkipUpdateParticles()
    {
        for (int i = 0; i < _particleTrees.Count; ++i)
            SkipUpdateParticles(_particleTrees[i]);
    }

    // only update stiffness and keep bone length
    void SkipUpdateParticles(ParticleTree pt)
    {
        for (int i = 0; i < pt._particles.Count; ++i)
        {
            Particle p = pt._particles[i];
            if (p._parentIndex >= 0)
            {
                p._prevPosition += _objectMove;
                p._position += _objectMove;

                Particle p0 = pt._particles[p._parentIndex];

                float restLen = p._transform is not null
                    ? (p0._transformPosition - p._transformPosition).Length()
                    : Vector3.Transform(p._endOffset, p0._transformLocalToWorldMatrix).Length();

                // keep shape
                float stiffness = Interp.Lerp(1.0f, p._stiffness, _weight);
                if (stiffness > 0)
                {
                    Matrix4x4 m0 = p0._transformLocalToWorldMatrix;
                    m0.Translation = p0._position;
                    Vector3 restPos = p._transform is not null 
                        ? Vector3.Transform(p._transformLocalPosition, m0)
                        : Vector3.Transform(p._endOffset, m0);
                    Vector3 d = restPos - p._position;
                    float len = d.Length();
                    float maxlen = restLen * (1 - stiffness) * 2;
                    if (len > maxlen)
                        p._position += d * ((len - maxlen) / len);
                }

                // keep length
                Vector3 dd = p0._position - p._position;
                float leng = dd.Length();
                if (leng > 0)
                    p._position += dd * ((leng - restLen) / leng);
            }
            else
            {
                p._prevPosition = p._position;
                p._position = p._transformPosition;
            }
        }
    }

    //static Vector3 MirrorVector(Vector3 v, Vector3 axis)
    //    => v - axis * (Vector3.Dot(v, axis) * 2);

    private void ApplyParticlesToTransforms()
    {
        for (int i = 0; i < _particleTrees.Count; ++i)
            ApplyParticlesToTransforms(_particleTrees[i]);
    }

    private static void ApplyParticlesToTransforms(ParticleTree pt)
    {
        for (int i = 1; i < pt._particles.Count; ++i)
        {
            Particle p = pt._particles[i];
            Particle p0 = pt._particles[p._parentIndex];

            if (p0._childCount <= 1 && p0._transform is not null) // do not modify bone orientation if has more then one child
            {
                Vector3 localPos = p._transform is not null
                    ? p._transform.LocalTranslation
                    : p._endOffset;

                Vector3 v0 = p0._transform.TransformDirection(localPos);
                Vector3 v1 = p._position - p0._position;
                Quaternion rot = XRMath.RotationBetweenVectors(v0, v1);
                p0._transform.SetWorldRotation(rot * p0._transform.WorldRotation);
            }

            p._transform?.SetWorldTranslation(p._position);
        }
    }

    static void AddPendingWork(PhysicsChainComponent db)
        => _pendingWorks.Add(db);

    static void AddWorkToQueue(PhysicsChainComponent db)
        => _workQueueSemaphore?.Release();

    static PhysicsChainComponent GetWorkFromQueue()
    {
        int idx = Interlocked.Increment(ref _workQueueIndex);
        return _effectiveWorks[idx];
    }

    static void ThreadProc()
    {
        while (true)
        {
            _workQueueSemaphore?.WaitOne();

            PhysicsChainComponent db = GetWorkFromQueue();
            db.UpdateParticles();

            if (Interlocked.Decrement(ref _remainWorkCount) <= 0)
                _allWorksDoneEvent?.Set();
        }
    }

    static void InitThreadPool()
    {
        _allWorksDoneEvent = new AutoResetEvent(false);
        _workQueueSemaphore = new Semaphore(0, int.MaxValue);

        int threadCount = Environment.ProcessorCount;

        for (int i = 0; i < threadCount; ++i)
        {
            var t = new Thread(ThreadProc)
            {
                IsBackground = true
            };
            t.Start();
        }
    }

    static void ExecuteWorks()
    {
        if (_pendingWorks.Count <= 0)
            return;

        _effectiveWorks.Clear();

        for (int i = 0; i < _pendingWorks.Count; ++i)
        {
            PhysicsChainComponent db = _pendingWorks[i];
            if (db != null && db.IsActive)
            {
                db.CheckDistance();
                if (db.IsNeedUpdate())
                    _effectiveWorks.Add(db);
            }
        }

        _pendingWorks.Clear();
        if (_effectiveWorks.Count <= 0)
            return;

        if (_allWorksDoneEvent == null)
            InitThreadPool();
        
        int workCount = _remainWorkCount = _effectiveWorks.Count;
        _workQueueIndex = -1;

        for (int i = 0; i < workCount; ++i)
        {
            PhysicsChainComponent db = _effectiveWorks[i];
            db.Prepare();
            AddWorkToQueue(db);
        }

        _allWorksDoneEvent?.WaitOne();

        for (int i = 0; i < workCount; ++i)
            _effectiveWorks[i].ApplyParticlesToTransforms();
    }
}
