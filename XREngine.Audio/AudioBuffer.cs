using Silk.NET.OpenAL;
using XREngine.Core;

namespace XREngine.Audio
{
    public sealed class AudioBuffer : IDisposable, IPoolable
    {
        public ListenerContext ParentListener { get; }
        public AL Api => ParentListener.Api;
        public uint Handle { get; }

        internal AudioBuffer(ListenerContext parentListener)
        {
            ParentListener = parentListener;
            Handle = parentListener.Api.GenBuffer();
        }

        public unsafe void SetData(byte[] data, int frequency, bool stereo)
        {
            fixed (byte* pData = data)
                Api.BufferData(Handle, stereo ? BufferFormat.Stereo8 : BufferFormat.Mono8, pData, data.Length, frequency);
        }

        public unsafe void SetData(short[] data, int frequency, bool stereo)
        {
            fixed (short* pData = data)
                Api.BufferData(Handle, stereo ? BufferFormat.Stereo16 : BufferFormat.Mono16, pData, data.Length, frequency);
        }

        public void Dispose()
        {
            Api.DeleteBuffer(Handle);
            GC.SuppressFinalize(this);
        }

        void IPoolable.OnPoolableReset()
        {
            //The user should call SetData to set the data for the buffer after taking it from the pool.
        }

        void IPoolable.OnPoolableReleased()
        {
            SetData(Array.Empty<byte>(), 0, false);
        }

        void IPoolable.OnPoolableDestroyed()
        {
            Dispose();
        }
    }
}