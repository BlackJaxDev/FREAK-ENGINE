namespace XREngine.Rendering.Commands
{
    public abstract class RenderCommand2D : RenderCommand
    {
        public int ZIndex { get; set; }

        public override int CompareTo(RenderCommand? other)
            => ZIndex < ((other as RenderCommand2D)?.ZIndex ?? 0) ? -1 : 1;

        public RenderCommand2D()
            : base(0) { }

        public RenderCommand2D(int renderPass)
            : base(renderPass) { }

        public RenderCommand2D(int renderPass, int zIndex)
            : base(renderPass) => ZIndex = zIndex;
    }
}
