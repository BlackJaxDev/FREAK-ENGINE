using XREngine.Data.Transforms;

namespace XREngine.Components.Camera.Parameters
{
    public class OrthographicCameraParameters : CameraParameters
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public OrthographicCameraParameters(float width, float height, float nearPlane, float farPlane)
            : base(nearPlane, farPlane)
        {
            Width = width;
            Height = height;
        }

        public override Matrix GetProjectionMatrix()
            => Matrix.CreateOrthographic(Width, Height, NearPlane, FarPlane);
    }
}
