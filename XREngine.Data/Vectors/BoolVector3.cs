using System.ComponentModel;
using System.Runtime.InteropServices;

namespace XREngine.Data.Vectors;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BoolVector3(bool x, bool y, bool z)
{
    public bool X
    {
        readonly get => _x;
        set => _x = value;
    }
    public bool Y
    {
        readonly get => _y;
        set => _y = value;
    }
    public bool Z
    {
        readonly get => _z;
        set => _z = value;
    }

    private bool
        _x = x,
        _y = y,
        _z = z;

    [Browsable(false)]
    public bool* Data { get { fixed (void* ptr = &this) return (bool*)ptr; } }

    public void Write(VoidPtr address)
    {
        byte* dPtr = (byte*)address;
        for (int i = 0; i < 3; ++i)
            *dPtr++ = (byte)(Data[i] ? 1 : 0);
    }

    public void Read(VoidPtr address)
    {
        byte* data = (byte*)address;
        for (int i = 0; i < 3; ++i)
            Data[i] = *data++ != 0;
    }

    public readonly bool None => !Any;
    public readonly bool Any => X || Y || Z;
    public readonly bool All => X && Y && Z;

    public bool this[int index]
    {
        get
        {
            if (index < 0 || index > 2)
                throw new IndexOutOfRangeException("Cannot access vector at index " + index);
            return Data[index];
        }
        set
        {
            if (index < 0 || index > 2)
                throw new IndexOutOfRangeException("Cannot access vector at index " + index);
            Data[index] = value;
        }
    }
}
