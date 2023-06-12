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

        public float EyeSeparation { get; set; }

        public XRCameraParameters(float eyeSeparation, float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
            : base(fieldOfView, aspectRatio, nearPlane, farPlane)
        {
            EyeSeparation = eyeSeparation;
        }

        public override Matrix GetProjectionMatrix()
            => Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);

        public Matrix GetViewMatrix(CameraComponent camera, Eye eye)
        {
            Vec3 eyePosition = camera.Transform.WorldTranslation + (eye == Eye.Left ? -camera.Right : camera.Right) * 0.5f * EyeSeparation;
            return Matrix.CreateLookAt(eyePosition, eyePosition + camera.Forward, camera.Up);
        }
    }
}
