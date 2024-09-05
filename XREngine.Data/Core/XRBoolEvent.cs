using System.Collections;

namespace XREngine.Data.Core
{
    public readonly struct XRBoolEvent<T>(InvokeBoolType type = InvokeBoolType.All) : IEnumerable<Func<T, bool>>
    {
        private readonly List<Func<T, bool>> _actions = [];
        public InvokeBoolType Type { get; } = type;

        public int Count => _actions.Count;

        public void AddListener(Func<T, bool> action)
            => _actions.Add(action);

        public void RemoveListener(Func<T, bool> action)
            => _actions.Remove(action);

        public bool Invoke(T item)
            => _actions.All(x => x.Invoke(item));

        public IEnumerator<Func<T, bool>> GetEnumerator()
            => ((IEnumerable<Func<T, bool>>)_actions).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)_actions).GetEnumerator();

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
