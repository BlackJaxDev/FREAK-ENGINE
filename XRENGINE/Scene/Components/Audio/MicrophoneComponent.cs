using XREngine.Audio;
using XREngine.Core;
using XREngine.Data;

namespace XREngine.Components.Scene
{
    public class MicrophoneComponent : AudioSourceComponent
    {
        private const int DefaultBufferCapacity = 16;
        public AudioInputDevice InputDevice { get; set; }

        private readonly ResourcePool<AudioData> _bufferPool = new(() => new AudioData(), DefaultBufferCapacity);

        public Queue<AudioData> BufferQueue { get; } = new Queue<AudioData>();

        public int BufferCapacity
        {
            get => _bufferPool.Capacity;
            set => _bufferPool.Capacity = value;
        }

        public MicrophoneComponent()
        {
            //InputDevice = new AudioInputDevice(ListenerContext.Default, 2048, 22050, BufferFormat.Mono16);
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            RegisterTick(ETickGroup.Normal, ETickOrder.Input, CaptureSamples);
        }

        public void CaptureSamples()
        {
            InputDevice.Capture(_bufferPool, BufferQueue);
        }
    }
}
