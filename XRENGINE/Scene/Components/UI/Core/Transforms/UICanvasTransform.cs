using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;

namespace XREngine.Rendering.UI
{
    public class UICanvasTransform : UIBoundableTransform
    {
        public event Action<UICanvasTransform>? LayoutingStarted;
        public event Action<UICanvasTransform>? LayoutingFinished;

        private XRCamera? _cameraSpaceCamera;
        public XRCamera? CameraSpaceCamera
        {
            get => _cameraSpaceCamera;
            set => SetField(ref _cameraSpaceCamera, value);
        }

        private ECanvasDrawSpace _drawSpace = ECanvasDrawSpace.Screen;
        /// <summary>
        /// This is the space in which the canvas is drawn.
        /// Screen means the canvas is drawn on top of the viewport, and it will always be visible.
        /// Camera means the canvas is drawn in front of the camera, and will only be visible as long as nothing is clipping into it.
        /// World means the canvas is drawn in the world like any other actor, and the camera is irrelevant.
        /// </summary>
        public ECanvasDrawSpace DrawSpace
        {
            get => _drawSpace;
            set => SetField(ref _drawSpace, value);
        }

        private float _cameraDrawSpaceDistance = 1.0f;
        /// <summary>
        /// When DrawSpace is set to Camera, this is the distance from the camera.
        /// Make sure the distance lies between NearZ and FarZ of the camera, or else the UI will seem to not render.
        /// </summary>
        public float CameraDrawSpaceDistance
        {
            get => _cameraDrawSpaceDistance;
            set => SetField(ref _cameraDrawSpaceDistance, value);
        }

        private bool _isLayoutInvalidated = true;
        public bool IsLayoutInvalidated
        {
            get => _isLayoutInvalidated;
            private set => SetField(ref _isLayoutInvalidated, value);
        }

        private bool _isUpdatingLayout = false;
        public bool IsUpdatingLayout
        {
            get => _isUpdatingLayout;
            private set => _isUpdatingLayout = value;
        }

        public override void InvalidateLayout()
        {
            base.InvalidateLayout();
            IsLayoutInvalidated = true;
        }

        public virtual void UpdateLayout()
        {
            //If the layout is not invalidated, don't update it.
            if (!IsLayoutInvalidated)
                return;

            IsUpdatingLayout = true;
            LayoutingStarted?.Invoke(this);

            //Create the canvas region.
            var canvasRegion = new BoundingRectangleF(Translation, Size);

            HorizontalAlignment = EHorizontalAlign.Stretch;
            VerticalAlignment = EVerticalAlign.Stretch;
            FitLayout(canvasRegion);

            IsLayoutInvalidated = false;
            IsUpdatingLayout = false;
            LayoutingFinished?.Invoke(this);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(DrawSpace):
                    MarkWorldModified();
                    break;
                case nameof(CameraDrawSpaceDistance):
                    MarkWorldModified();
                    break;
                case nameof(Size):
                    ActualSize = Size;
                    break;
            }
        }

        protected override Matrix4x4 CreateWorldMatrix()
        {
            switch (DrawSpace)
            {
                case ECanvasDrawSpace.Screen:
                    return Matrix4x4.Identity;
                case ECanvasDrawSpace.Camera:
                    if (CameraSpaceCamera is not null)
                    {
                        float depth = XRMath.DistanceToDepth(CameraDrawSpaceDistance, CameraSpaceCamera.NearZ, CameraSpaceCamera.FarZ);
                        var bottomLeft = CameraSpaceCamera.NormalizedViewportToWorldCoordinate(Vector2.Zero, depth);
                        return Matrix4x4.CreateWorld(bottomLeft, CameraSpaceCamera.Transform.WorldForward, CameraSpaceCamera.Transform.WorldUp);
                    }
                    else
                        return base.CreateWorldMatrix();
                default:
                case ECanvasDrawSpace.World:
                    return base.CreateWorldMatrix();
            }
        }
    }
}
