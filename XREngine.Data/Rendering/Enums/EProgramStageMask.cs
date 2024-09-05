namespace XREngine.Data.Rendering
{
    [Flags]
    public enum EProgramStageMask
    {
        AllShaderBits = -1,
        None = 00,
        VertexShaderBit = 01,
        FragmentShaderBit = 02,
        GeometryShaderBit = 04,
        TessControlShaderBit = 08,
        TessEvaluationShaderBit = 16,
        ComputeShaderBit = 32
    }
}