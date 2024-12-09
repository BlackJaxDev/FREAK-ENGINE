using Extensions;
using System.Drawing;
using System.Numerics;
using XREngine.Data;
using XREngine.Physics;
using XREngine.Physics.ShapeTracing;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Scene.Transforms
{
    /// <summary>
    /// Typical 3rd person boom camera transform, with a trace sphere to prevent clipping through walls.
    /// </summary>
    public class BoomTransform : TransformBase, IRenderable
    {
        public delegate void DelDistanceChange(float newLength);
        public event DelDistanceChange? CurrentDistanceChanged;

        private readonly XRCollisionSphere _traceShape = XRCollisionSphere.New(0.3f);
        private float _currentLength = 0.0f;
        private float _maxLength = 300.0f;
        private XRCollisionObject[] _ignoreCast = [];
        private float _zoomOutSpeed = 15.0f;

        /// <summary>
        /// How big the trace sphere is.
        /// Should be at least the size of the camera's near clip plane.
        /// </summary>
        public float TraceRadius
        {
            get => _traceShape.Radius;
            set => _traceShape.Radius = value;
        }

        /// <summary>
        /// Maximum length the boom can extend.
        /// </summary>
        public float MaxLength
        {
            get => _maxLength;
            set => SetField(ref _maxLength, value);
        }

        /// <summary>
        /// All objects to ignore when casting the trace sphere.
        /// </summary>
        public XRCollisionObject[] IgnoreCast
        {
            get => _ignoreCast;
            set => SetField(ref _ignoreCast, value);
        }

        /// <summary>
        /// How fast the boom zooms out when it's not obstructed.
        /// </summary>
        public float ZoomOutSpeed
        {
            get => _zoomOutSpeed;
            set => SetField(ref _zoomOutSpeed, value);
        }

        protected override void RenderDebug(bool shadowPass)
        {
            //base.RenderDebug(shadowPass);

            if (shadowPass)
                return;

            Engine.Rendering.Debug.RenderSphere(WorldTranslation, _traceShape.Radius, false, Color.Black);
            Engine.Rendering.Debug.RenderLine(ParentWorldMatrix.Translation, WorldTranslation, Color.Black);
        }

        protected override Matrix4x4 CreateLocalMatrix()
            => Matrix4x4.CreateTranslation(0.0f, 0.0f, _currentLength);

        protected internal override void OnSceneNodeActivated()
        {
            base.OnSceneNodeActivated();
            RegisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
        }
        protected internal override void OnSceneNodeDeactivated()
        {
            base.OnSceneNodeDeactivated();
            UnregisterTick(ETickGroup.PostPhysics, (int)ETickOrder.Scene, Tick);
        }

        private void Tick()
        {
            Matrix4x4 startMatrix = ParentWorldMatrix;
            var startPoint = startMatrix.Translation;
            Matrix4x4 endMatrix = startMatrix * Matrix4x4.CreateTranslation(new Vector3(0.0f, 0.0f, MaxLength));
            Vector3 testEnd = endMatrix.Translation;

            ShapeTraceClosest result = new(_traceShape, startMatrix, endMatrix, (ushort)ECollisionGroup.Camera, (ushort)ECollisionGroup.All, IgnoreCast);

            bool hasHit = World?.PhysicsScene?.Trace(result) ?? false;
            Vector3 newEndPoint = hasHit ? result.HitPointWorld : testEnd;
            float newLength = (newEndPoint - startPoint).Length();
            if (newLength.EqualTo(_currentLength, 0.001f))
                return;
            
            if (newLength < _currentLength)
                _currentLength = newLength; //Moving closer to the character, meaning something is obscuring the view. Need to jump to the right position.
            else //Nothing is now obscuring the view, so we can lerp out quickly to give the appearance of a clean camera zoom out
                _currentLength = Interp.Lerp(_currentLength, newLength, Engine.UndilatedDelta, ZoomOutSpeed);

            MarkLocalModified();
            CurrentDistanceChanged?.Invoke(_currentLength);
        }
    }
}
