namespace System
{
    public class HashedStack<T> : Stack<T>
    {
        HashSet<T> _hash;
        
        public HashedStack() : base()
        {
            _hash = new HashSet<T>();
        }
        public HashedStack(int capacity) : base(capacity)
        {
            _hash = new HashSet<T>();
        }
        public HashedStack(IEnumerable<T> collection)
        {
            _hash = new HashSet<T>();
            foreach (T value in collection)
                Push(value);
        }
        public new void Clear()
        {
            base.Clear();
            _hash.Clear();
        }
        public new T Pop()
        {
            T value = base.Pop();
            _hash.Remove(value);
            return value;
        }
        public new bool Push(T item)
        {
            if (!_hash.Contains(item))
            {
                base.Push(item);
                _hash.Add(item);
                return true;
            }
            return false;
        }
        public void PushRange(IEnumerable<T> values)
        {
            foreach (T value in values)
                Push(value);
        }
    }
}
