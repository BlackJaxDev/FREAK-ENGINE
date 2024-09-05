using System.Runtime.InteropServices;

namespace XREngine.Data;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BUInt24
{
    public byte _dat0, _dat1, _dat2;

    public uint Value
    {
        readonly get => Endian.SerializeBig ?
            ((_dat0) | ((uint)_dat1 << 8) | (uint)_dat2 << 16) :
            (((uint)_dat0 << 16) | ((uint)_dat1 << 8) | _dat2);
        set
        {
            if (Endian.SerializeBig)
            {
                _dat0 = (byte)((value) & 0xFF);
                _dat1 = (byte)((value >> 8) & 0xFF);
                _dat2 = (byte)((value >> 16) & 0xFF);
            }
            else
            {
                _dat2 = (byte)((value) & 0xFF);
                _dat1 = (byte)((value >> 8) & 0xFF);
                _dat0 = (byte)((value >> 16) & 0xFF);
            }
        }
    }

    public static implicit operator uint(BUInt24 val) { return val.Value; }
    public static implicit operator BUInt24(uint val) { return new BUInt24(val); }

    public static explicit operator int(BUInt24 val) { return (int)val.Value; }
    public static explicit operator BUInt24(int val) { return new BUInt24((uint)val); }

    public static implicit operator UInt24(BUInt24 val) { return new UInt24(val.Value); }
    public static implicit operator BUInt24(UInt24 val) { return new BUInt24(val.Value); }

    public BUInt24(uint value)
    {
        _dat2 = _dat1 = _dat0 = 0;
        Value = value;
    }

    public BUInt24(byte v0, byte v1, byte v2)
    {
        _dat2 = v2;
        _dat1 = v1;
        _dat0 = v0;
    }

    public VoidPtr Address { get { fixed (void* ptr = &this) return ptr; } }
}