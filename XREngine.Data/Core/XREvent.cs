using System.Collections;

namespace XREngine.Data.Core
{
    public struct XREvent<T> : IEnumerable<Action<T>>
    {
        private List<Action<T>>? _actions;
        private List<Action<T>> Actions => _actions ??= [];
        
        public int Count => Actions.Count;

        public void AddListener(Action<T> action)
            => Actions.Add(action);

        public void RemoveListener(Action<T> action)
            => Actions.Remove(action);

        public void Invoke(T item)
            => Actions.ForEach(x => x.Invoke(item));

        public IEnumerator<Action<T>> GetEnumerator()
            => ((IEnumerable<Action<T>>)Actions).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)Actions).GetEnumerator();

        public static XREvent<T> operator +(XREvent<T> e, Action<T> a)
        {
            e.AddListener(a);
            return e;
        }
        public static XREvent<T> operator -(XREvent<T> e, Action<T> a)
        {
            e.RemoveListener(a);
            return e;
        }
    }
}
