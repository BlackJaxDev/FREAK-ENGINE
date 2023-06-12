using XREngine.Components.Camera;
using XREngine.Data.Transforms.Vectors;

namespace XREngine.Rendering
{
    public abstract class RenderCommand3D : RenderCommand
    {
        /// <summary>
        /// Used to determine what order to render in.
        /// Opaque objects closer to the camera are drawn first,
        /// whereas translucent objects farther from the camera are drawn first.
        /// </summary>
        public float RenderDistance { get; set; }

        public override int CompareTo(RenderCommand? other)
            => RenderDistance < ((other as RenderCommand3D)?.RenderDistance ?? 0.0f) ? -1 : 1;

        public RenderCommand3D() : this(0.0f) { }
        public RenderCommand3D(float renderDistance) : base(ERenderPass.OpaqueDeferredLit)
            => RenderDistance = renderDistance;
        public RenderCommand3D(ERenderPass renderPass) : base(renderPass)
            => RenderDistance = 0.0f;
        public RenderCommand3D(ERenderPass renderPass, float renderDistance) : this(renderPass)
            => RenderDistance = renderDistance;

        /// <summary>
        /// Sets RenderDistance by calculating the distance between the provided camera and point.
        /// If planar is true, distance is calculated to the camera's near plane.
        /// If false, the distance is calculated to the camera's world position.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="point"></param>
        /// <param name="planar"></param>
        public void SetRenderDistanceByCamera(CameraComponent camera, Vec3 point, bool planar)
            => RenderDistance = camera is null ? 0.0f : (planar ? camera.DistanceFromScreenPlane(point) : camera.DistanceFromWorldPointFast(point));
    }
}
