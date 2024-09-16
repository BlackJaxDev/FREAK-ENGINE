namespace XREngine.Audio
{
    public class AudioManager
    {
        private readonly EventList<ListenerContext> _listeners = [];
        public IEventListReadOnly<ListenerContext> Listeners => _listeners;

        private void OnContextDisposed(ListenerContext listener)
        {
            listener.Disposed -= OnContextDisposed;
            _listeners.Remove(listener);
        }
        public ListenerContext NewListener()
        {
            ListenerContext listener = new();
            listener.Disposed += OnContextDisposed;
            _listeners.Add(listener);
            return listener;
        }

        public AudioManager() { }
    }
}