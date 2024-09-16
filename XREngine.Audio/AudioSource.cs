﻿using Silk.NET.OpenAL;
using System.Numerics;
using XREngine.Core;

namespace XREngine.Audio
{
    public sealed class AudioSource : IDisposable, IPoolable
    {
        internal AudioSource(ListenerContext parentListener)
        {
            ParentListener = parentListener;
            Handle = parentListener.Api.GenSource();
        }

        public ListenerContext ParentListener { get; }
        public AL Api => ParentListener.Api;
        private uint Handle { get; }

        public void Dispose()
        {
            Api.DeleteSource(Handle);
            GC.SuppressFinalize(this);
        }

        #region Buffers
        /// <summary>
        /// The number of buffers queued on this source.
        /// </summary>
        public int BuffersQueued
            => GetBuffersQueued();
        /// <summary>
        /// The number of buffers in the queue that have been processed.
        /// </summary>
        public int BuffersProcessed
            => GetBuffersProcessed();
        /// <summary>
        /// The buffer that the source is playing from.
        /// </summary>
        public AudioBuffer? Buffer
        {
            get => ParentListener.GetBufferByHandle(GetBufferHandle());
            set
            {
                if (value is not null)
                    SetBufferHandle(value.Handle);
                else
                    SetBufferHandle(0); //TODO: is a handle of 0 invalid?
            }
        }
        public unsafe void QueueBuffers(params AudioBuffer[] buffers)
        {
            uint[] handles = new uint[buffers.Length];
            for (int i = 0; i < buffers.Length; i++)
                handles[i] = buffers[i].Handle;
            fixed (uint* pBuffers = handles)
                Api.SourceQueueBuffers(Handle, buffers.Length, pBuffers);
        }
        public unsafe void UnqueueBuffers(params AudioBuffer[] buffers)
        {
            uint[] handles = new uint[buffers.Length];
            for (int i = 0; i < buffers.Length; i++)
                handles[i] = buffers[i].Handle;
            fixed (uint* pBuffers = handles)
                Api.SourceUnqueueBuffers(Handle, buffers.Length, pBuffers);
        }
        #endregion

        #region Offset
        /// <summary>
        /// The playback position, expressed in seconds.
        /// </summary>
        public float SecondsOffset
        {
            get => GetSecOffset();
            set => SetSecOffset(value);
        }
        /// <summary>
        /// The offset in bytes of the source.
        /// </summary>
        public int ByteOffset
        {
            get => GetByteOffset();
            set => SetByteOffset(value);
        }
        /// <summary>
        /// The offset in samples of the source.
        /// </summary>
        public int SampleOffset
        {
            get => GetSampleOffset();
            set => SetSampleOffset(value);
        }
        #endregion

        public enum ESourceState
        {
            Initial,
            Playing,
            Paused,
            Stopped,
        }

        public enum ESourceType
        {
            Static,
            Streaming,
            Undetermined,
        }

        private static ESourceState ConvSourceState(SourceState state)
            => state switch
            {
                Silk.NET.OpenAL.SourceState.Initial => ESourceState.Initial,
                Silk.NET.OpenAL.SourceState.Playing => ESourceState.Playing,
                Silk.NET.OpenAL.SourceState.Paused => ESourceState.Paused,
                Silk.NET.OpenAL.SourceState.Stopped => ESourceState.Stopped,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
            };

        public static SourceState ConvSourceState(ESourceState state)
            => state switch
            {
                ESourceState.Initial => Silk.NET.OpenAL.SourceState.Initial,
                ESourceState.Playing => Silk.NET.OpenAL.SourceState.Playing,
                ESourceState.Paused => Silk.NET.OpenAL.SourceState.Paused,
                ESourceState.Stopped => Silk.NET.OpenAL.SourceState.Stopped,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
            };

