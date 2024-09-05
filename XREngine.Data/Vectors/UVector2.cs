using System.ComponentModel;
using System.Runtime.InteropServices;

namespace XREngine.Data.Vectors;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct UVector2(uint x, uint y)
{
    public uint X
    {
        readonly get => _x;
        set => _x = value;
    }

    public uint Y
    {
        readonly get => _y;
        set => _y = value;
    }

    private uint
        _x = x,
        _y = y;

    [Browsable(false)]
    public uint* Data { get { fixed (void* ptr = &this) return (uint*)ptr; } }

    public void Write(VoidPtr address)
    {
        uint* dPtr = (uint*)address;
        for (int i = 0; i < 2; ++i)
            *dPtr++ = Data[i];
    }

    public void Read(VoidPtr address)
    {
        uint* data = (uint*)address;
        for (int i = 0; i < 2; ++i)
            Data[i] = *data++;
    }

    public uint this[int index]
    {
        get
        {
            if (index < 0 || index > 1)
                throw new IndexOutOfRangeException("Cannot access vector at index " + index);
            return Data[index];
        }
        set
        {
            if (index < 0 || index > 1)
                throw new IndexOutOfRangeException("Cannot access vector at index " + index);
            Data[index] = value;
        }
    }
}
