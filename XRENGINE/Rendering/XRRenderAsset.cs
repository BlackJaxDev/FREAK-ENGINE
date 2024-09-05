using XREngine.Core.Files;

namespace XREngine.Rendering
{
    public class XRRenderAsset<T>(T data, string name) : XRAsset(name) where T : GenericRenderObject
    {
        public T Data { get; set; } = data;
    }
}
