using System.Numerics;
using XREngine.Audio;
using static XREngine.Audio.AudioSource;

namespace XREngine.Components.Scene
{
    public class AudioSourceComponent : XRComponent
    {
        private Vector3 _direction = Vector3.Zero;
        private ESourceState _state = ESourceState.Initial;
        private ESourceType type = ESourceType.Static;

        //TODO: create objects for only relevant listeners that this object is audible to
        //Dynamically turn off and on audio sources based on distance to listeners
        public List<AudioSource> SourcePerListener { get; private set; } = [];

        public Vector3 Direction
        {
            get => _direction;
            set => SetField(ref _direction, value);
        }

        public ESourceState State
        {
            get => _state;
            private set => SetField(ref _state, value);
        }

        public ESourceType Type
        {
            get => type;
            set => SetField(ref type, value);
        }

        public void PlayAudio()
        {
            if (State == ESourceState.Playing)
                return;

            State = ESourceState.Playing;
        }
        public void StopAudio()
        {
            if (State != ESourceState.Playing)
                return;

            State = ESourceState.Stopped;
        }
        public void PauseAudio()
        {
            if (State != ESourceState.Playing)
                return;

            State = ESourceState.Paused;
        }
        public void RewindAudio()
        {
            if (State == ESourceState.Initial)
                return;

            State = ESourceState.Initial;
        }

        public void SetStaticBuffer(AudioBuffer buffer)
        {
            if (type != ESourceType.Static)
                throw new InvalidOperationException("Cannot set static buffer on a streaming source.");
            foreach (var source in SourcePerListener)
                source.Buffer = buffer;
        }
        public void QueueStreamingBuffers(params AudioBuffer[] buffers)
        {
            if (type != ESourceType.Streaming)
                throw new InvalidOperationException("Cannot queue streaming buffers on a static source.");
            foreach (var source in SourcePerListener)
                source.QueueBuffers(buffers);
        }
        public void UnqueueStreamingBuffers(params AudioBuffer[] buffers)
        {
            if (type != ESourceType.Streaming)
                throw new InvalidOperationException("Cannot unqueue streaming buffers on a static source.");
            foreach (var source in SourcePerListener)
                source.UnqueueBuffers(buffers);
        }

        public void MakeSourceForListener(ListenerContext listener)
            => SourcePerListener.Add(listener.TakeSource());

        protected internal override void Start()
        {
            base.Start();
            if (World is not null)
            {
                SourcePerListener = World.Listeners.Select(l => l.TakeSource()).ToList() ?? [];
                World.Listeners.PostAnythingAdded += MakeSourceForListener;
            }
            RegisterTick(ETickGroup.PostPhysics, ETickOrder.Scene, UpdatePosition);
        }

        protected internal override void Stop()
        {
            base.Stop();
            foreach (var source in SourcePerListener)
                source.Dispose();
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Direction):
                    foreach (var source in SourcePerListener)
                        source.Direction = Direction;
                    break;
                case nameof(State):
                    switch (State)
                    {
                        case ESourceState.Playing:
                            foreach (var source in SourcePerListener)
                                source.Play();
                            break;
                        case ESourceState.Stopped:
                            foreach (var source in SourcePerListener)
                                source.Stop();
                            break;
                        case ESourceState.Paused:
                            foreach (var source in SourcePerListener)
                                source.Pause();
                            break;
                        case ESourceState.Initial:
                            foreach (var source in SourcePerListener)
                                source.Rewind();
                            break;
                    }
                    break;
            }
        }

        private void UpdatePosition()
        {
            if (World is not null)
            {
                foreach (var listener in World.Listeners)
                {
                    //TODO: check if listener is within range, add and remove sources as needed
                }
            }

            float delta = Engine.Time.Timer.FixedUpdateDelta;
            Vector3 worldPosition = Transform.WorldTranslation;
            foreach (var source in SourcePerListener)
            {
                Vector3 lastPosition = source.Position;
                source.Position = worldPosition;
                source.Velocity = (worldPosition - lastPosition) / delta;
            }
        }
    }
}
