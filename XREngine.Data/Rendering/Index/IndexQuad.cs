using YamlDotNet.Serialization;

namespace XREngine.Data.Rendering
{
    public class IndexQuad : IndexPolygon
    {
        public override FaceType Type => FaceType.Quads;

        private bool _forwardSlash = false;
        public bool ForwardSlash
        {
            get => _forwardSlash;
            set => SetField(ref _forwardSlash, value);
        }

        [YamlIgnore]
        public int Point0 => _points[0];
        [YamlIgnore]
        public int Point1 => _points[1];
        [YamlIgnore]
        public int Point2 => _points[2];
        [YamlIgnore]
        public int Point3 => _points[3];

        public IndexQuad() { }
        /// <summary>
        /// Counter-Clockwise winding
        ///3--2        2       3--2      3          3--2
        ///|  |  =    /|  and  | /   or  |\    and   \ |
        ///|  |      / |       |/        | \          \|
        ///0--1     0--1       0         0--1          1
        /// </summary>
        public IndexQuad(int point0, int point1, int point2, int point3, bool forwardSlash = false)
            : base(point0, point1, point2, point3)
        {
            //IndexLine e01 = point0.LinkTo(point1);
            //IndexLine e13 = point1.LinkTo(point3);
            //IndexLine e32 = point3.LinkTo(point2);
            //IndexLine e20 = point2.LinkTo(point0);

            //e01.AddFace(this);
            //e13.AddFace(this);
            //e32.AddFace(this);
            //e20.AddFace(this);

            _forwardSlash = forwardSlash;

            //if (_forwardSlash)
            //{
            //    Line e03 = point0.LinkTo(point3);
            //}
            //else
            //{
            //    Line e12 = point1.LinkTo(point2);
            //}
        }
        public override List<IndexTriangle> ToTriangles()
        {
            List<IndexTriangle> triangles = [];
            if (_forwardSlash)
            {
                triangles.Add(new IndexTriangle(Point0, Point1, Point2));
                triangles.Add(new IndexTriangle(Point0, Point2, Point3));
            }
            else
            {
                triangles.Add(new IndexTriangle(Point0, Point1, Point3));
                triangles.Add(new IndexTriangle(Point3, Point1, Point2));
            }
            return triangles;
        }
    }
}
