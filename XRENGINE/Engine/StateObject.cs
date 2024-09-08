namespace XREngine
{
    public class StateObject(Action onStateEnded) : IDisposable
    {
        void IDisposable.Dispose()
        {
            onStateEnded();
            GC.SuppressFinalize(this);
        }
    }
}