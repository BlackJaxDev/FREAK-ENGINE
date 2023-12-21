using XREngine.Data.Geometry;
using XREngine.Data.Transforms;
using XREngine.Data.Transforms.Vectors;
using XREngine.Scenes;
using XREngine.Scenes.Transforms;

namespace XREngine.Components.Camera
{
    public class CameraComponent : Component
    {
        public CameraParameters Parameters { get; set; }

        public Matrix LocalViewMatrix => Transform.LocalMatrix;
        public Matrix WorldViewMatrix => Transform.WorldMatrix;
        public Matrix ProjectionMatrix => Parameters.GetProjectionMatrix();
        public Matrix WorldViewProjectionMatrix { get; set; }

        public CameraComponent(CameraParameters parameters, SceneNode node) : base(node)
        {
            Parameters = parameters;
            Parameters.ProjectionMatrixChanged.AddListener(ProjectionMatrixChanged);
            Transform.WorldMatrixChanged.AddListener(WorldMatrixChanged);
        }

        private void ProjectionMatrixChanged(CameraParameters parameters)
            => UpdateMatrix();
        private void WorldMatrixChanged(TransformBase transform)
            => UpdateMatrix();
        private void UpdateMatrix()
            => WorldViewProjectionMatrix = WorldViewMatrix * ProjectionMatrix;

        public Vec3 WorldPoint => WorldViewMatrix.Translation;
        public Vec3 WorldForward => WorldViewMatrix.Forward;
        
        public Plane GetScreenPlane()
            => new(WorldPoint, WorldForward);

        public float GetScreenPlaneOriginDistance()
            => Plane.ComputeDistance(WorldPoint, WorldForward);

        public float DistanceFromScreenPlane(Vec3 point)
        {
            Vec3 forward = WorldForward;
            return Collision.DistancePlanePoint(forward, Plane.ComputeDistance(WorldPoint, forward), point);
        }

        public Vec3 ClosestPointOnScreenPlane(Vec3 point)
        {
            Vec3 forward = WorldForward;
            return Collision.ClosestPlanePointToPoint(forward, Plane.ComputeDistance(WorldPoint, forward), point);
        }
        
        public float DistanceTo(Vec3 point)
            => WorldPoint.DistanceTo(point);

        public Frustum GetCameraFrustum()
            => new Frustum(WorldViewProjectionMatrix);
    }
}
