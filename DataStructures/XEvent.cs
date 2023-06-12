namespace XREngine.Data
{
    public class XEvent<T>
    {
        private List<Action<T>> _actions = new();

        public void AddListener(Action<T> action)
        {
            _actions.Add(action);
        }
        public void RemoveListener(Action<T> action)
        {
            _actions.Remove(action);
        }
        public void Invoke(T item)
        {
            _actions.ForEach(x => x.Invoke(item));
        }
    }
}
