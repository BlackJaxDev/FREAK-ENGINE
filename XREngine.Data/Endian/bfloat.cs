using Extensions;
using System.Runtime.InteropServices;

namespace XREngine.Data;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public unsafe struct bfloat
{
    public float _data;

    public static implicit operator float(bfloat val)
        => Endian.SerializeBig ? val._data.Reverse() : val._data;
    public static implicit operator bfloat(float val)
        => new() { _data = Endian.SerializeBig ? val.Reverse() : val };

    public float Value
    {
        readonly get => this;
        set => this = value;
    }

    public override readonly string ToString()
        => Value.ToString();

    public VoidPtr Address { get { fixed (void* p = &this) return p; } }
}