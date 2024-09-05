using Extensions;

namespace XREngine.Data.Rendering
{
    public class IndexTriangle : IndexPolygon
    {
        public override FaceType Type => FaceType.Triangles;

        public IndexPoint Point0 => _points[0];
        public IndexPoint Point1 => _points[1];
        public IndexPoint Point2 => _points[2];

        public IndexTriangle() { }
        /// <summary>
        /// Counter-Clockwise winding
        ///     2
        ///    / \
        ///   /   \
        ///  0-----1
        /// </summary>
        public IndexTriangle(IndexPoint point0, IndexPoint point1, IndexPoint point2)
        {
            _points.Add(point0);
            _points.Add(point1);
            _points.Add(point2);
        }

        public override List<IndexTriangle> ToTriangles()
            => [this];

        public override bool Equals(object? obj)
            => obj is IndexTriangle t && 
            t.Point0 == Point0 && 
            t.Point1 == Point1 && 
            t.Point2 == Point2;

        public override int GetHashCode()
            => base.GetHashCode();

        public string WriteToString()
            => $"{Point0.WriteToString()} {Point1.WriteToString()} {Point2.WriteToString()}";

        private static readonly char[] Separator = [' '];
        public void ReadFromString(string str)
        {
            string[] indices = str.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            _points = [new(), new(), new()];
            _points[0].ReadFromString(indices.IndexInRangeArrayT(0) ? indices[0] : string.Empty);
            _points[1].ReadFromString(indices.IndexInRangeArrayT(1) ? indices[1] : string.Empty);
            _points[2].ReadFromString(indices.IndexInRangeArrayT(2) ? indices[2] : string.Empty);
        }
    }
}
