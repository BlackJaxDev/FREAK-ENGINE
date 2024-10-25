using Silk.NET.OpenAL.Extensions.EXT;
using XREngine.Core;
using XREngine.Data;
using XREngine.Data.Core;

namespace XREngine.Audio
{
    public class AudioInputDeviceFloat(
        ListenerContext listener,
        int bufferSize = 2048,
        uint freq = 22050,
        FloatBufferFormat format = FloatBufferFormat.Mono,
        string? deviceName = null) : XRBase
    {
        private uint _sampleRate = freq;
        private FloatBufferFormat _format = format;
        private int _bufferSize = bufferSize;
        private string? _deviceName = deviceName;

        public ListenerContext Listener { get; private set; } = listener;
        public AudioCapture<FloatBufferFormat> AudioCapture { get; private set; } = new AudioCapture<FloatBufferFormat>(listener.Capture, deviceName, freq, format, bufferSize);

        public uint SampleRate
        {
            get => _sampleRate;
            set => SetField(ref _sampleRate, value);
        }
        public FloatBufferFormat Format
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

            bool stereo = Format == FloatBufferFormat.Stereo;
            float[] data = new float[BufferSize];
            fixed (float* ptr = data)
            {
                Listener.Capture?.CaptureSamples(Listener.DeviceHandle, ptr, samples);
            }

            buffer.ChannelCount = stereo ? 2 : 1;
            buffer.Data = DataSource.FromArray(data);
            buffer.Frequency = (int)SampleRate;
            buffer.Type = AudioData.EPCMType.Float;

            return true;
        }
    }
}