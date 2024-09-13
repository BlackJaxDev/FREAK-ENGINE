using System.Numerics;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Rotates around the parent transform about the local Y axis.
    /// </summary>
    /// <param name="parent"></param>
    public class OrbitTransform(TransformBase? parent = null) : TransformBase(parent)
    {
        private float _angle = 0.0f;
        private float _radius = 1.0f;

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
            => Matrix4x4.CreateTranslation(new Vector3(Radius, 0, 0)) * Matrix4x4.CreateRotationY(Angle);
    }
}