using System.Collections;

namespace Extensions
{
    public class ThreadSafeEnumerator<T> : IEnumerator<T>, IDisposable
    {
        private readonly IEnumerator<T> _inner;
        private ReaderWriterLockSlim _lock;

        public ThreadSafeEnumerator(IEnumerator<T> inner, ReaderWriterLockSlim rwlock)
        {
            _inner = inner;
            _lock = rwlock;
            _lock.EnterReadLock();
        }
        public void Dispose()
        {
            _lock.ExitReadLock();
        }

        public bool MoveNext() => _inner.MoveNext();
        public void Reset() => _inner.Reset();
        public T Current => _inner.Current;
        object IEnumerator.Current => Current!;
    }
}
