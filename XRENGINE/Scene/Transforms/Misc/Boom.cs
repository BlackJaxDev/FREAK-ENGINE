using Extensions;
using System.Drawing;
using System.Numerics;
using XREngine.Data;
using XREngine.Physics;
using XREngine.Physics.ShapeTracing;
using XREngine.Rendering;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Scene.Transforms
{
    /// <summary>
    /// Typical 3rd person boom camera transform, with a trace sphere to prevent clipping through walls.
    /// </summary>
    public class BoomTransform : TransformBase, IRenderable
    {
        public BoomTransform(TransformBase? parent) : base(parent)
        {
            _debugRC = new RenderCommandMethod3D(0, DebugRender);
            RenderInfo = new RenderInfo3D(this);
            RenderedObjects = [RenderInfo];
        }

        public delegate void DelDistanceChange(float newLength);
        public event DelDistanceChange? CurrentDistanceChanged;

        private readonly XRCollisionSphere _traceShape = XRCollisionSphere.New(0.3f);
        private float _currentLength = 0.0f;
        private Vector3 _startPoint = Vector3.Zero;
        private Quaternion _rotation;
        private Vector3 _translation;
        private float _interpSpeed = 15.0f;
        private float _maxLength = 300.0f;
        private XRCollisionObject? _ignoreCast = null;

        public float TraceRadius
        {
            get => _traceShape.Radius;
            set => _traceShape.Radius = value;
        }
        public float InterpolationSpeed
        {
            get => _interpSpeed;
            set => SetField(ref _interpSpeed, value);
        }
        public float MaxLength
        {
            get => _maxLength;
            set => SetField(ref _maxLength, value);
        }
        public XRCollisionObject? IgnoreCast
        {
            get => _ignoreCast;
            set => SetField(ref _ignoreCast, value);
        }
        public Quaternion Rotation
        {
            get => _rotation;
            set => SetField(ref _rotation, value);
        }
        public Vector3 Translation
        {
            get => _translation;
            set => SetField(ref _translation, value);
        }

        public RenderInfo[] RenderedObjects { get; }
        public RenderInfo3D RenderInfo { get; protected set; }

        private void DebugRender(bool shadowPass)
        {
            Engine.Rendering.Debug.RenderSphere(WorldTranslation, _traceShape.Radius, false, Color.Black);
            Engine.Rendering.Debug.RenderLine(ParentWorldMatrix.Translation, WorldTranslation, Color.Black);
        }

        protected override Matrix4x4 CreateLocalMatrix()
        {
            Matrix4x4 r = Matrix4x4.CreateFromQuaternion(Rotation);
            Matrix4x4 t = Matrix4x4.CreateTranslation(Translation);
            Matrix4x4 dist = Matrix4x4.CreateTranslation(0.0f, 0.0f, _currentLength);
            return r * t * dist;
        }

        protected internal override void Start()
        {
            base.Start();
            RegisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
        }
        protected internal override void Stop()
        {
            base.Stop();
            UnregisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
        }

        private void Tick()
        {
            Matrix4x4 startMatrix = ParentWorldMatrix * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Translation);
            _startPoint = startMatrix.Translation;
            Matrix4x4 endMatrix = startMatrix * Matrix4x4.CreateTranslation(new Vector3(0.0f, 0.0f, MaxLength));
            Vector3 testEnd = endMatrix.Translation;

            ShapeTraceClosest result = new(_traceShape, startMatrix, endMatrix, (ushort)ECollisionGroup.Camera, (ushort)ECollisionGroup.All, IgnoreCast);

            Vector3 newEndPoint;
            if (World?.PhysicsScene?.Trace(result) ?? false)
                newEndPoint = result.HitPointWorld;
            else
                newEndPoint = testEnd;
            float newLength = (newEndPoint - _startPoint).Length();
            if (!newLength.EqualTo(_currentLength, 0.001f))
            {
                if (newLength < _currentLength)
                    _currentLength = newLength; //Moving closer to the character, meaning something is obscuring the view. Need to jump to the right position.
                else //Nothing is now obscuring the view, so we can lerp out quickly to give the appearance of a clean camera zoom out
                    _currentLength = Interp.Lerp(_currentLength, newLength, Engine.Delta, 15.0f);

                MarkLocalModified();
                CurrentDistanceChanged?.Invoke(_currentLength);
            }
        }

        private readonly RenderCommandMethod3D _debugRC;
        public void AddRenderCommands(RenderCommandCollection passes, XRCamera camera)
            => passes.Add(_debugRC);
    }
}
