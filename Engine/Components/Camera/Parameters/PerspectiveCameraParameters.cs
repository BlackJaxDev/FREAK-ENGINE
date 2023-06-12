using XREngine.Data.Transforms;

namespace XREngine.Components.Camera.Parameters
{
    public class PerspectiveCameraParameters : CameraParameters
    {
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }

        public PerspectiveCameraParameters(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
            : base(nearPlane, farPlane)
        {
            FieldOfView = fieldOfView;
            AspectRatio = aspectRatio;
        }

        public override Matrix GetProjectionMatrix()
            => Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
    }
}
