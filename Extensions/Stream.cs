using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Extensions
{
    public static partial class Ext
    {
        public static async Task<T> ReadAsync<T>(this Stream stream) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            byte[] bytes = new byte[size];
            await stream.ReadAsync(bytes, 0, size);
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return result;
        }
        public static T Read<T>(this Stream stream) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            byte[] bytes = new byte[size];
            stream.Read(bytes, 0, size);
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return result;
        }
        public static async Task WriteAsync<T>(this Stream stream, T value) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(value, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            await stream.WriteAsync(arr, 0, arr.Length);
        }
        public static void Write<T>(this Stream stream, T value) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(value, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            stream.Write(arr, 0, arr.Length);
        }
    }
}
