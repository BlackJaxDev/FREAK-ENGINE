using XREngine.Data;
using XREngine.Data.Rendering;

namespace XREngine
{
    public interface IBufferable
    {
        EComponentType ComponentType { get; }
        uint ComponentCount { get; }
        bool Normalize { get; }
        void Write(VoidPtr address);
        void Read(VoidPtr address);
    }
}
