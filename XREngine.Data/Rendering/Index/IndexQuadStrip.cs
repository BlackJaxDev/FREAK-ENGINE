using YamlDotNet.Serialization;

namespace XREngine.Data.Rendering
{
    public class IndexQuadStrip : IndexPolygon
    {
        [YamlIgnore]
        public override FaceType Type => FaceType.QuadStrip;

        public IndexQuadStrip() { }
        public IndexQuadStrip(params int[] points) : base(points) { }

        public override List<IndexTriangle> ToTriangles()
        {
            List<IndexTriangle> triangles = [];
            for (int i = 0; i < _points.Count - 2; i += 2)
            {
                triangles.Add(new IndexTriangle(_points[i], _points[i + 1], _points[i + 2]));
                triangles.Add(new IndexTriangle(_points[i + 1], _points[i + 2], _points[i + 3]));
            }
            return triangles;
        }
    }
}
