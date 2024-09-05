using System.Numerics;
using XREngine.Data.Core;
using XRBase = XREngine.Data.Core.XRBase;

namespace XREngine.Rendering
{
    public abstract class XRCameraParameters(float nearPlane, float farPlane) : XRBase
    {
        public XREvent<XRCameraParameters> ProjectionMatrixChanged { get; }

        public void ForceInvalidateProjection()
            => _projectionMatrix = null;

        protected bool ProjectionInvalidated
            => _projectionMatrix is null;

        protected Matrix4x4? _projectionMatrix;

        /// <summary>
        /// The distance to the near clipping plane (closest to the eye).
        /// This value must be less than the far plane distance but does not need to be positive for orthographic cameras.
        /// </summary>
        public float NearPlane
        {
            get => nearPlane;
            set => SetField(ref nearPlane, value);
        }

        /// <summary>
        /// The distance to the far clipping plane (farthest from the eye).
        /// This value must be greater than the near plane distance.
        /// </summary>
        public float FarPlane
        {
            get => farPlane;
            set => SetField(ref farPlane, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            _projectionMatrix = null;
        }

        /// <summary>
        /// Returns the projection matrix for the parameters set in this class.
        /// Recalculates the projection matrix if it has been invalidated by any parameter changes.
        /// </summary>
        /// <returns></returns>
        public Matrix4x4 GetProjectionMatrix()
        {
            if (_projectionMatrix is null)
            {
                _projectionMatrix = CalculateProjectionMatrix();
                ProjectionMatrixChanged.Invoke(this);
            }
            return _projectionMatrix.Value;
        }

        /// <summary>
        /// Requests the projection matrix to be recalculated by a derived class.
        /// </summary>
        /// <returns></returns>
        protected abstract Matrix4x4 CalculateProjectionMatrix();

        /// <summary>
        /// Returns the world transformation of the left and right eyes.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public virtual Matrix4x4 GetViewMatrix(XRCamera camera)
            => camera.Transform.WorldMatrix;
    }
}
