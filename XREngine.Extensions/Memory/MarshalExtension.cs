using System.Runtime.InteropServices;

namespace XREngine.Extensions
{
    public static class MarshalExtension
    {
        public static CoTaskMemoryHandle CopyToCoTaskMem(this byte[] array)
        {
            var ptr = AllocArrayCoTaskMem(array);
            Marshal.Copy(array, 0, ptr, array.Length);
            return new CoTaskMemoryHandle(ptr);
        }
        public static CoTaskMemoryHandle CopyToCoTaskMem(this float[] array)
        {
            var ptr = AllocArrayCoTaskMem(array);
            Marshal.Copy(array, 0, ptr, array.Length);
            return new CoTaskMemoryHandle(ptr);
        }
        public static CoTaskMemoryHandle CopyToCoTaskMem(this double[] array)
        {
            var ptr = AllocArrayCoTaskMem(array);
            Marshal.Copy(array, 0, ptr, array.Length);
            return new CoTaskMemoryHandle(ptr);
        }
        public static CoTaskMemoryHandle CopyToCoTaskMem(this int[] array)
        {
            var ptr = AllocArrayCoTaskMem(array);
            Marshal.Copy(array, 0, ptr, array.Length);
            return new CoTaskMemoryHandle(ptr);
        }
        public static CoTaskMemoryHandle CopyToCoTaskMem(this short[] array)
        {
            var ptr = AllocArrayCoTaskMem(array);
            Marshal.Copy(array, 0, ptr, array.Length);
            return new CoTaskMemoryHandle(ptr);
        }
        public static CoTaskMemoryHandle CopyToCoTaskMem(this long[] array)
        {
            var ptr = AllocArrayCoTaskMem(array);
            Marshal.Copy(array, 0, ptr, array.Length);
            return new CoTaskMemoryHandle(ptr);
        }
        private static IntPtr AllocArrayCoTaskMem<T>(T[] array)
        {
            var type = typeof(T);
            var size = Marshal.SizeOf(type) * array.Length;
            return Marshal.AllocCoTaskMem(size);
        }
    }
}
