using Silk.NET.OpenAL;
using System.Numerics;
using XREngine.Core;

namespace XREngine.Audio
{
    public sealed class ListenerContext : IDisposable
    {
        //TODO: implement audio source priority
        //destroy sources with lower priority first to make room for higher priority sources.
        //0 is the lowest priority, 255 is the highest priority.

        internal AL Api { get; } = AL.GetApi();
        public EventDictionary<uint, AudioSource> Sources { get; } = [];
        public EventDictionary<uint, AudioBuffer> Buffers { get; } = [];

        internal ListenerContext()
        {
            SourcePool = new ResourcePool<AudioSource>(() => new AudioSource(this));
            BufferPool = new ResourcePool<AudioBuffer>(() => new AudioBuffer(this));
        }

        private ResourcePool<AudioSource> SourcePool { get; }
        private ResourcePool<AudioBuffer> BufferPool { get; }

        public AudioSource TakeSource()
            => SourcePool.Take();
        public AudioBuffer TakeBuffer()
            => BufferPool.Take();

        public void ReleaseSource(AudioSource source)
            => SourcePool.Release(source);
        public void ReleaseBuffer(AudioBuffer buffer)
            => BufferPool.Release(buffer);

        public void DestroyUnusedSources(int count)
            => SourcePool.Destroy(count);
        public void DestroyUnusedBuffers(int count)
            => BufferPool.Destroy(count);

        public AudioSource? GetSourceByHandle(uint handle)
            => Sources.TryGetValue(handle, out AudioSource source) ? source : null;
        public AudioBuffer? GetBufferByHandle(uint handle)
            => Buffers.TryGetValue(handle, out AudioBuffer buffer) ? buffer : null;

        public AudioError GetError()
            => Api.GetError();

        public bool IsExtensionPresent(string extension)
            => Api.IsExtensionPresent(extension);

        public bool HasDopplerFactorSet()
            => Api.GetStateProperty(StateBoolean.HasDopplerFactor);
        public bool HasDopplerVelocitySet()
            => Api.GetStateProperty(StateBoolean.HasDopplerVelocity);
        public bool HasSpeedOfSoundSet()
            => Api.GetStateProperty(StateBoolean.HasSpeedOfSound);
        public bool IsDistanceModelInverseDistanceClamped()
            => Api.GetStateProperty(StateBoolean.IsDistanceModelInverseDistanceClamped);

        public string GetVendor()
            => Api.GetStateProperty(StateString.Vendor);
        public string GetRenderer()
            => Api.GetStateProperty(StateString.Renderer);
        public string GetVersion()
            => Api.GetStateProperty(StateString.Version);
        public string[] GetExtensions()
            => Api.GetStateProperty(StateString.Extensions).Split(' ');

        public float DopplerFactor
        {
            get => GetDopplerFactor();
            set => SetDopplerFactor(value);
        }
        public float SpeedOfSound
        {
            get => GetSpeedOfSound();
            set => SetSpeedOfSound(value);
        }
        public DistanceModel DistanceModel
        {
            get => GetDistanceModel();
            set => SetDistanceModel(value);
        }

        public float Gain
        {
            get => GetGain();
            set => SetGain(value);
        }
        public Vector3 Position
        {
            get => GetPosition();
            set => SetPosition(value);
        }
        public Vector3 Velocity
        {
            get => GetVelocity();
            set => SetVelocity(value);
        }

        public Vector3 Up
        {
            get
            {
                GetOrientation(out _, out Vector3 up);
                return up;
            }
            set => SetOrientation(Forward, value);
        }

        public Vector3 Forward
        {
            get
            {
                GetOrientation(out Vector3 forward, out _);
                return forward;
            }
            set => SetOrientation(value, Up);
        }

        private void SetPosition(Vector3 position)
            => Api.SetListenerProperty(ListenerVector3.Position, position);
        private void SetVelocity(Vector3 velocity)
            => Api.SetListenerProperty(ListenerVector3.Velocity, velocity);

        private Vector3 GetPosition()
        {
            Api.GetListenerProperty(ListenerVector3.Position, out Vector3 position);
            return position;
        }
        private Vector3 GetVelocity()
        {
            Api.GetListenerProperty(ListenerVector3.Velocity, out Vector3 velocity);
            return velocity;
        }

        /// <summary>
        /// Gets both the forward and up vectors of the listener.
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="up"></param>
        public unsafe void SetOrientation(Vector3 forward, Vector3 up)
        {
            float[] orientation = [forward.X, forward.Y, forward.Z, up.X, up.Y, up.Z];
            fixed (float* pOrientation = orientation)
                Api.SetListenerProperty(ListenerFloatArray.Orientation, pOrientation);
        }

        /// <summary>
        /// Sets both the forward and up vectors of the listener.
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="up"></param>
        public unsafe void GetOrientation(out Vector3 forward, out Vector3 up)
        {
            float[] orientation = new float[6];
            fixed (float* pOrientation = orientation)
                Api.GetListenerProperty(ListenerFloatArray.Orientation, pOrientation);
            forward = new Vector3(orientation[0], orientation[1], orientation[2]);
            up = new Vector3(orientation[3], orientation[4], orientation[5]);
        }

        private void SetGain(float gain)
            => Api.SetListenerProperty(ListenerFloat.Gain, gain);
        private float GetGain()
        {
            Api.GetListenerProperty(ListenerFloat.Gain, out float gain);
            return gain;
        }

        private float GetDopplerFactor()
            => Api.GetStateProperty(StateFloat.DopplerFactor);
        private float GetSpeedOfSound()
            => Api.GetStateProperty(StateFloat.SpeedOfSound);
        private DistanceModel GetDistanceModel()
            => (DistanceModel)Api.GetStateProperty(StateInteger.DistanceModel);

        private void SetDopplerFactor(float factor)
            => Api.DopplerFactor(factor);
        private void SetSpeedOfSound(float speed)
            => Api.SpeedOfSound(speed);
        private void SetDistanceModel(DistanceModel model)
            => Api.DistanceModel(model);

        public event Action<ListenerContext>? Disposed;

        public void Dispose()
        {
            foreach (AudioSource source in Sources.Values)
                source.Dispose();
            foreach (AudioBuffer buffer in Buffers.Values)
                buffer.Dispose();
            Sources.Clear();
            Buffers.Clear();
            SourcePool.Destroy(int.MaxValue);
            BufferPool.Destroy(int.MaxValue);
            Disposed?.Invoke(this);
            GC.SuppressFinalize(this);
        }
    }
}