//using System.ComponentModel;
//using System.Runtime.InteropServices;
//using XREngine.Data.Rendering;
//using XREngine.Rendering.Objects;

//namespace XREngine.Data;

//[Serializable]
//[StructLayout(LayoutKind.Sequential, Pack = 1)]
//public unsafe struct IVector4(int x, int y, int z, int w) : IBufferable, IUniformable
//{
//    public int X { readonly get => x; set => x = value; }
//    public int Y { readonly get => y; set => y = value; }
//    public int Z { readonly get => z; set => z = value; }
//    public int W { readonly get => w; set => w = value; }

//    [Browsable(false)]
//    public int* Data { get { fixed (void* ptr = &this) return (int*)ptr; } }

//    [Browsable(false)]
//    public readonly EComponentType ComponentType => EComponentType.Int;
//    [Browsable(false)]
//    public readonly uint ComponentCount => 4;
//    [Browsable(false)]
//    public readonly bool Normalize => false;

//    public void Write(VoidPtr address)
//    {
//        int* dPtr = (int*)address;
//        for (int i = 0; i < ComponentCount; ++i)
//            *dPtr++ = Data[i];
//    }
//    public void Read(VoidPtr address)
//    {
//        int* data = (int*)address;
//        for (int i = 0; i < ComponentCount; ++i)
//            Data[i] = *data++;
//    }

//    public int this[int index]
//    {
//        get
//        {
//            if (index < 0 || index > 3)
//                throw new IndexOutOfRangeException("Cannot access vector at index " + index);
//            return Data[index];
//        }
//        set
//        {
//            if (index < 0 || index > 3)
//                throw new IndexOutOfRangeException("Cannot access vector at index " + index);
//            Data[index] = value;
//        }
//    }

//    public override readonly string ToString()
//        => string.Format("({0}, {1}, {2}, {3})", X, Y, Z, W);
//}
