using System.Numerics;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Rotates around the parent transform about the local Y axis.
    /// </summary>
    /// <param name="parent"></param>
    public class OrbitTransform : TransformBase
    {
        public OrbitTransform() { }
        public OrbitTransform(TransformBase? parent)
            : base(parent) { }

        private float _angle = 0.0f;
        private float _radius = 1.0f;
        private bool _ignoreRotation = false;

        public float Radius
        {
            get => _radius;
            set => SetField(ref _radius, value);
        }

        public float Angle
        {
            get => _angle;
            set => SetField(ref _angle, value);
        }

        public bool IgnoreRotation 
        {
            get => _ignoreRotation;
            set => SetField(ref _ignoreRotation, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Radius):
                case nameof(Angle):
                    MarkLocalModified();
                    break;
            }
        }

        protected override Matrix4x4 CreateLocalMatrix()
        {
            var mtx = Matrix4x4.CreateTranslation(new Vector3(0, 0, Radius)) * Matrix4x4.CreateRotationY(Angle);
            return IgnoreRotation ? Matrix4x4.CreateTranslation(mtx.Translation) : mtx;
        }
    }
}