using System.Runtime.InteropServices;

namespace XREngine.Data;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct Int24
{
    public static readonly int MaxValue = 0x00FFFFFF;

    public byte _dat2, _dat1, _dat0;

    public int Value
    {
        readonly get => ((int)_dat0 << 16) | ((int)_dat1 << 8) | ((int)_dat2);
        set
        {
            _dat2 = (byte)((value) & 0xFF);
            _dat1 = (byte)((value >> 8) & 0xFF);
            _dat0 = (byte)((value >> 16) & 0xFF);
        }
    }

    public static implicit operator int(Int24 val) => val.Value;
    public static implicit operator Int24(int val) => new(val);

    public static implicit operator uint(Int24 val) => (uint)val.Value;
    public static implicit operator Int24(uint val) => new((int)val);

    public Int24(int value)
    {
        _dat2 = (byte)((value) & 0xFF);
        _dat1 = (byte)((value >> 8) & 0xFF);
        _dat0 = (byte)((value >> 16) & 0xFF);
    }

    public Int24(byte v0, byte v1, byte v2)
    {
        _dat2 = v2;
        _dat1 = v1;
        _dat0 = v0;
    }

    public VoidPtr Address { get { fixed (void* ptr = &this) return ptr; } }
}