        public static ESourceType ConvSourceType(SourceType type)
            => type switch
            {
                Silk.NET.OpenAL.SourceType.Static => ESourceType.Static,
                Silk.NET.OpenAL.SourceType.Streaming => ESourceType.Streaming,
                Silk.NET.OpenAL.SourceType.Undetermined => ESourceType.Undetermined,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

        public static SourceType ConvSourceType(ESourceType type)
            => type switch
            {
                ESourceType.Static => Silk.NET.OpenAL.SourceType.Static,
                ESourceType.Streaming => Silk.NET.OpenAL.SourceType.Streaming,
                ESourceType.Undetermined => Silk.NET.OpenAL.SourceType.Undetermined,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

        #region State
        public bool IsPlaying
            => SourceState == ESourceState.Playing;
        public bool IsStopped
            => SourceState == ESourceState.Stopped;
        public bool IsPaused
            => SourceState == ESourceState.Paused;
        public bool IsInitial
            => SourceState == ESourceState.Initial;
        /// <summary>
        /// The state of the source (Stopped, Playing, etc).
        /// </summary>
        public ESourceState SourceState
            => ConvSourceState((SourceState)GetSourceState());
        /// <summary>
        /// Plays the source.
        /// </summary>
        public void Play()
            => Api.SourcePlay(Handle);
        /// <summary>
        /// Stops the source from playing.
        /// </summary>
        public void Stop()
            => Api.SourceStop(Handle);
        /// <summary>
        /// Pauses the source.
        /// </summary>
        public void Pause()
            => Api.SourcePause(Handle);
        /// <summary>
        /// Sets the source to play from the beginning (initial state).
        /// </summary>
        public void Rewind()
            => Api.SourceRewind(Handle);
        #endregion

        #region Settings

        /// <summary>
        /// The type of the source, either Static, Streaming, or undetermined.
        /// </summary>
        public ESourceType SourceType
        {
            get => ConvSourceType((SourceType)GetSourceType());
            set => SetSourceType((int)ConvSourceType(value));
        }
        /// <summary>
        /// If true, the source's position is relative to the listener.
        /// If false, the source's position is in world space.
        /// </summary>
        public bool RelativeToListener
        {
            get => GetSourceRelative();
            set => SetSourceRelative(value);
        }
        /// <summary>
        /// If true, the source will loop.
        /// </summary>
        public bool Looping
        {
            get => GetLooping();
            set => SetLooping(value);
        }
        /// <summary>
        /// How far the source is from the listener.
        /// At 0.0f, no distance attenuation occurs.
        /// Default: 1.0f.
        /// Range: [0.0f - float.PositiveInfinity] 
        /// </summary>
        public float ReferenceDistance
        {
            get => GetReferenceDistance();
            set => SetReferenceDistance(value);
        }
        /// <summary>
        /// The distance above which sources are not attenuated using the inverse clamped distance model.
        /// Default: float.PositiveInfinity
        /// Range: [0.0f - float.PositiveInfinity]
        /// </summary>
        public float MaxDistance
        {
            get => GetMaxDistance();
            set => SetMaxDistance(value);
        }
        /// <summary>
        /// The rolloff factor of the source.
        /// Rolloff factor is the rate at which the source's volume decreases as it moves further from the listener.
        /// Range: [0.0f - float.PositiveInfinity]
        /// </summary>
        public float RolloffFactor
        {
            get => GetRolloffFactor();
            set => SetRolloffFactor(value);
        }
        /// <summary>
        /// The pitch of the source.
        /// Default: 1.0f
        /// Range: [0.5f - 2.0f]
        /// </summary>
        public float Pitch
        {
            get => GetPitch();
            set => SetPitch(value);
        }
        /// <summary>
        /// The minimum gain of the source.
        /// Range: [0.0f - 1.0f] (Logarithmic)
        /// </summary>
        public float MinGain
        {
            get => GetMinGain();
            set => SetMinGain(value);
        }
        /// <summary>
        /// The maximum gain of the source.
        /// Range: [0.0f - 1.0f] (Logarithmic)
        /// </summary>
        public float MaxGain
        {
            get => GetMaxGain();
            set => SetMaxGain(value);
        }
        /// <summary>
        /// The gain (volume) of the source.
        /// A value of 1.0 means un-attenuated/unchanged.
        /// Each division by 2 equals an attenuation of -6dB.
        /// Each multiplication with 2 equals an amplification of +6dB.
        /// A value of 0.0f is meaningless with respect to a logarithmic scale; it is interpreted as zero volume - the channel is effectively disabled.
        /// </summary>
        public float Gain
        {
            get => GetGain();
            set => SetGain(value);
        }
        /// <summary>
        /// Directional source, inner cone angle, in degrees.
        /// Default: 360
        /// Range: [0-360]
        /// </summary>
        public float ConeInnerAngle
        {
            get => GetConeInnerAngle();
            set => SetConeInnerAngle(value);
        }
        /// <summary>
        /// Directional source, outer cone angle, in degrees.
        /// Default: 360
        /// Range: [0-360]
        /// </summary>
        public float ConeOuterAngle
        {
            get => GetConeOuterAngle();
            set => SetConeOuterAngle(value);
        }
        /// <summary>
        /// Directional source, outer cone gain.
        /// Default: 0.0f
        /// Range: [0.0f - 1.0] (Logarithmic)
        /// </summary>
        public float ConeOuterGain
        {
            get => GetConeOuterGain();
            set => SetConeOuterGain(value);
        }
        #endregion

        #region Location
        /// <summary>
        /// The position of the source in world space.
        /// </summary>
        public Vector3 Position
        {
            get => GetPosition();
            set => SetPosition(value);
        }
        /// <summary>
        /// The velocity of the source.
        /// </summary>
        public Vector3 Velocity
        {
            get => GetVelocity();
            set => SetVelocity(value);
        }
        /// <summary>
        /// The direction the source is facing.
        /// </summary>
        public Vector3 Direction
        {
            get => GetDirection();
            set => SetDirection(value);
        }
        #endregion

        #region Get / Set Methods
        private bool GetSourceRelative()
        {
            Api.GetSourceProperty(Handle, SourceBoolean.SourceRelative, out bool value);
            return value;
        }
        private bool GetLooping()
        {
            Api.GetSourceProperty(Handle, SourceBoolean.Looping, out bool value);
            return value;
        }
        private void SetSourceRelative(bool relative)
            => Api.SetSourceProperty(Handle, SourceBoolean.SourceRelative, relative);
        private void SetLooping(bool loop)
            => Api.SetSourceProperty(Handle, SourceBoolean.Looping, loop);

        private int GetByteOffset()
        {
            Api.GetSourceProperty(Handle, GetSourceInteger.ByteOffset, out int value);
            return value;
        }
        private int GetSampleOffset()
        {
            Api.GetSourceProperty(Handle, GetSourceInteger.SampleOffset, out int value);
            return value;
        }
        private unsafe uint GetBufferHandle()
        {
            uint buffer;
            Api.GetSourceProperty(Handle, GetSourceInteger.Buffer, (int*)&buffer);
            return buffer;
        }
        private int GetSourceType()
        {
            Api.GetSourceProperty(Handle, GetSourceInteger.SourceType, out int value);
            return value;
        }
        private int GetSourceState()
        {
            Api.GetSourceProperty(Handle, GetSourceInteger.SourceState, out int value);
            return value;
        }
        private int GetBuffersQueued()
        {
            Api.GetSourceProperty(Handle, GetSourceInteger.BuffersQueued, out int value);
            return value;
        }
        private int GetBuffersProcessed()
        {
            Api.GetSourceProperty(Handle, GetSourceInteger.BuffersProcessed, out int value);
            return value;
        }

        private void SetByteOffset(int offset)
            => Api.SetSourceProperty(Handle, SourceInteger.ByteOffset, offset);
        private void SetSampleOffset(int offset)
            => Api.SetSourceProperty(Handle, SourceInteger.SampleOffset, offset);
        private void SetBufferHandle(uint buffer)
            => Api.SetSourceProperty(Handle, SourceInteger.Buffer, buffer);
        private void SetSourceType(int type)
            => Api.SetSourceProperty(Handle, SourceInteger.SourceType, type);

        private Vector3 GetPosition()
        {
            Api.GetSourceProperty(Handle, SourceVector3.Position, out Vector3 value);
            return value;
        }
        private Vector3 GetVelocity()
        {
            Api.GetSourceProperty(Handle, SourceVector3.Velocity, out Vector3 value);
            return value;
        }
        private Vector3 GetDirection()
        {
            Api.GetSourceProperty(Handle, SourceVector3.Direction, out Vector3 value);
            return value;
        }

        private void SetPosition(Vector3 position)
            => Api.SetSourceProperty(Handle, SourceVector3.Position, position);
        private void SetVelocity(Vector3 velocity)
            => Api.SetSourceProperty(Handle, SourceVector3.Velocity, velocity);
        private void SetDirection(Vector3 direction)
            => Api.SetSourceProperty(Handle, SourceVector3.Direction, direction);

        private float GetReferenceDistance()
        {
            Api.GetSourceProperty(Handle, SourceFloat.ReferenceDistance, out float value);
            return value;
        }
        private float GetMaxDistance()
        {
            Api.GetSourceProperty(Handle, SourceFloat.MaxDistance, out float value);
            return value;
        }
        private float GetRolloffFactor()
        {
            Api.GetSourceProperty(Handle, SourceFloat.RolloffFactor, out float value);
            return value;
        }
        private float GetPitch()
        {
            Api.GetSourceProperty(Handle, SourceFloat.Pitch, out float value);
            return value;
        }
        private float GetMinGain()
        {
            Api.GetSourceProperty(Handle, SourceFloat.MinGain, out float value);
            return value;
        }
        private float GetMaxGain()
        {
            Api.GetSourceProperty(Handle, SourceFloat.MaxGain, out float value);
            return value;
        }
        private float GetGain()
        {
            Api.GetSourceProperty(Handle, SourceFloat.Gain, out float value);
            return value;
        }
        private float GetConeInnerAngle()
        {
            Api.GetSourceProperty(Handle, SourceFloat.ConeInnerAngle, out float value);
            return value;
        }
        private float GetConeOuterAngle()
        {
            Api.GetSourceProperty(Handle, SourceFloat.ConeOuterAngle, out float value);
            return value;
        }
        private float GetConeOuterGain()
        {
            Api.GetSourceProperty(Handle, SourceFloat.ConeOuterGain, out float value);
            return value;
        }
        private float GetSecOffset()
        {
            Api.GetSourceProperty(Handle, SourceFloat.SecOffset, out float value);
            return value;
        }

        private void SetReferenceDistance(float distance)
            => Api.SetSourceProperty(Handle, SourceFloat.ReferenceDistance, distance);
        private void SetMaxDistance(float distance)
            => Api.SetSourceProperty(Handle, SourceFloat.MaxDistance, distance);
        private void SetRolloffFactor(float factor)
            => Api.SetSourceProperty(Handle, SourceFloat.RolloffFactor, factor);
        private void SetPitch(float pitch)
            => Api.SetSourceProperty(Handle, SourceFloat.Pitch, pitch);
        private void SetMinGain(float gain)
            => Api.SetSourceProperty(Handle, SourceFloat.MinGain, gain);
        private void SetMaxGain(float gain)
            => Api.SetSourceProperty(Handle, SourceFloat.MaxGain, gain);
        private void SetGain(float gain)
            => Api.SetSourceProperty(Handle, SourceFloat.Gain, gain);
        private void SetConeInnerAngle(float angle)
            => Api.SetSourceProperty(Handle, SourceFloat.ConeInnerAngle, angle);
        private void SetConeOuterAngle(float angle)
            => Api.SetSourceProperty(Handle, SourceFloat.ConeOuterAngle, angle);
        private void SetConeOuterGain(float gain)
            => Api.SetSourceProperty(Handle, SourceFloat.ConeOuterGain, gain);
        private void SetSecOffset(float offset)
            => Api.SetSourceProperty(Handle, SourceFloat.SecOffset, offset);
        #endregion

        void IPoolable.OnPoolableReset()
        {

        }

        void IPoolable.OnPoolableReleased()
        {

        }

        void IPoolable.OnPoolableDestroyed()
        {
            Dispose();
        }
    }
}