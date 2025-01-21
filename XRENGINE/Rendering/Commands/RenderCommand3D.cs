using System.Numerics;

namespace XREngine.Rendering.Commands
{
    public abstract class RenderCommand3D(int renderPass) : RenderCommand(renderPass)
    {
        private float _renderDistance = 0.0f;
        /// <summary>
        /// Used to determine what order to render in.
        /// Opaque objects closer to the camera are drawn first,
        /// whereas translucent objects farther from the camera are drawn first.
        /// </summary>
        public float RenderDistance
        {
            get => _renderDistance;
            set => SetField(ref _renderDistance, value);
        }

        public void UpdateRenderDistance(Vector3 thisWorldPosition, XRCamera camera)
            => RenderDistance = (camera.Transform.WorldTranslation - thisWorldPosition).LengthSquared();
        public override int CompareTo(RenderCommand? other)
            => RenderDistance < ((other as RenderCommand3D)?.RenderDistance ?? 0.0f) ? -1 : 1;

        public RenderCommand3D()
            : this(0) { }
    }
}
