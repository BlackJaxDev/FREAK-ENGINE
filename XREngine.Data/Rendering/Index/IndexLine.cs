using YamlDotNet.Serialization;

namespace XREngine.Data.Rendering
{
    public struct IndexLine(int point1, int point2)
    {
        [YamlIgnore]
        public int Point0 = point1;
        [YamlIgnore]
        public int Point1 = point2;
    }
}
