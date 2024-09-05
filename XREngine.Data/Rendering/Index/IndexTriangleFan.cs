namespace XREngine.Data.Rendering
{
    public class IndexTriangleFan(params IndexPoint[] points) : IndexPolygon(points)
    {
        public override FaceType Type => FaceType.TriangleFan;

        public override List<IndexTriangle> ToTriangles()
        {
            throw new NotImplementedException();
        }
    }
}
