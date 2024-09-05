namespace XREngine.Data.Rendering
{
    public abstract class IndexPolygon(params IndexPoint[] points) : IndexPrimitive(points)
    {
        public abstract List<IndexTriangle> ToTriangles();
        public bool ContainsEdge(IndexLine edge, out bool polygonIsCCW)
        {
            for (int i = 0; i < _points.Count; ++i)
            {
                if (_points[i] == edge.Point0)
                {
                    if (i + 1 < _points.Count && _points[i + 1] == edge.Point1)
                    {
                        polygonIsCCW = true;
                        return true;
                    }
                    else if (i - 1 >= 0 && _points[i - 1] == edge.Point1)
                    {
                        polygonIsCCW = false;
                        return true;
                    }
                }
            }
            polygonIsCCW = true;
            return false;
        }
    }
}
