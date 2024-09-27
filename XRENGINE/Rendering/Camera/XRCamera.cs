using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering
{
    /// <summary>
    /// This class represents a camera in 3D space.
    /// It calculates the model-view-projection matrix driven by a transform and projection parameters.
    /// </summary>
    public class XRCamera : XRBase
    {
        public static XREvent<XRCamera?> CurrentRenderTargetChanged { get; } = new();
        private static XRCamera? _currentRenderTarget = null;
        public static XRCamera? CurrentRenderTarget
        {
            get => _currentRenderTarget;
            set
            {
                if (_currentRenderTarget == value)
                    return;

                _currentRenderTarget = value;
                CurrentRenderTargetChanged.Invoke(value);
            }
        }

        public List<XRViewport> Viewports { get; set; } = [];

        public XRCamera() { }
        public XRCamera(TransformBase transform)
        {
            Transform = transform;
        }
        public XRCamera(TransformBase transform, XRCameraParameters parameters)
        {
            Transform = transform;
            Parameters = parameters;
        }

        private TransformBase? _transform;
        public TransformBase Transform
        {
            get => _transform ?? SetFieldReturn(ref _transform, new Transform())!;
            set => SetField(ref _transform, value);
        }

        public PostProcessingSettings? PostProcessing
        {
            get => _postProcessing;
            set => SetField(ref _postProcessing, value);
        }

        private Matrix4x4 _modelViewProjectionMatrix = Matrix4x4.Identity;
        public Matrix4x4 WorldViewProjectionMatrix
        {
            get
            {
                VerifyMVP();
                return _modelViewProjectionMatrix;
            }
            set
            {
                SetField(ref _modelViewProjectionMatrix, value);
                _inverseModelViewProjectionMatrix = null;
            }
        }

        private Matrix4x4? _inverseModelViewProjectionMatrix = Matrix4x4.Identity;
        public Matrix4x4 InverseWorldViewProjectionMatrix
        {
            get
            {
                if (_inverseModelViewProjectionMatrix != null)
                    return _inverseModelViewProjectionMatrix.Value;

                if (!Matrix4x4.Invert(WorldViewProjectionMatrix, out Matrix4x4 inverted))
                {
                    Debug.LogWarning($"Failed to invert {nameof(WorldViewProjectionMatrix)}");
                    inverted = Matrix4x4.Identity;
                }
                _inverseModelViewProjectionMatrix = inverted;
                return inverted;
            }
            set
            {
                _inverseModelViewProjectionMatrix = value;
                if (!Matrix4x4.Invert(value, out Matrix4x4 inverted))
                {
                    Debug.LogWarning($"Failed to invert value set to {nameof(InverseWorldViewProjectionMatrix)}");
                    inverted = Matrix4x4.Identity;
                }
                WorldViewProjectionMatrix = inverted;
            }
        }

        private XRCameraParameters? _parameters;
        public XRCameraParameters Parameters
        {
            get => _parameters ?? SetFieldReturn(ref _parameters, GetDefaultCameraParameters(), UnregisterProjectionMatrixChanged, RegisterProjectionMatrixChanged)!;
            set => SetField(ref _parameters, value, UnregisterProjectionMatrixChanged, RegisterProjectionMatrixChanged);
        }

        public Matrix4x4 ProjectionMatrix
            => Parameters.GetProjectionMatrix();

        private static XRPerspectiveCameraParameters GetDefaultCameraParameters()
            => new(90.0f, null, 0.1f, 10000.0f);

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool canChange = base.OnPropertyChanging(propName, field, @new);
            if (canChange)
            {
                switch (propName)
                {
                    case nameof(Transform):
                        UnregisterWorldMatrixChanged();
                        break;
                }
            }
            return canChange;
        }
        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            switch (propName)
            {
                case nameof(Transform):
                    RegisterWorldMatrixChanged();
                    break;
            }
            base.OnPropertyChanged(propName, prev, field);
        }

        private void RegisterProjectionMatrixChanged(XRCameraParameters? parameters)
            => parameters?.ProjectionMatrixChanged.AddListener(ProjectionMatrixChanged);
        private void UnregisterProjectionMatrixChanged(XRCameraParameters? parameters)
            => parameters?.ProjectionMatrixChanged.RemoveListener(ProjectionMatrixChanged);

        private void RegisterWorldMatrixChanged()
            => _transform?.WorldMatrixChanged.AddListener(WorldMatrixChanged);
        private void UnregisterWorldMatrixChanged()
            => _transform?.WorldMatrixChanged.RemoveListener(WorldMatrixChanged);

        private void ProjectionMatrixChanged(XRCameraParameters parameters)
            => InvalidateMVP();
        private void WorldMatrixChanged(TransformBase transform)
            => InvalidateMVP();

        private bool _mvpInvalidated = true;
        private PostProcessingSettings? _postProcessing = null;
        private XRMaterial? _postProcessMaterial;

        /// <summary>
        /// Called any time the camera's world matrix or projection matrix changes.
        /// </summary>
        private void InvalidateMVP()
            => _mvpInvalidated = true;
        /// <summary>
        /// Recalculates the model-view-projection matrix.
        /// </summary>
        private void RecalcMVP()
            => WorldViewProjectionMatrix = Parameters.GetViewMatrix(this) * ProjectionMatrix;
        /// <summary>
        /// Checks if the model-view-projection matrix has been invalidated and recalculates it if so.
        /// </summary>
        private void VerifyMVP()
        {
            if (!_mvpInvalidated)
                return;

            RecalcMVP();
            _mvpInvalidated = false;
        }

        /// <summary>
        /// Returns the camera's near plane as a plane object.
        /// The normal is the camera's forward vector.
        /// </summary>
        /// <returns></returns>
        public Plane NearPlane()
            => XRMath.CreatePlaneFromPointAndNormal(CenterPointNearPlane, Transform.WorldForward);

        /// <summary>
        /// Returns the camera's far plane as a plane object.
        /// The normal is the opposite of the camera's forward vector.
        /// </summary>
        /// <returns></returns>
        public Plane FarPlane()
            => XRMath.CreatePlaneFromPointAndNormal(CenterPointFarPlane, -Transform.WorldForward);

        /// <summary>
        /// The center point of the camera's near plane in world space.
        /// </summary>
        public Vector3 CenterPointNearPlane
            => Transform.WorldTranslation + Transform.WorldForward * Parameters.NearPlane;

        /// <summary>
        /// The center point of the camera's far plane in world space.
        /// </summary>
        public Vector3 CenterPointFarPlane
            => Transform.WorldTranslation + Transform.WorldForward * Parameters.FarPlane;

        /// <summary>
        /// The distance from the camera's position to the given point in world space.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float DistanceFromWorldPosition(Vector3 point)
            => Vector3.Distance(Transform.WorldTranslation, point);

        /// <summary>
        /// Returns the distance from the camera's near plane to the given point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float DistanceFromNearPlane(Vector3 point)
        {
            Vector3 forward = Transform.WorldForward;
            Vector3 nearPoint = Transform.WorldTranslation + forward * Parameters.NearPlane;
            return GeoUtil.DistancePlanePoint(forward, XRMath.GetPlaneDistance(nearPoint, forward), point);
        }

        /// <summary>
        /// Returns the distance from the camera's far plane to the given point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float DistanceFromFarPlane(Vector3 point)
        {
            Vector3 forward = Transform.WorldForward;
            Vector3 farPoint = Transform.WorldTranslation + forward * Parameters.FarPlane;
            return GeoUtil.DistancePlanePoint(-forward, XRMath.GetPlaneDistance(farPoint, -forward), point);
        }

        /// <summary>
        /// Returns the point on the near plane closest to the given point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3 ClosestPointOnNearPlane(Vector3 point)
        {
            Vector3 forward = Transform.WorldForward;
            Vector3 nearPoint = Transform.WorldTranslation + forward * Parameters.NearPlane;
            return GeoUtil.ClosestPlanePointToPoint(forward, XRMath.GetPlaneDistance(nearPoint, forward), point);
        }

        /// <summary>
        /// Returns the point on the far plane closest to the given point.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3 ClosestPointOnFarPlane(Vector3 point)
        {
            Vector3 forward = Transform.WorldForward;
            Vector3 farPoint = Transform.WorldTranslation + forward * Parameters.FarPlane;
            return GeoUtil.ClosestPlanePointToPoint(-forward, XRMath.GetPlaneDistance(farPoint, -forward), point);
        }

        /// <summary>
        /// The frustum of this camera in world space.
        /// </summary>
        /// <returns></returns>
        public Frustum WorldFrustum()
            => new(WorldViewProjectionMatrix);

        /// <summary>
        /// The projection frustum of this camera with no transformation applied.
        /// </summary>
        /// <returns></returns>
        public Frustum UntransformedFrustum()
            => new(ProjectionMatrix);

        /// <summary>
        /// Returns a scale value that maintains the size of an object relative to the camera's distance.
        /// </summary>
        /// <param name="worldPoint">The point to evaluate scale at</param>
        /// <param name="refDistance">The distance from the camera to be at a scale of 1.0</param>
        public float DistanceScale(Vector3 worldPoint, float refDistance)
            => DistanceFromNearPlane(worldPoint) / refDistance;

        /// <summary>
        /// Returns a normalized X, Y coordinate relative to the camera's origin (center for perspective, bottom-left for orthographic) 
        /// with Z being the normalized depth (0.0f - 1.0f) from NearDepth (0.0f) to FarDepth (1.0f).
        /// </summary>
        public void WorldToScreen(Vector3 worldPoint, out Vector2 screenPoint, out float depth)
        {
            Vector3 xyd = WorldToScreen(worldPoint);
            screenPoint = new Vector2(xyd.X, xyd.Y);
            depth = xyd.Z;
        }
        /// <summary>
        /// Returns a normalized X, Y coordinate relative to the camera's origin (center for perspective, bottom-left for orthographic) 
        /// with Z being the normalized depth (0.0f - 1.0f) from NearDepth (0.0f) to FarDepth (1.0f).
        /// </summary>
        public void WorldToScreen(Vector3 worldPoint, out float x, out float y, out float depth)
        {
            Vector3 xyd = WorldToScreen(worldPoint);
            x = xyd.X;
            y = xyd.Y;
            depth = xyd.Z;
        }
        /// <summary>
        /// Returns a normalized X, Y coordinate relative to the camera's origin (center for perspective, bottom-left for orthographic) 
        /// with Z being the normalized depth (0.0f - 1.0f) from NearDepth (0.0f) to FarDepth (1.0f).
        /// </summary>
        public Vector3 WorldToScreen(Vector3 worldPoint)
            => (((Vector3.Transform(worldPoint, WorldViewProjectionMatrix)) + Vector3.One) * new Vector3(0.5f));

        /// <summary>
        /// Takes an X, Y coordinate relative to the camera's origin along with the normalized depth (0.0f - 1.0f) from NearDepth (0.0f) to FarDepth (1.0f), and returns a position in the world.
        /// </summary>
        public Vector3 ScreenToWorld(Vector2 normalizedScreenPoint, float depth)
            => ScreenToWorld(normalizedScreenPoint.X, normalizedScreenPoint.Y, depth);
        /// <summary>
        /// Takes an X, Y coordinate relative to the camera's Origin along with the normalized depth (0.0f - 1.0f) from NearDepth (0.0f) to FarDepth (1.0f), and returns a position in the world.
        /// </summary>
        public Vector3 ScreenToWorld(float x, float y, float depth)
            => ScreenToWorld(new Vector3(x, y, depth));
        /// <summary>
        /// Takes an X, Y coordinate relative to the camera's Origin, with Z being the normalized depth (0.0f - 1.0f) from NearDepth (0.0f) to FarDepth (1.0f), and returns a position in the world.
        /// </summary>
        public Vector3 ScreenToWorld(Vector3 normalizedPointDepth)
            => Vector3.Transform(normalizedPointDepth * new Vector3(2.0f) - Vector3.One, InverseWorldViewProjectionMatrix);

        public Segment GetWorldSegment(Vector2 screenPoint)
        {
            Vector3 start = ScreenToWorld(screenPoint, 0.0f);
            Vector3 end = ScreenToWorld(screenPoint, 1.0f);
            return new Segment(start, end);
        }
        public Ray GetWorldRay(Vector2 screenPoint)
        {
            Vector3 start = ScreenToWorld(screenPoint, 0.0f);
            Vector3 end = ScreenToWorld(screenPoint, 1.0f);
            return new Ray(start, end - start);
        }
        public Vector3 GetPointAtDepth(Vector2 screenPoint, float depth)
            => ScreenToWorld(screenPoint, depth);
        public Vector3 GetPointAtDistance(Vector2 screenPoint, float distance)
            => GetWorldSegment(screenPoint).PointAtLineDistance(distance);

        /// <summary>
        /// Sets RenderDistance by calculating the distance between the provided camera and point.
        /// If planar is true, distance is calculated to the camera's near plane.
        /// If false, the distance is calculated to the camera's world position.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="point"></param>
        /// <param name="planar"></param>
        public float DistanceFrom(Vector3 point, bool planar)
            => planar
                ? DistanceFromNearPlane(point)
                : DistanceFromWorldPosition(point);

        public virtual void SetAmbientOcclusionUniforms(XRRenderProgram program)
            => PostProcessing?.AmbientOcclusion?.SetUniforms(program);
        public virtual void SetBloomUniforms(XRRenderProgram program)
            => PostProcessing?.Bloom?.SetUniforms(program);
        public virtual void SetPostProcessUniforms(XRRenderProgram program)
            => PostProcessing?.SetUniforms(program);

        public XRMaterial? PostProcessMaterial
        {
            get => _postProcessMaterial;
            set => SetField(ref _postProcessMaterial, value);
        }

        public void SetUniforms(XRRenderProgram program)
        {

        }
    }
}
