namespace XREngine.Data.Rendering
{
    public class IndexQuadStrip : IndexPolygon
    {
        public override FaceType Type { get { return FaceType.QuadStrip; } }

        public IndexQuadStrip() { }
        public IndexQuadStrip(params IndexPoint[] points) { }

        public override List<IndexTriangle> ToTriangles()
        {
            throw new NotImplementedException();
        }
    }
}
