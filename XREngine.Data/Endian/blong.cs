using Extensions;
using System.Runtime.InteropServices;

namespace XREngine.Data;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public unsafe struct blong
{
    public long _data;

    public static implicit operator long(blong val)
        => Endian.SerializeBig ? val._data.Reverse() : val._data;
    public static implicit operator blong(long val)
        => new() { _data = Endian.SerializeBig ? val.Reverse() : val };

    public long Value
    {
        readonly get => this;
        set => this = value;
    }

    public override readonly string ToString()
        => Value.ToString();

    public VoidPtr OffsetAddress
    {
        get => Address + Value;
        set => Value = (long)value - (long)Address;
    }

    public VoidPtr Address { get { fixed (void* p = &this) return p; } }
}