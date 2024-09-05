namespace XREngine.Data.Rendering
{
    public class IndexLineStrip : IndexPrimitive
    {
        private bool _closed = false;

        public IndexLineStrip() { }
        public IndexLineStrip(bool closed, params IndexPoint[] points) 
            : base(points) { _closed = closed; }

        public override FaceType Type => _closed ? FaceType.LineLoop : FaceType.LineStrip;
    }
}
