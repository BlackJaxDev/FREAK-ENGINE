using System.Runtime.InteropServices;
using System.Text;

namespace XREngine.Data.Lists.Unsafe
{
    public unsafe class UTF8ArrayPtr : IDisposable
    {
        private bool disposedValue;
        private readonly IntPtr[] ptrs;
        private readonly GCHandle handle;
        public IReadOnlyList<string> Strings { get; }

        public UTF8ArrayPtr(List<string> strings)
        {
            Strings = strings;
            ptrs = GetUtf8Pointers(strings);
            handle = GCHandle.Alloc(ptrs, GCHandleType.Pinned);
        }

        public byte** Ptr => (byte**)handle.AddrOfPinnedObject();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                handle.Free();
                foreach (IntPtr ptr in ptrs)
                    Marshal.FreeHGlobal(ptr);

                disposedValue = true;
            }
        }
        private static IntPtr[] GetUtf8Pointers(List<string> strings)
        {
            var pointers = new IntPtr[strings.Count];

            for (int i = 0; i < strings.Count; ++i)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(strings[i]);
                IntPtr ptr = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                Marshal.WriteByte(ptr, bytes.Length, 0); // Null-terminator
                pointers[i] = ptr;
            }

            return pointers;
        }

        ~UTF8ArrayPtr()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
