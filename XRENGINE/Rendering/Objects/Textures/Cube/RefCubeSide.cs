namespace XREngine.Rendering
{
    public abstract class RefCubeSide
    {
        public uint Width { get; set; }
        public uint Height { get; set; }

        public abstract RenderCubeSide AsRenderSide(int mipIndex);
    }
}
