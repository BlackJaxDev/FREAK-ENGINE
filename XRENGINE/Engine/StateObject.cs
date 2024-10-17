using XREngine.Core;

namespace XREngine
{
    public class StateObject(Action? onStateEnded) : IDisposable, IPoolable
    {
        public Action? OnStateEnded { get; set; } = onStateEnded;

        public void OnPoolableDestroyed()
        {
            OnStateEnded = null;
        }

        public void OnPoolableReleased()
        {
            OnStateEnded = null;
        }

        public void OnPoolableReset()
        {
            OnStateEnded = null;
        }

        void IDisposable.Dispose()
        {
            OnStateEnded?.Invoke();
            GC.SuppressFinalize(this);
        }
    }
}