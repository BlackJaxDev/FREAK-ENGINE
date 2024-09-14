namespace XREngine.Rendering
{
    public abstract class RefCubeSide
    {
        public uint Width { get; set; }
        public uint Height { get; set; }

        public RefCubeSide() { }
        public RefCubeSide(uint width, uint height)
        {
            Width = width;
            Height = height;
        }

        public abstract RenderCubeSide AsRenderSide(int mipIndex);
    }
}
