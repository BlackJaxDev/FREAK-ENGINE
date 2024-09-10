using System.Collections;

namespace XREngine.Data.Core
{
    public struct XRBoolEvent<T>(InvokeBoolType type = InvokeBoolType.All) : IEnumerable<Func<T, bool>>
    {
        private List<Func<T, bool>>? _actions = [];
        private List<Func<T, bool>> Actions => _actions ??= [];

        public InvokeBoolType Type { get; } = type;

        public int Count => Actions.Count;

        public void AddListener(Func<T, bool> action)
            => Actions.Add(action);

        public void RemoveListener(Func<T, bool> action)
            => Actions.Remove(action);

        public bool Invoke(T item)
            => Actions.All(x => x.Invoke(item));

        public IEnumerator<Func<T, bool>> GetEnumerator()
            => ((IEnumerable<Func<T, bool>>)Actions).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)Actions).GetEnumerator();

        public static XRBoolEvent<T> operator +(XRBoolEvent<T> e, Func<T, bool> a)
        {
            e.AddListener(a);
            return e;
        }
        public static XRBoolEvent<T> operator -(XRBoolEvent<T> e, Func<T, bool> a)
        {
            e.RemoveListener(a);
            return e;
        }
    }
}
