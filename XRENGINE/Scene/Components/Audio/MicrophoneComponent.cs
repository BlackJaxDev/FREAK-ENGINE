using NAudio.Wave;
using System.Collections;

namespace XREngine.Components.Scene
{
    //[RequireComponents(typeof(AudioSourceComponent))]
    public class MicrophoneComponent : XRComponent
    {
        private const int DefaultBufferCapacity = 16;

        public AudioSourceComponent? GetAudioSourceComponent(bool forceCreate)
            => GetSiblingComponent<AudioSourceComponent>(forceCreate);

        private WaveInEvent? _waveIn;
        private int _deviceIndex = 0;
        private int _bufferMs = 30;
        private int _sampleRate = 44100;
        private int _bits = 8;
        private bool _receive = true;
        private bool _capture = true;

        private byte[] _currentBuffer = [];
        private int _currentBufferIndex = 0;
        private bool _muted = false;
        private float _lowerCutOff = 0.006f;

        /// <summary>
        /// Whether to capture and broadcast audio from the local microphone.
        /// </summary>
        public bool Capture
        {
            get => _capture;
            set => SetField(ref _capture, value);
        }
        /// <summary>
        /// Whether to receive audio from the remote microphone.
        /// </summary>
        public bool Receive
        {
            get => _receive;
            set => SetField(ref _receive, value);
        }
        /// <summary>
        /// The index of the audio device to capture from.
        /// Device 0 is the default device set in Windows.
        /// </summary>
        public int DeviceIndex
        {
            get => _deviceIndex;
            set => SetField(ref _deviceIndex, value);
        }
        /// <summary>
        /// The size of the buffer in milliseconds.
        /// </summary>
        public int BufferMs
        {
            get => _bufferMs;
            set => SetField(ref _bufferMs, value);
        }
        /// <summary>
        /// The sample rate of the audio.
        /// </summary>
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
        /// <summary>
        /// Whether the microphone is muted.
        /// Separate from Capture to allow for receiving audio while not broadcasting - faster than re-initializing the capture device.
        /// If enabled on the sending end, the audio will still be captured but not broadcast.
        /// If enabled on the receiving end, the audio will be received but not played.
        /// TODO: also notify the sending end to not send audio to this client if muted - wasted bandwidth.
        /// </summary>
        public bool Muted
        {
            get => _muted;
            set => SetField(ref _muted, value);
        }

        public float LowerCutOff
        {
            get => _lowerCutOff;
            set => SetField(ref _lowerCutOff, value);
        }

        public bool IsCapturing => _waveIn is not null;

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(DeviceIndex):
                case nameof(BufferMs):
                case nameof(SampleRate):
                case nameof(Capture):
                //case nameof(Bits):
                    if (IsCapturing && Capture)
                        StartCapture();
                    else
                        StopCapture();
                    break;
            }
        }

        public static string[] GetInputDeviceNames()
        {
            List<string> devices = [];
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                devices.Add(WaveInEvent.GetCapabilities(i).ProductName);
            return [.. devices];
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

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (Muted)
                return;

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

                    //If the buffer happens to be perfectly filled (edge case), queue it
                    if (_currentBufferIndex == _currentBuffer.Length)
                        ReplicateCurrentBuffer();
                }
                else
                {
                    //Consume remaining space from the available data
                    int remainingSpace = _currentBuffer.Length - _currentBufferIndex;
                    offset += remainingSpace;
                    remainingByteCount -= remainingSpace;

                    if (remainingSpace > 0)
                        Buffer.BlockCopy(e.Buffer, 0, _currentBuffer, _currentBufferIndex, remainingSpace);

                    ReplicateCurrentBuffer();
                }
            }
        }

        private bool VerifyLowerCutoff()
        {
            // If cutoff is nearly zero, then there's no need to check the samples.
            if (_lowerCutOff < float.Epsilon)
                return true;

            float sumSquares = 0.0f;
            // For 8-bit PCM, samples are unsigned bytes (0-255). The midpoint is 128.
            // We'll normalize each sample to [-1, 1] then compute the RMS.
            for (int i = 0; i < _currentBuffer.Length; i++)
            {
                float normalized = (_currentBuffer[i] - 128) / 127.0f;
                sumSquares += normalized * normalized;
            }

            float rmsSq = sumSquares / _currentBuffer.Length;
            //Debug.Out($"Mic RMS: {Math.Sqrt(rmsSq)}");
            return rmsSq >= _lowerCutOff * _lowerCutOff;
        }

        private void ReplicateCurrentBuffer()
        {
            if (!VerifyLowerCutoff())
                return;

            //Denoise(_currentBuffer);

            EnqueueDataReplication(nameof(_currentBuffer), _currentBuffer.ToArray(), false, false);
            _currentBufferIndex = 0;
        }

        //TODO: verify this works
        /// <summary>
        /// A simple moving average filter with a window size of 3 for noise reduction.
        /// </summary>
        /// <param name="currentBuffer"></param>
        private static void Denoise(byte[] currentBuffer)
        {
            // A simple moving average filter with a window size of 3 for noise reduction.
            int length = currentBuffer.Length;
            if (length < 3)
                return;

            byte[] smoothed = new byte[length];

            // Process the first sample: average of first two samples.
            smoothed[0] = (byte)((currentBuffer[0] + currentBuffer[1]) / 2);

            // Process middle samples with a 3-point moving average.
            for (int i = 1; i < length - 1; i++)
            {
                int sum = currentBuffer[i - 1] + currentBuffer[i] + currentBuffer[i + 1];
                smoothed[i] = (byte)(sum / 3);
            }

            // Process the last sample: average of the last two samples.
            smoothed[length - 1] = (byte)((currentBuffer[length - 2] + currentBuffer[length - 1]) / 2);

            // Copy the denoised data back into the current buffer.
            Buffer.BlockCopy(smoothed, 0, currentBuffer, 0, length);
        }

        public override void ReceiveData(string id, object data)
        {
            switch (id)
            {
                case nameof(_currentBuffer):
                    if (Receive && !Muted && data is ICollection col)
                        Task.Run(() => EnqueueStreamingAudio(col));
                    break;
            }
        }

        private void EnqueueStreamingAudio(ICollection col)
        {
            var buffer = new byte[col.Count];
            int i = 0;
            foreach (object b in col)
            {
                if (b is byte bVal)
                    buffer[i] = bVal;
                else if (byte.TryParse(b.ToString(), out byte sVal))
                    buffer[i] = sVal;
                i++;
            }
            BufferReceived?.Invoke(buffer);
            var audioSource = GetAudioSourceComponent(true)!;
            audioSource.EnqueueStreamingBuffers(SampleRate, false, buffer);
            audioSource.Play();
        }

        public event Action<byte[]>? BufferReceived;

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

            string[] devices = GetInputDeviceNames();
            if (devices.Length == 0)
            {
                Debug.Out("No audio input devices found.");
                return;
            }
            else
                Debug.Out($"Available audio input devices:{Environment.NewLine}{string.Join(Environment.NewLine, devices)}");

            DeviceIndex = Math.Clamp(DeviceIndex, 0, devices.Length - 1);

            if (Capture)
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
