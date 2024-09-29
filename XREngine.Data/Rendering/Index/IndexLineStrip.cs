using YamlDotNet.Serialization;

namespace XREngine.Data.Rendering
{
    public class IndexLineStrip : IndexPrimitive
    {
        private bool _closed = false;
        public bool Closed
        {
            get => _closed;
            set => SetField(ref _closed, value);
        }

        public IndexLineStrip() { }
        public IndexLineStrip(bool closed, params int[] points) 
            : base(points) { _closed = closed; }

        [YamlIgnore]
        public override FaceType Type => _closed ? FaceType.LineLoop : FaceType.LineStrip;
    }
}
