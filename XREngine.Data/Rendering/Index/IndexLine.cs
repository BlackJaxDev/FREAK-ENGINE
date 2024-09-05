namespace XREngine.Data.Rendering
{
    public struct IndexLine(IndexPoint point1, IndexPoint point2)
    {
        public IndexPoint Point0 = point1;
        public IndexPoint Point1 = point2;
    }
}
