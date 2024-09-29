using System.Collections.ObjectModel;
using XREngine.Data.Core;
using YamlDotNet.Serialization;

namespace XREngine.Data.Rendering
{
    public abstract class IndexPrimitive(params int[] points) : XRBase
    {
        [YamlIgnore]
        public abstract FaceType Type { get; }

        protected List<int> _points = [.. points];
        public List<int> Points
        {
            get => _points;
            set => SetField(ref _points, value);
        }
    }
}
