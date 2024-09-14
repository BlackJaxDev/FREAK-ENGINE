using System.Numerics;
using XREngine.Data.Core;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    public class UIRotationComponent : UITransform
    {
        public UIRotationComponent() : this(null) { }
        public UIRotationComponent(TransformBase? parent) : base(parent) { }

        private float _rotationAngle = 0.0f;
        /// <summary>
        /// The rotation angle of the component in degrees.
        /// </summary>
        public float RotationAngle
        {
            get => _rotationAngle;
            set => SetField(ref _rotationAngle, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(RotationAngle):
                    MarkLocalModified();
                    break;
            }
        }

        protected override Matrix4x4 CreateLocalMatrix()
            => base.CreateLocalMatrix() * Matrix4x4.CreateRotationZ(XRMath.DegToRad(RotationAngle));
    }
}
