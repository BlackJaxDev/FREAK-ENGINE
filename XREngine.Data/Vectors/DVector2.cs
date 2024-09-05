using System.ComponentModel;
using System.Runtime.InteropServices;

namespace XREngine.Data.Vectors;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct DVector2(double x, double y)
{
    public double X
    {
        readonly get => _x;
        set => _x = value;
    }

    public double Y
    {
        readonly get => _y;
        set => _y = value;
    }

    private double
        _x = x,
        _y = y;

    [Browsable(false)]
    public double* Data { get { fixed (void* ptr = &this) return (double*)ptr; } }

    public void Write(VoidPtr address)
    {
        double* dPtr = (double*)address;
        for (int i = 0; i < 2; ++i)
            *dPtr++ = Data[i];
    }

    public void Read(VoidPtr address)
    {
        double* data = (double*)address;
        for (int i = 0; i < 2; ++i)
            Data[i] = *data++;
    }

    public double this[int index]
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
