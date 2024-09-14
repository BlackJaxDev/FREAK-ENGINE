namespace XREngine.Data.Rendering
{
    [Flags]
    public enum EFrameBufferTextureTypeFlags
    {
        None = 0x0,
        Color = 0x1,
        Depth = 0x2,
        Stencil = 0x4,
        All = 0xF,
    }
}
