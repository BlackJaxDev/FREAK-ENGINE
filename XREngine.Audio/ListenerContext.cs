﻿using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Creative;
using Silk.NET.OpenAL.Extensions.Enumeration;
using Silk.NET.OpenAL.Extensions.EXT;
using System.Diagnostics;
using System.Numerics;
using XREngine.Core;

namespace XREngine.Audio
{
    public sealed unsafe class ListenerContext : IDisposable
    {
        //TODO: implement audio source priority
        //destroy sources with lower priority first to make room for higher priority sources.
        //0 is the lowest priority, 255 is the highest priority.

        public static AL Api { get; } = AL.GetApi();
        public static ALContext Context { get; } = ALContext.GetApi(true);

        internal Device* DeviceHandle { get; }
        internal Context* ContextHandle { get; }

        public EffectContext? Effects { get; } = null;
        public VorbisFormat? VorbisFormat { get; } = null;
        public MP3Format? MP3Format { get; } = null;
        public XRam? XRam { get; } = null;
        public MultiChannelBuffers? MultiChannel { get; } = null;
        public DoubleFormat? DoubleFormat { get; } = null;
        public MULAWFormat? MuLawFormat { get; } = null;
        public FloatFormat? FloatFormat { get; } = null;
        public MCFormats? MCFormats { get; } = null;
        public ALAWFormat? ALawFormat { get; } = null;

        public Capture? Capture { get; } = null;

        public EventDictionary<uint, AudioSource> Sources { get; } = [];
        public EventDictionary<uint, AudioBuffer> Buffers { get; } = [];

        internal ListenerContext()
        {
            SourcePool = new ResourcePool<AudioSource>(() => new AudioSource(this));
            BufferPool = new ResourcePool<AudioBuffer>(() => new AudioBuffer(this));

            if (Api.TryGetExtension<VorbisFormat>(out var vorbisFormat))
                VorbisFormat = vorbisFormat;
            if (Api.TryGetExtension<MP3Format>(out var mp3Format))
                MP3Format = mp3Format;
            if (Api.TryGetExtension<MultiChannelBuffers>(out var multiChannel))
                MultiChannel = multiChannel;
            if (Api.TryGetExtension<DoubleFormat>(out var doubleFormat))
                DoubleFormat = doubleFormat;
            if (Api.TryGetExtension<MULAWFormat>(out var mulawFormat))
                MuLawFormat = mulawFormat;
            if (Api.TryGetExtension<FloatFormat>(out var floatFormat))
                FloatFormat = floatFormat;
            if (Api.TryGetExtension<MCFormats>(out var mcFormats))
                MCFormats = mcFormats;
            if (Api.TryGetExtension<ALAWFormat>(out var alawFormat))
                ALawFormat = alawFormat;

            if (Api.TryGetExtension<EffectExtension>(out var effectExtension))
                Effects = new EffectContext(this, effectExtension);
            if (Api.TryGetExtension<XRam>(out var xram))
                XRam = xram;

            string deviceSpecifier = "";
            if (Context.TryGetExtension<Enumeration>(null, out var e))
            {
                var stringList = e.GetStringList(GetEnumerationContextStringList.DeviceSpecifiers);
                foreach (var device in stringList)
                {
                    Debug.WriteLine($"Found audio device \"{device}\"");
                    deviceSpecifier = device;
                }
                e.Dispose();
            }

            DeviceHandle = Context.OpenDevice(null);
            ContextHandle = Context.CreateContext(DeviceHandle, null);
            if (Context.TryGetExtension<Capture>(DeviceHandle, out var captureExtension))
                Capture = captureExtension;
            MakeCurrent();
            VerifyError();
        }

        public static ListenerContext? CurrentContext { get; private set; }

        public void MakeCurrent()
        {
            if (CurrentContext == this)
                return;

            CurrentContext = this;
            Context.MakeContextCurrent(ContextHandle);
        }

        public void VerifyError()
        {
            if (CurrentContext != this)
                return;

            var error = Api.GetError();
            if (error != AudioError.NoError)
                throw new Exception($"{error}");
        }

        private ResourcePool<AudioSource> SourcePool { get; }
        private ResourcePool<AudioBuffer> BufferPool { get; }

        public AudioSource TakeSource()
        {
            var source = SourcePool.Take();
            Sources.Add(source.Handle, source);
            VerifyError();
            return source;
        }
        public AudioBuffer TakeBuffer()
        {
            var buffer = BufferPool.Take();
            Buffers.Add(buffer.Handle, buffer);
            VerifyError();
            return buffer;
        }

        public void ReleaseSource(AudioSource source)
        {
            Sources.Remove(source.Handle);
            SourcePool.Release(source);
            VerifyError();
        }
        public void ReleaseBuffer(AudioBuffer buffer)
        {
            Buffers.Remove(buffer.Handle);
            BufferPool.Release(buffer);
            VerifyError();
        }

        public void DestroyUnusedSources(int count)
            => SourcePool.Destroy(count);
        public void DestroyUnusedBuffers(int count)
            => BufferPool.Destroy(count);

