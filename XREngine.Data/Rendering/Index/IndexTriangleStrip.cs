namespace XREngine.Data.Rendering
{
    public class IndexTriangleStrip : IndexPolygon
    {
        public override FaceType Type { get { return FaceType.TriangleStrip; } }

        public IndexTriangleStrip() { }
        /// <summary>
        /// Clockwise, Counter-Clockwise, repeat
        ///    1-----3
        ///   / \   / \
        ///  /   \ /   \
        /// 0-----2-----4
        /// </summary>
        public IndexTriangleStrip(params IndexPoint[] points)
        {
            if (points.Length < 3)
                throw new Exception("A triangle strip needs 3 or more points.");
            _points = [.. points];
            //for (int i = 0; i < _points.Count - 1; ++i)
            //{
            //    _points[i].LinkTo(_points[i + 1]);
            //    if (i + 2 < _points.Count)
            //        _points[i].LinkTo(_points[i + 2]);
            //}
        }

        public override List<IndexTriangle> ToTriangles()
        {
            List<IndexTriangle> triangles = [];
            for (int i = 2; i < _points.Count; ++i)
            {
                bool cw = (i & 1) == 0;
                triangles.Add(new IndexTriangle(
                    _points[i - 2], 
                    _points[cw ? i : i - 1], 
                    _points[cw ? i - 1 : i]));
            }
            return triangles;
        }
    }
}
