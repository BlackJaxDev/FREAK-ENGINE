namespace XREngine.Data.Rendering
{
    public enum EBufferUsage
    {
        StreamDraw = 0,
        StreamRead = 1,
        StreamCopy = 2,
        StaticDraw = 4,
        StaticRead = 5,
        StaticCopy = 6,
        DynamicDraw = 8,
        DynamicRead = 9,
        DynamicCopy = 10
    }
}
