using XREngine.Data.XMath.Transforms;

namespace XREngine.Components.Core
{
    public abstract class CameraParameters
    {
        protected CameraParameters(float nearPlane, float farPlane)
        {
            NearPlane = nearPlane;
            FarPlane = farPlane;
        }

        public float NearPlane { get; set; }
        public float FarPlane { get; set; }
        public abstract Matrix44 GetProjectionMatrix();
    }

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

        public override Matrix44 GetProjectionMatrix()
        {
            return Matrix44.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
        }
    }

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

        public override Matrix44 GetProjectionMatrix()
        {
            return Matrix44.CreateOrthographic(Width, Height, NearPlane, FarPlane);
        }
    }

    public class Camera
    {
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public CameraParameters Parameters { get; set; }

        public Matrix44 ViewMatrix => Matrix44.CreateLookAt(Position, Position + Forward, Up);
        public Matrix44 ProjectionMatrix => Parameters.GetProjectionMatrix();

        public Vector3 Forward => Rotation.Rotate(Vector3.UnitZ);
        public Vector3 Up => Rotation.Rotate(Vector3.UnitY);
        public Vector3 Right => Rotation.Rotate(Vector3.UnitX);

        public Camera(CameraParameters parameters)
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Parameters = parameters;
        }

        public void Rotate(Quaternion rotation)
        {
            Rotation = (Rotation * rotation).Normalized();
        }

        public void Translate(Vector3 translation)
        {
            Position += Rotation.Rotate(translation);
        }

        public Matrix44 GetWorldMatrix()
        {
            Matrix44 worldMatrix = Matrix44.Identity;
            worldMatrix *= Matrix44.Rotation(Rotation);
            worldMatrix *= Matrix44.Translation(Position);
            return worldMatrix;
        }
    }
}
