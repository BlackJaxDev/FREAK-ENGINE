namespace XREngine.Rendering
{
    public class XRTexture2DArray : XRTexture
    {
        public override uint MaxDimension { get; } = 2u;
        public bool MultiSample { get; set; }
    }
}