using System.Runtime.InteropServices;

namespace XREngine.Data;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BInt24
{
    public byte _dat0, _dat1, _dat2;

    public int Value
    {
        readonly get => Endian.SerializeBig ?
            ((_dat0) | (_dat1 << 8) | _dat2 << 16) :
            ((_dat0 << 16) | (_dat1 << 8) | _dat2);
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

    public static implicit operator int(BInt24 val) => val.Value;
    public static implicit operator BInt24(int val) => new(val);

    public static implicit operator Int24(BInt24 val) => val.Value;
    public static implicit operator BInt24(Int24 val) => new(val);

    public BInt24(int value)
    {
        _dat2 = _dat1 = _dat0 = 0;
        Value = value;
    }

    public BInt24(byte v0, byte v1, byte v2)
    {
        _dat2 = v2;
        _dat1 = v1;
        _dat0 = v0;
    }

    public VoidPtr Address { get { fixed (void* ptr = &this) return ptr; } }
}