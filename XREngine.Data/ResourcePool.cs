using System.Collections.Concurrent;

namespace XREngine.Core
{
    public interface IPoolable
    {
        /// <summary>
        /// Reset this resource to as if it had just been constructed.
        /// </summary>
        void Reset();
    }
    public class ResourcePool<T> where T : IPoolable
    {
        private readonly ConcurrentBag<T> _objects = new ConcurrentBag<T>();
        private readonly Func<T> _generator;

        public ResourcePool(Func<T> generator) : this(0, generator) { }
        public ResourcePool(int initialCount, Func<T> generator)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            for (int i = 0; i < initialCount; ++i)
                _objects.Add(_generator());
        }
        public T Take()
        {
            if (_objects.TryTake(out T item))
            {
                item.Reset();
                return item;
            }
            return _generator();
        }
        public void Put(T item)
        {
            _objects.Add(item);
        }
    }
}
