using Extensions;
using System.Numerics;
using XREngine.Animation;
using XREngine.Data.Core;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Scene
{
    public class Spline3DTransform : TransformBase
    {
        private PropAnimVector3? _spline = null;
        private float _animationSecond = 0.0f;
        private bool _animateOnActivate = true;
        private bool _loop = false;

        public Spline3DTransform() : base() { }
        public Spline3DTransform(PropAnimVector3? spline) : base() => Spline = spline;

        protected internal override void OnSceneNodeActivated()
        {
            base.OnSceneNodeActivated();
            if (AnimateOnActivate)
                StartAnimation();
        }

        public void StartAnimation()
            => RegisterTick(ETickGroup.Normal, ETickOrder.Animation, Tick);
        public void StopAnimation()
            => UnregisterTick(ETickGroup.Normal, ETickOrder.Animation, Tick);

        public PropAnimVector3? Spline
        {
            get => _spline;
            set => SetField(ref _spline, value);
        }
        public float AnimationSecond
        {
            get => _animationSecond;
            set => SetField(ref _animationSecond, value);
        }
        public bool Loop
        {
            get => _loop;
            set => SetField(ref _loop, value);
        }
        public bool AnimateOnActivate
        {
            get => _animateOnActivate;
            set => SetField(ref _animateOnActivate, value);
        }

        public Vector3 CurrentPosition => Spline?.GetValue(AnimationSecond) ?? Vector3.Zero;
        public Vector3 CurrentVelocity => Spline?.GetVelocityKeyframed(AnimationSecond) ?? Vector3.Zero;
        public Vector3 CurrentAcceleration => Spline?.GetAccelerationKeyframed(AnimationSecond) ?? Vector3.Zero;

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(AnimationSecond):
                    MarkLocalModified();
                    break;
            }
        }

        private void Tick()
        {
            var spline = Spline;
            if (spline is null)
                return;

            AnimationSecond += Engine.Delta;
            if (AnimationSecond <= spline.LengthInSeconds)
                return;
            
            if (Loop)
                AnimationSecond -= spline.LengthInSeconds;
            else
            {
                AnimationSecond = spline.LengthInSeconds;
                StopAnimation();
            }
        }

        public bool RotateToVelocity { get; set; } = false;
        public bool SmoothRotation { get; set; } = false;
        public float RotationSpeed { get; set; } = 1.0f;
        public float Roll { get; set; } = 0.0f;

        protected override Matrix4x4 CreateLocalMatrix()
        {
            var parentUp = Parent?.WorldUp ?? Globals.Up;
            var parentForward = Parent?.WorldForward ?? Globals.Forward;
            if (RotateToVelocity)
            {
                var forward = CurrentVelocity;
                if (forward.LengthSquared() > 0.0f)
                {
                    forward = forward.Normalized();

                    Vector3 right = forward.Dot(parentUp) > 0.99f
                        ? Vector3.Cross(forward, parentForward).Normalized()
                        : Vector3.Cross(forward, parentUp).Normalized();

                    if (Roll != 0.0f)
                        right = Vector3.TransformNormal(right, Matrix4x4.CreateFromAxisAngle(forward, XRMath.DegToRad(Roll)));

                    var up = Vector3.Cross(right, forward).Normalized();
                    var matrix = Matrix4x4.CreateWorld(CurrentPosition, forward, up);

                    if (SmoothRotation)
                        matrix = Matrix4x4.Lerp(LocalMatrix, matrix, RotationSpeed * Engine.Delta);

                    return matrix;
                }
            }

            return Matrix4x4.CreateWorld(CurrentPosition, parentForward, parentUp);
        }
    }
}