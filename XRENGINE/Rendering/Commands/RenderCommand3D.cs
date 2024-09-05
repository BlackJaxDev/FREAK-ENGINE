namespace XREngine.Rendering.Commands
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

        public RenderCommand3D()
            : this(0.0f) { }

        public RenderCommand3D(float renderDistance)
            : base(0) => RenderDistance = renderDistance;

        public RenderCommand3D(int renderPass)
            : base(renderPass) => RenderDistance = 0.0f;

        public RenderCommand3D(int renderPass, float renderDistance)
            : this(renderPass) => RenderDistance = renderDistance;
    }
}
