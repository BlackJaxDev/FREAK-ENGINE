using System.Runtime.InteropServices;

namespace XREngine.Extensions
{
    public sealed class CoTaskMemoryHandle : IDisposable
    {
        bool isDisposed;
        readonly IntPtr handle;

        public IntPtr Handle { get { return handle; } }

        public CoTaskMemoryHandle(IntPtr handle)
        {
            this.handle = handle;
        }

        public void Dispose()
        {
            OnDispose(true);
            GC.SuppressFinalize(this);
        }

        void OnDispose(bool isDisposing)
        {
            if (isDisposed) return;

            if (isDisposing)
            {
                if (handle != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(handle);
            }

            isDisposed = true;
        }
    }
}
