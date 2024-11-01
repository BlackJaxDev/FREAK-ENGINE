using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.EXT;
using XREngine.Core;
using XREngine.Data;
using XREngine.Data.Core;

namespace XREngine.Audio
{
    public class AudioInputDevice(
        ListenerContext listener,
        int bufferSize = 2048,
        uint freq = 22050,
        BufferFormat format = BufferFormat.Mono16,
        string? deviceName = null) : XRBase
    {
        private uint _sampleRate = freq;
        private BufferFormat _format = format;
        private int _bufferSize = bufferSize;
        private string? _deviceName = deviceName;

        public ListenerContext Listener { get; private set; } = listener;
        public AudioCapture<BufferFormat> AudioCapture { get; private set; } = new AudioCapture<BufferFormat>(listener.Capture, deviceName, freq, format, bufferSize);

        public uint SampleRate
        {
            get => _sampleRate;
            set => SetField(ref _sampleRate, value);
        }
        public BufferFormat Format
        {
            get => _format;
            set => SetField(ref _format, value);
        }
        public int BufferSize
        {
            get => _bufferSize;
            set => SetField(ref _bufferSize, value);
        }
        public string? DeviceName
        {
            get => _deviceName;
            set => SetField(ref _deviceName, value);
        }

        public void StartCapture()
            => AudioCapture.Start();

        public void StopCapture()
            => AudioCapture.Stop();

        public unsafe bool Capture(ResourcePool<AudioData> dataPool, Queue<AudioData> buffers)
        {
            int samples = Listener.Capture?.GetAvailableSamples(Listener.DeviceHandle) ?? 0;
            if (samples < BufferSize)
                return false;

            var buffer = dataPool.Take();

            switch (Format)
            {
                case BufferFormat.Mono8:
                case BufferFormat.Stereo8:
                    {
                        bool stereo = Format == BufferFormat.Stereo8;
                        byte[] data = new byte[BufferSize];
                        fixed (byte* ptr = data)
                        {
                            Listener.Capture?.CaptureSamples(Listener.DeviceHandle, ptr, samples);
                        }
                        buffer.ChannelCount = stereo ? 2 : 1;
                        buffer.Data = DataSource.FromArray(data);
                        buffer.Frequency = (int)SampleRate;
                        buffer.Type = AudioData.EPCMType.Byte;
                    }
                    break;
                case BufferFormat.Mono16:
                case BufferFormat.Stereo16:
                    {
                        bool stereo = Format == BufferFormat.Stereo16;
                        short[] data = new short[BufferSize];
                        fixed (short* ptr = data)
                        {
                            Listener.Capture?.CaptureSamples(Listener.DeviceHandle, ptr, samples);
                        }
                        buffer.ChannelCount = stereo ? 2 : 1;
                        buffer.Data = DataSource.FromArray(data);
                        buffer.Frequency = (int)SampleRate;
                        buffer.Type = AudioData.EPCMType.Short;
                    }
                    break;
                default:
                    return false;
            }
            buffers.Enqueue(buffer);
            return true;
        }
    }
}