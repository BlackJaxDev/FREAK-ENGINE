using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.EXT;
using XREngine.Core;
using XREngine.Data;
using XREngine.Data.Core;

namespace XREngine.Audio
{
    public sealed class AudioBuffer : XRBase, IDisposable, IPoolable
    {
        public ListenerContext ParentListener { get; }
        public static AL Api => ListenerContext.Api;
        public uint Handle { get; private set; }

        internal AudioBuffer(ListenerContext parentListener)
        {
            ParentListener = parentListener;
            Handle = ListenerContext.Api.GenBuffer();
            ParentListener.VerifyError();
        }

        public unsafe void SetData(byte[] data, int frequency, bool stereo)
        {
            Api.BufferData(Handle, stereo ? BufferFormat.Stereo8 : BufferFormat.Mono8, data, frequency);
            ParentListener.VerifyError();
        }
        public void SetData(short[] data, int frequency, bool stereo)
        {
            ParentListener.VerifyError();
            Api.BufferData(Handle, stereo ? BufferFormat.Stereo16 : BufferFormat.Mono16, data, frequency);
            ParentListener.VerifyError();
        }
        public void SetData(float[] data, int frequency, bool stereo)
        {
            Api.BufferData(Handle, stereo ? FloatBufferFormat.Stereo : FloatBufferFormat.Mono, data, frequency);
            ParentListener.VerifyError();
        }

        public unsafe void SetData(AudioData buffer)
        {
            if (buffer.Data is null)
                return;

            void* ptr = buffer.Data.Address.Pointer;
            int length = (int)buffer.Data.Length;
            ParentListener.VerifyError();
            switch (buffer.Type)
            {
                case AudioData.EPCMType.Byte:
                    Api.BufferData(Handle, buffer.Stereo ? BufferFormat.Stereo8 : BufferFormat.Mono8, ptr, length, buffer.Frequency);
                    break;
                case AudioData.EPCMType.Short:
                    Api.BufferData(Handle, buffer.Stereo ? BufferFormat.Stereo16 : BufferFormat.Mono16, ptr, length, buffer.Frequency);
                    break;
                case AudioData.EPCMType.Float:
                    Api.BufferData(Handle, buffer.Stereo ? FloatBufferFormat.Stereo : FloatBufferFormat.Mono, ptr, length, buffer.Frequency);
                    break;
            }
            ParentListener.VerifyError();
        }

        public void Dispose()
        {
            Api.DeleteBuffer(Handle);
            ParentListener.VerifyError();
            GC.SuppressFinalize(this);
        }

        void IPoolable.OnPoolableReset()
        {
            Handle = ListenerContext.Api.GenBuffer();
            //The user should call SetData to set the data for the buffer after taking it from the pool.
        }

        void IPoolable.OnPoolableReleased()
        {
            SetData(Array.Empty<byte>(), 0, false);
            Api.DeleteBuffer(Handle);
        }

        void IPoolable.OnPoolableDestroyed()
        {
            Dispose();
        }
    }
}