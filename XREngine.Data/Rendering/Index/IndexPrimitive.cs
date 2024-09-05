using System.Collections.ObjectModel;
using XREngine.Data.Core;

namespace XREngine.Data.Rendering
{
    public abstract class IndexPrimitive(params IndexPoint[] points) : XRBase
    {
        public abstract FaceType Type { get; }

        protected List<IndexPoint> _points = [.. points];
        public ReadOnlyCollection<IndexPoint> Points => _points.AsReadOnly();
    }
}
