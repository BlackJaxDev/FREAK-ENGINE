using System.Numerics;
using XREngine.Audio;

namespace XREngine.Components.Scene
{
    public class AudioListenerComponent : XRComponent
    {
        public ListenerContext? Listener { get; private set; }

        protected internal override void Start()
        {
            base.Start();
            MakeListener();
            RegisterTick(ETickGroup.PostPhysics, ETickOrder.Scene, UpdatePosition);
        }

        protected internal override void Stop()
        {
            base.Stop();
            DestroyListener();
        }

        private void MakeListener()
        {
            Listener = Engine.Audio.NewListener();
            World?.Listeners?.Add(Listener);
        }

        private void DestroyListener()
        {
            if (Listener is not null)
                World?.Listeners?.Remove(Listener);
            Listener?.Dispose();
            Listener = null;
        }

        private void UpdatePosition()
        {
            if (Listener is null)
                return;

            float delta = Engine.Time.Timer.FixedUpdateDelta;
            Vector3 pos = Transform.WorldTranslation;
            Vector3 lastPosition = Listener.Position;
            Listener.Position = pos;
            Listener.Velocity = (pos - lastPosition) / delta;
        }
    }
}
