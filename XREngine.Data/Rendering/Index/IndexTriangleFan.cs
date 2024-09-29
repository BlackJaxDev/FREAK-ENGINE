using YamlDotNet.Serialization;

namespace XREngine.Data.Rendering
{
    public class IndexTriangleFan(params int[] points) : IndexPolygon(points)
    {
        [YamlIgnore]
        public override FaceType Type => FaceType.TriangleFan;

        public override List<IndexTriangle> ToTriangles()
        {
            List<IndexTriangle> triangles = [];
            for (int i = 1; i < _points.Count - 1; ++i)
                triangles.Add(new IndexTriangle(_points[0], _points[i], _points[i + 1]));
            return triangles;
        }
    }
}
