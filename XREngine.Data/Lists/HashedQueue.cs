namespace System
{
    public class HashedQueue<T> : Queue<T>
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        HashSet<T> _hash;

        public HashedQueue() : base()
        {
            _hash = new HashSet<T>();
        }
        public HashedQueue(int capacity) : base(capacity)
        {
            _hash = new HashSet<T>();
        }
        public HashedQueue(IEnumerable<T> collection)
        {
            _hash = new HashSet<T>();
            foreach (T value in collection)
                Enqueue(value);
        }
        public new void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                base.Clear();
                _hash.Clear();
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }
        public new T Dequeue()
        {
            _lock.EnterWriteLock();
            try
            {
                T value = base.Dequeue();
                _hash.Remove(value);
                return value;
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
        }
        public new bool Enqueue(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_hash.Contains(item))
                {
                    base.Enqueue(item);
                    _hash.Add(item);
                    return true;
                }
            }
            finally
            {
                if (_lock.IsWriteLockHeld)
                    _lock.ExitWriteLock();
            }
            return false;
        }
        public void EnqueueRange(IEnumerable<T> values)
        {
            foreach (T value in values)
                Enqueue(value);
        }
    }
}
