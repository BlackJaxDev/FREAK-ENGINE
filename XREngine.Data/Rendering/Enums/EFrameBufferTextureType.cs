namespace XREngine.Data.Rendering
{
    [Flags]
    public enum EFrameBufferTextureType
    {
        None = 0x0,
        Color = 0x1,
        Depth = 0x2,
        Stencil = 0x4,
        Accum = 0x8,
        All = 0xF,
    }
}
