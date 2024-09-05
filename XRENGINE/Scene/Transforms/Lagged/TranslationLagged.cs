using System.Numerics;
using XREngine.Data;
using XREngine.Scene.Transforms;

namespace XREngine.Components.Scene.Transforms
{
    /// <summary>
    /// Moves the scene node to the parent's position + an offset in local space, lagging behind by a specified amount for smooth movement.
    /// </summary>
    public class TranslationLaggedComponent : TransformBase
    {
        public TranslationLaggedComponent(TransformBase? parent) : this(parent, Vector3.Zero) { }
        public TranslationLaggedComponent(TransformBase? parent, Vector3 translation) : base(parent)
        {
            _currentTranslation = translation;
            MarkLocalModified();
        }

        protected Vector3 _currentTranslation;
        protected Vector3 _desiredTranslation;
        protected float _invTransInterpSec = 40.0f;

        public Vector3 CurrentTranslation
        {
            get => _currentTranslation;
            set => SetField(ref _currentTranslation, value);
        }
        public Vector3 DesiredTranslation
        {
            get => _desiredTranslation;
            set => SetField(ref _desiredTranslation, value);
        }
        public float InverseTranslationInterpSeconds
        {
            get => _invTransInterpSec;
            set => _invTransInterpSec = value;
        }

        protected internal override void Start()
            => RegisterTick(ETickGroup.DuringPhysics, (int)ETickOrder.Logic, Tick);
        protected internal override void Stop()
            => UnregisterTick(ETickGroup.DuringPhysics, (int)ETickOrder.Logic, Tick);

        protected virtual void Tick()
        {
            _currentTranslation = Interp.CosineTo(_currentTranslation, _desiredTranslation, Engine.SmoothedDilatedDelta, _invTransInterpSec);
            MarkLocalModified();
        }
        protected override Matrix4x4 CreateLocalMatrix()
            => Matrix4x4.CreateTranslation(_currentTranslation);

        public void SetTranslation(Vector3 translation)
        {
            _desiredTranslation = translation;
            _currentTranslation = translation;
        }
    }
}
