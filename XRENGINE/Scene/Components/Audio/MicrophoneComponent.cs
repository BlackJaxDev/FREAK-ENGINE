using NAudio.Wave;
using XREngine.Core.Attributes;

namespace XREngine.Components.Scene
{
    [RequireComponents(typeof(AudioSourceComponent))]
    public class MicrophoneComponent : XRComponent
    {
        private const int DefaultBufferCapacity = 16;

        private WaveInEvent? _waveIn;
        private int _deviceIndex = 0;
        private int _bufferMs = 100;
        private int _sampleRate = 44100;
        private int _bits = 8;

        public Queue<byte[]> BufferQueue { get; } = new Queue<byte[]>();

        public int DeviceIndex
        {
            get => _deviceIndex;
            set => SetField(ref _deviceIndex, value);
        }
        public int BufferMs
        {
            get => _bufferMs;
            set => SetField(ref _bufferMs, value);
        }
        public int SampleRate
        {
            get => _sampleRate;
            set => SetField(ref _sampleRate, value);
        }
        //public int Bits
        //{
        //    get => _bits;
        //    set => SetField(ref _bits, value);
        //}

        public bool IsCapturing => _waveIn is not null;

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(DeviceIndex):
                case nameof(BufferMs):
                case nameof(SampleRate):
                //case nameof(Bits):
                    if (IsCapturing)
                        StartCapture();
                    break;
            }
        }

        public void StartCapture()
        {
            if (_waveIn is not null)
                StopCapture();

            _waveIn = new WaveInEvent
            {
                DeviceNumber = DeviceIndex,
                WaveFormat = new WaveFormat(SampleRate, _bits, channels: 1), //TODO: support different formats other than pcm16
                BufferMilliseconds = BufferMs
            };
            //Allocate 100 ms worth of buffer space
            int bufferSize = SampleRate * _bits / 8 * BufferMs / 1000;
            //Explanation: ((Samples / 1 Second) * (Bits / 1 Sample) / 8) * (BufferMs / 1000) = bytes per second * seconds = bytes
            _currentBuffer = new byte[bufferSize];
            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.StartRecording();

            //InputDevice.StartCapture();
            //RegisterTick(ETickGroup.Normal, ETickOrder.Input, CaptureSamples);
        }
        public void StopCapture()
        {
            if (_waveIn is null)
                return;

            _waveIn.DataAvailable -= WaveIn_DataAvailable;
            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;

            //InputDevice.StopCapture();
        }

        private byte[] _currentBuffer = [];
        private int _currentBufferIndex = 0;
        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            bool buffersQueued = false;
            int remainingByteCount = e.BytesRecorded;
            int offset = 0;
            while (remainingByteCount > 0)
            {
                int endIndex = _currentBufferIndex + remainingByteCount;
                if (endIndex < _currentBuffer.Length)
                {
                    //If the buffer has enough space, just copy the data and move on with our life
                    Buffer.BlockCopy(e.Buffer, offset, _currentBuffer, _currentBufferIndex, remainingByteCount);
                    _currentBufferIndex += remainingByteCount;
                    remainingByteCount = 0;

                    //If the buffer is full, queue it
                    if (_currentBufferIndex == _currentBuffer.Length)
                    {
                        buffersQueued = true;
                        Queue();
                    }
                }
                else
                {
                    //Consume remaining space from the available data
                    int remainingSpace = _currentBuffer.Length - _currentBufferIndex;
                    offset += remainingSpace;
                    remainingByteCount -= remainingSpace;

                    if (remainingSpace > 0)
                        Buffer.BlockCopy(e.Buffer, 0, _currentBuffer, _currentBufferIndex, remainingSpace);

                    buffersQueued = true;
                    Queue();
                }
            }

            if (buffersQueued)
                EnqueuePropertyReplication(nameof(BufferQueue), BufferQueue, true);
        }

        private void Queue()
        {
            BufferQueue.Enqueue(_currentBuffer);
            NewBufferQueued?.Invoke();
            _currentBuffer = new byte[_currentBuffer.Length];
            _currentBufferIndex = 0;

            var comp = GetSiblingComponent<AudioSourceComponent>(true)!;
            comp.EnqueueStreamingBuffers(SampleRate, false, _currentBuffer);
        }

        public event Action? NewBufferQueued;

        //public byte[] GetByteBuffer()
        //{
        //    var buffer = GetBuffer();
        //    if (buffer is null)
        //        return [];

        //    byte[] data = buffer.GetByteData();
        //    _bufferPool.Release(buffer);
        //    return data;
        //}

        //public short[] GetShortBuffer()
        //{
        //    var buffer = GetBuffer();
        //    if (buffer is null)
        //        return [];

        //    return buffer.GetShortData();
        //}

        //public float[] GetFloatData()
        //{
        //    var buffer = GetBuffer();
        //    if (buffer is null)
        //        return [];

        //    float[] data = buffer.GetFloatData();
        //    _bufferPool.Release(buffer);
        //    return data;
        //}

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            GetSiblingComponent<AudioSourceComponent>(true)!.Type = Audio.AudioSource.ESourceType.Streaming;
            StartCapture();
        }
        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();
            StopCapture();
        }

        //I'm not using OpenAL's capture because I don't know how to select a specific device with it.

        //public void SetAudioDevice(int deviceNumber)
        //{
        //    //InputDevice = new AudioInputDevice(ListenerContext.Default, 2048, 22050, BufferFormat.Mono16, GetAudioDevices().ElementAt(deviceNumber).FriendlyName);
        //}

        //public AudioInputDevice InputDevice { get; set; }
        //public MicrophoneComponent()
        //{

        //    //InputDevice = new AudioInputDevice(ListenerContext.Default, 2048, 22050, BufferFormat.Mono16);
        //}

        //public void StartCapture()
        //{
        //    InputDevice.StartCapture();
        //}

        //public void CaptureSamples()
        //{
        //    //InputDevice.Capture(_bufferPool, BufferQueue);
        //}
    }
}
