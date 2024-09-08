﻿using System.ComponentModel;
using System.Runtime.InteropServices;

namespace XREngine.Data.Vectors;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct UVector3(uint x, uint y, uint z)
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
    public uint Z
    {
        readonly get => _z;
        set => _z = value;
    }

    private uint
        _x = x,
        _y = y,
        _z = z;

    [Browsable(false)]
    public uint* Data { get { fixed (void* ptr = &this) return (uint*)ptr; } }

    public void Write(VoidPtr address)
    {
        uint* dPtr = (uint*)address;
        for (int i = 0; i < 3; ++i)
            *dPtr++ = Data[i];
    }

    public void Read(VoidPtr address)
    {
        uint* data = (uint*)address;
        for (int i = 0; i < 3; ++i)
            Data[i] = *data++;
    }

    public uint this[int index]
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