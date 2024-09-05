using System.Collections;

namespace Extensions
{
    public class ThreadSafeEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _inner;
        private readonly ReaderWriterLockSlim _lock;

        public ThreadSafeEnumerable(IEnumerable<T> inner, ReaderWriterLockSlim rwlock)
        {
            _inner = inner;
            _lock = rwlock;
        }

        public IEnumerator<T> GetEnumerator()
            => new ThreadSafeEnumerator<T>(_inner.GetEnumerator(), _lock);

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
