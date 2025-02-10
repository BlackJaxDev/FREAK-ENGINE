using XREngine.Data;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.Objects
{
    public interface IBufferable
    {
        EComponentType ComponentType { get; }
        uint ComponentCount { get; }
        bool Normalize { get; }

        void Read(VoidPtr address);
        void Write(VoidPtr address);
    }
}
