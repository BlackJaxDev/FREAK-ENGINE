using System.Numerics;
using XREngine.Data;
using XREngine.Data.Core;
using XREngine.Data.Transforms.Rotations;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Scene.Transforms
{
    /// <summary>
    /// Moves the scene node to the parent's position + an offset and rotation + an offset in local space, lagging behind by a specified amount for smooth movement.
    /// </summary>
    public class TranslationRotationLaggedTransform : LaggedTranslationTransform
    {
        public TranslationRotationLaggedTransform()
            : this(null) { }
        public TranslationRotationLaggedTransform(TransformBase? parent)
            : this(parent, Vector3.Zero, Quaternion.Identity) { }
        public TranslationRotationLaggedTransform(TransformBase? parent, Vector3 translation, Quaternion rotation)
            : base(parent, translation)
        {
            _currentRotation = rotation;
            _desiredRotation = rotation;
        }
        public TranslationRotationLaggedTransform(TransformBase? parent, Vector3 translation)
            : base(parent, translation)
        {
            _currentRotation = Quaternion.Identity;
            _desiredRotation = Quaternion.Identity;
        }
        public TranslationRotationLaggedTransform(TransformBase? parent, Quaternion rotation)
            : base(parent)
        {
            _currentRotation = rotation;
            _desiredRotation = rotation;
        }

        protected Quaternion _currentRotation;
        protected Quaternion _desiredRotation;
        protected float _invRotInterpSec = 40.0f;

        public Quaternion DesiredRotation
        {
            get => _desiredRotation;
            set => SetField(ref _desiredRotation, value);
        }
        public Quaternion CurrentRotation
        {
            get => _currentRotation;
            set => SetField(ref _currentRotation, value);
        }
        public float InverseRotationInterpSeconds
        {
            get => _invRotInterpSec;
            set => SetField(ref _invRotInterpSec, value);
        }

        protected override void Tick()
        {
            _currentRotation = Interp.Slerp(_currentRotation, _desiredRotation, Engine.SmoothedDilatedDelta * _invRotInterpSec);
            base.Tick();
        }

        public void Set(Vector3 translation, Quaternion rotation)
        {
            _currentTranslation = _desiredTranslation = translation;
            _currentRotation = rotation;
            _desiredRotation = rotation;
        }
        public void SetRotation(Quaternion rotation)
        {
            _desiredRotation = rotation;
            _currentRotation = rotation;
        }

        protected override Matrix4x4 CreateLocalMatrix()
            => Matrix4x4.CreateTranslation(_currentTranslation) * Matrix4x4.CreateFromQuaternion(_currentRotation);

        public void TranslateRelative(float x, float y, float z)
            => TranslateRelative(new Vector3(x, y, z));

        public void TranslateRelative(Vector3 translation)
            => _desiredTranslation = (Matrix4x4.CreateTranslation(_desiredTranslation) * Matrix4x4.CreateFromQuaternion(_desiredRotation) * Matrix4x4.CreateTranslation(translation)).Translation;

        public void Pivot(float pitch, float yaw, float distance)
            => Pivot(pitch, yaw, _desiredTranslation + GetDesiredForwardDir() * distance);
        public void Pivot(float pitch, float yaw, Vector3 focusPoint)
        {
            //"Arcball" rotation
            //All rotation is done within local component space
            _desiredTranslation = XRMath.ArcballTranslation(pitch, yaw, focusPoint, _desiredTranslation, GetDesiredRightDir());
            _desiredRotation *= new Rotator(pitch, yaw, 0.0f).ToQuaternion();
        }

        public Vector3 GetDesiredRightDir()
            => Vector3.Transform(Globals.Right, _desiredRotation);
        public Vector3 GetDesiredUpDir()
            => Vector3.Transform(Globals.Up, _desiredRotation);
        public Vector3 GetDesiredForwardDir()
            => Vector3.Transform(Globals.Forward, _desiredRotation);
    }
}
