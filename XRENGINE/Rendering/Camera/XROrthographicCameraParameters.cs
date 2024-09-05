using System.Numerics;

namespace XREngine.Rendering
{
    public class XROrthographicCameraParameters(float width, float height, float nearPlane, float farPlane)
        : XRCameraParameters(nearPlane, farPlane)
    {
        public float Width
        {
            get => width;
            set => SetField(ref width, value);
        }

        public float Height 
        {
            get => height;
            set => SetField(ref height, value);
        }

        protected override Matrix4x4 CalculateProjectionMatrix()
            => Matrix4x4.CreateOrthographic(Width, Height, NearPlane, FarPlane);
    }
}
