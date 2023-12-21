using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Components.Camera.Parameters
{
    public class XRCameraParameters : PerspectiveCameraParameters
    {
        public enum Eye
        {
            Left,
            Right,
        };

        /// <summary>
        /// This distance between both eye viewpoints.
        /// </summary>
        public float EyeSeparation { get; set; }

        public XRCameraParameters(float eyeSeparation, float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
            : base(fieldOfView, aspectRatio, nearPlane, farPlane) => EyeSeparation = eyeSeparation;

        public override Matrix GetProjectionMatrix()
            => Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);

        public Matrix GetViewMatrix(CameraComponent camera, Eye eye)
        {
            var tfm = camera.Transform.WorldMatrix;
            Vec3 pos = tfm.Translation + (eye == Eye.Left ? -tfm.Right : tfm.Right) * 0.5f * EyeSeparation;
            return Matrix.CreateLookAt(pos, pos + tfm.Forward, tfm.Up);
        }
    }
}
