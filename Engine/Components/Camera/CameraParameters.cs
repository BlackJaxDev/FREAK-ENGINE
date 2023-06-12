using XREngine.Data;
using XREngine.Data.Transforms;
using XREngine.Scenes.Transforms;

namespace XREngine.Components.Camera
{
    public abstract class CameraParameters
    {
        protected CameraParameters(float nearPlane, float farPlane)
        {
            NearPlane = nearPlane;
            FarPlane = farPlane;
            ProjectionMatrixChanged = new XEvent<CameraParameters>();
        }

        public XEvent<CameraParameters> ProjectionMatrixChanged;

        public float NearPlane { get; set; }
        public float FarPlane { get; set; }
        public abstract Matrix GetProjectionMatrix();
    }
}
