using Extensions;
using System.Runtime.InteropServices;

namespace XREngine.Data;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct bshort
{
    public short _data;

    public static implicit operator short(bshort val)
        => Endian.SerializeBig ? val._data.Reverse() : val._data;
    public static implicit operator bshort(short val)
        => new() { _data = Endian.SerializeBig ? val.Reverse() : val };

    public short Value
    {
        readonly get => this;
        set => this = value;
    }
    public override string ToString()
        => Value.ToString();

    public VoidPtr Address { get { fixed (void* p = &this) return p; } }
}
