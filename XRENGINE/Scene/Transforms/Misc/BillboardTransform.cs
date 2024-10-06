using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Rendering;

namespace XREngine.Scene.Transforms
{
    /// <summary>
    /// Represents a transform that always faces the camera.
    /// Calculates the rotation on the CPU.
    /// Use vertex shaders or set billboard parameters in render options on the mesh directly on meshes for better performance.
    /// </summary>
    public class BillboardTransform : TransformBase
    {
        public BillboardTransform() : this(null) { }
        public BillboardTransform(TransformBase? parent) : base(parent)
            => XRCamera.CurrentRenderTargetChanged.AddListener(OnCameraChanged);

        private void OnCameraChanged(XRCamera? camera)
            => MarkWorldModified();

        private bool _perspective = false;
        /// <summary>
        /// If perspective is true, the billboard will face towards the camera's position.
        /// If perspective is false, the billboard will face towards the camera's near plane.
        /// </summary>
        public bool Perspective
        {
            get => _perspective;
            set => SetField(ref _perspective, value);
        }

        private TransformBase? _constrainDirection = null;
        /// <summary>
        /// If set, the billboard will only rotate around the direction from here to this transform.
        /// </summary>
        public TransformBase? ConstrainDirectionTransform
        {
            get => _constrainDirection;
            set => SetField(ref _constrainDirection, value);
        }

        private bool _scaleByDistance = false;
        /// <summary>
        /// If true, the billboard will scale based on the distance from the camera.
        /// </summary>
        public bool ScaleByDistance
        {
            get => _scaleByDistance;
            set => SetField(ref _scaleByDistance, value);
        }

        /// <summary>
        /// This scalar is multiplied by the distance from the camera to scale the billboard.
        /// </summary>
        private float _distanceScalar = 1.0f;
        public float DistanceScalar
        {
            get => _distanceScalar;
            set => SetField(ref _distanceScalar, value);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(ConstrainDirectionTransform):
                        ConstrainDirectionTransform?.WorldMatrixChanged.RemoveListener(OnConstrainDirectionChanged);
                        break;
                }
            }
            return change;
        }
        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Perspective):
                case nameof(ScaleByDistance):
                case nameof(DistanceScalar):
                    MarkWorldModified();
                    break;

                case nameof(ConstrainDirectionTransform):
                    ConstrainDirectionTransform?.WorldMatrixChanged.AddListener(OnConstrainDirectionChanged);
                    MarkWorldModified();
                    break;
            }
        }

        private void OnConstrainDirectionChanged(TransformBase @base)
            => MarkWorldModified();

        protected override Matrix4x4 CreateWorldMatrix()
        {
            var camera = XRCamera.CurrentRenderTarget;
            if (camera is null)
                return Parent?.WorldMatrix ?? Matrix4x4.Identity;

            var parentPosition = Parent?.WorldTranslation ?? Vector3.Zero;

            Vector3 lookAtPoint = Perspective 
                ? camera.Transform.WorldTranslation
                : GeoUtil.ClosestPointPlanePoint(camera.GetNearPlane(), parentPosition);

            Vector3 toCamVec = lookAtPoint - parentPosition;

            if (ConstrainDirectionTransform is not null)
            {
                //Constrain dir dictates the only direction in which the billboard can rotate around.
                Vector3 dir = ConstrainDirectionTransform.WorldTranslation - parentPosition;
                Vector3 constrained = Vector3.Cross(dir, toCamVec);
                toCamVec = Vector3.Cross(constrained, dir);
            }

            if (ScaleByDistance)
            {
                //Scale the billboard by the distance from the camera.
                toCamVec *= DistanceScalar;
            }

            return Matrix4x4.CreateLookTo(parentPosition, toCamVec, Globals.Up);
        }

        protected override Matrix4x4 CreateLocalMatrix()
        {
            return Matrix4x4.Identity;
        }
    }
}