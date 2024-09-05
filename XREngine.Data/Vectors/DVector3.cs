using System.ComponentModel;
using System.Runtime.InteropServices;

namespace XREngine.Data.Vectors;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct DVector3(double x, double y, double z)
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
    public double Z
    {
        readonly get => _z;
        set => _z = value;
    }

    private double
        _x = x,
        _y = y,
        _z = z;

    [Browsable(false)]
    public double* Data { get { fixed (void* ptr = &this) return (double*)ptr; } }

    public void Write(VoidPtr address)
    {
        double* dPtr = (double*)address;
        for (int i = 0; i < 3; ++i)
            *dPtr++ = Data[i];
    }

    public void Read(VoidPtr address)
    {
        double* data = (double*)address;
        for (int i = 0; i < 3; ++i)
            Data[i] = *data++;
    }

    public double this[int index]
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
