using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    public class UICanvasTransform : UIBoundableTransform
    {
        protected override Matrix4x4 CreateLocalMatrix()
        {
            return base.CreateLocalMatrix();
        }

        public UICanvasTransform()
        {
            Camera2D = new XRCamera(new Transform());
            var param = new XROrthographicCameraParameters(1.0f, 1.0f, -0.5f, 0.5f);
            param.SetOriginBottomLeft();
            Camera2D.Parameters = param;
            Scene2D = new VisualScene2D();
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

        /// <summary>
        /// This is the camera used to render the 2D canvas.
        /// </summary>
        public XRCamera Camera2D { get; } = new XRCamera();
        /// <summary>
        /// This is the scene that contains all the 2D renderables.
        /// </summary>
        public VisualScene2D Scene2D { get; }

        protected void OnResizeLayout(BoundingRectangleF parentRegion)
        {
            Scene2D?.RenderTree.Remake(parentRegion);
            if (Camera2D.Parameters is not XROrthographicCameraParameters orthoParams)
                Camera2D.Parameters = orthoParams = new XROrthographicCameraParameters(parentRegion.Width, parentRegion.Height, -0.5f, 0.5f);
            orthoParams.SetOriginBottomLeft();
            orthoParams.Resize(parentRegion.Width, parentRegion.Height);
        }

        protected virtual void ResizeLayout()
        {
            OnResizeLayout(new BoundingRectangleF(Translation, Size));
        }

        public bool IsLayoutInvalidated { get; private set; }
        public override void InvalidateLayout()
        {
            base.InvalidateLayout();
            IsLayoutInvalidated = true;
            //World?.AddDirtyTransform(this, out _, false);
        }
        //protected internal override bool ParallelDepthRecalculate()
        //{
        //    return base.ParallelDepthRecalculate();
        //}

        public event Action<UICanvasTransform>? ResizeStarted;
        public event Action<UICanvasTransform>? ResizeFinished;
        public bool IsResizing { get; private set; }

        public virtual void UpdateLayout()
        {
            if (!IsLayoutInvalidated)
                return;
            
            IsResizing = true;
            ResizeStarted?.Invoke(this);
            ResizeLayout();
            IsLayoutInvalidated = false;
            IsResizing = false;
            ResizeFinished?.Invoke(this);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(DrawSpace):
                    switch (_drawSpace)
                    {
                        case ECanvasDrawSpace.Camera:
                            
                            break;
                        case ECanvasDrawSpace.Screen:

                            break;
                        case ECanvasDrawSpace.World:

                            break;
                    }
                    break;
            }
        }
    }
}