        public AudioSource? GetSourceByHandle(uint handle)
            => Sources.TryGetValue(handle, out AudioSource? source) ? source : null;
        public AudioBuffer? GetBufferByHandle(uint handle)
            => Buffers.TryGetValue(handle, out AudioBuffer? buffer) ? buffer : null;

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
        {
            MakeCurrent();
            Api.SetListenerProperty(ListenerVector3.Position, position);
            VerifyError();
        }
        private void SetVelocity(Vector3 velocity)
        {
            MakeCurrent();
            Api.SetListenerProperty(ListenerVector3.Velocity, velocity);
            VerifyError();
        }

        private Vector3 GetPosition()
        {
            MakeCurrent();
            Api.GetListenerProperty(ListenerVector3.Position, out Vector3 position);
            VerifyError();
            return position;
        }
        private Vector3 GetVelocity()
        {
            MakeCurrent();
            Api.GetListenerProperty(ListenerVector3.Velocity, out Vector3 velocity);
            VerifyError();
            return velocity;
        }

        /// <summary>
        /// Gets both the forward and up vectors of the listener.
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="up"></param>
        public unsafe void SetOrientation(Vector3 forward, Vector3 up)
        {
            MakeCurrent();
            float[] orientation = [forward.X, forward.Y, forward.Z, up.X, up.Y, up.Z];
            fixed (float* pOrientation = orientation)
                Api.SetListenerProperty(ListenerFloatArray.Orientation, pOrientation);
            VerifyError();
        }

        /// <summary>
        /// Sets both the forward and up vectors of the listener.
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="up"></param>
        public unsafe void GetOrientation(out Vector3 forward, out Vector3 up)
        {
            MakeCurrent();
            float[] orientation = new float[6];
            fixed (float* pOrientation = orientation)
                Api.GetListenerProperty(ListenerFloatArray.Orientation, pOrientation);
            VerifyError();
            forward = new Vector3(orientation[0], orientation[1], orientation[2]);
            up = new Vector3(orientation[3], orientation[4], orientation[5]);
        }

        private void SetGain(float gain)
        {
            MakeCurrent();
            Api.SetListenerProperty(ListenerFloat.Gain, gain);
            VerifyError();
        }
        private float GetGain()
        {
            MakeCurrent();
            Api.GetListenerProperty(ListenerFloat.Gain, out float gain);
            VerifyError();
            return gain;
        }

        private float GetDopplerFactor()
        {
            MakeCurrent();
            var factor = Api.GetStateProperty(StateFloat.DopplerFactor);
            VerifyError();
            return factor;
        }
        private float GetSpeedOfSound()
        {
            MakeCurrent();
            var speed = Api.GetStateProperty(StateFloat.SpeedOfSound);
            VerifyError();
            return speed;
        }
        private DistanceModel GetDistanceModel()
        {
            MakeCurrent();
            var model = (DistanceModel)Api.GetStateProperty(StateInteger.DistanceModel);
            VerifyError();
            return model;
        }

        private void SetDopplerFactor(float factor)
        {
            MakeCurrent();
            Api.DopplerFactor(factor);
            VerifyError();
        }
        private void SetSpeedOfSound(float speed)
        {
            MakeCurrent();
            Api.SpeedOfSound(speed);
            VerifyError();
        }
        private void SetDistanceModel(DistanceModel model)
        {
            MakeCurrent();
            Api.DistanceModel(model);
            VerifyError();
            _calcGainDistModelFunc = model switch
            {
                DistanceModel.InverseDistance => CalcInvDistGain,
                DistanceModel.InverseDistanceClamped => CalcInvDistGainClamped,
                DistanceModel.LinearDistance => CalcLinearGain,
                DistanceModel.LinearDistanceClamped => CalcLinearGainClamped,
                DistanceModel.ExponentDistance => CalcExpDistGain,
                DistanceModel.ExponentDistanceClamped => CalcExpDistGainClamped,
                _ => null,
            };
        }

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

        private delegate float DelCalcGainDistModel(float distance, float referenceDistance, float maxDistance, float rolloffFactor);
        private DelCalcGainDistModel? _calcGainDistModelFunc = null;

        public float CalcGain(Vector3 worldPosition, float referenceDistance, float maxDistance, float rolloffFactor)
            => _calcGainDistModelFunc?.Invoke(Vector3.Distance(worldPosition, Position), referenceDistance, maxDistance, rolloffFactor) ?? 1.0f;

        private static float ClampDist(float dist, float refDist, float maxDist)
            => Math.Max(refDist, Math.Min(dist, maxDist));

        private static float CalcExpDistGainClamped(float dist, float refDist, float maxDist, float rolloff)
            => CalcExpDistGain(ClampDist(dist, refDist, maxDist), refDist, maxDist, rolloff);
        private static float CalcExpDistGain(float dist, float refDist, float maxDist, float rolloff)
            => MathF.Pow(dist / refDist, -rolloff);

        private static float CalcLinearGainClamped(float dist, float refDist, float maxDist, float rolloff)
            => CalcLinearGain(ClampDist(dist, refDist, maxDist), refDist, maxDist, rolloff);
        private static float CalcLinearGain(float dist, float refDist, float maxDist, float rolloff)
            => 1.0f - rolloff * (dist - refDist) / (maxDist - refDist);

        private static float CalcInvDistGainClamped(float dist, float refDist, float maxDist, float rolloff)
            => CalcInvDistGain(ClampDist(dist, refDist, maxDist), refDist, maxDist, rolloff);
        private static float CalcInvDistGain(float dist, float refDist, float maxDist, float rolloff)
            => refDist / (refDist + rolloff * (dist - refDist));
    }
}