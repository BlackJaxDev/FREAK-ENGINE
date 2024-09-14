using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace XREngine
{
    public interface IEventDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        ICollection<KeyValuePair<TKey, TValue>>, 
        IEnumerable<KeyValuePair<TKey, TValue>>,
        IEnumerable, IDictionary, ICollection, 
        IReadOnlyDictionary<TKey, TValue>, 
        IReadOnlyCollection<KeyValuePair<TKey, TValue>>, 
        ISerializable,
        IReadOnlyEventDictionary<TKey, TValue>,
        IDeserializationCallback where TKey : notnull
    {
        //TValue this[TKey key] { get; set; }
        //void Add(TKey key, TValue value);
        //void Clear();
        //bool Remove(TKey key);
    }
    public interface IReadOnlyEventDictionary<TKey, TValue> :
        IReadOnlyDictionary<TKey, TValue>,
        IEnumerable<KeyValuePair<TKey, TValue>>,
        IEnumerable, IReadOnlyCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
    {
        event EventDictionary<TKey, TValue>.DelAdded? Added;
        event EventDictionary<TKey, TValue>.DelCleared? Cleared;
        event EventDictionary<TKey, TValue>.DelRemoved? Removed;
        event EventDictionary<TKey, TValue>.DelSet? Set;
        event Action? Changed;
    }
    public class EventDictionary<TKey, TValue> : IEventDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _dic = [];

        public delegate void DelAdded(TKey key, TValue value);
        public delegate void DelCleared();
        public delegate void DelRemoved(TKey key, TValue value);
        public delegate void DelSet(TKey key, TValue oldValue, TValue newValue);

        public event DelAdded? Added;
        public event DelCleared? Cleared;
        public event DelRemoved? Removed;
        public event DelSet? Set;
        public event Action? Changed;

        public EventDictionary() 
            => _dic = [];
        public EventDictionary(int capacity)
            => _dic = new Dictionary<TKey, TValue>(capacity);
        public EventDictionary(IEqualityComparer<TKey> comparer) 
            => _dic = new Dictionary<TKey, TValue>(comparer);
        public EventDictionary(IDictionary<TKey, TValue> dictionary) 
            => _dic = new Dictionary<TKey, TValue>(dictionary);
        public EventDictionary(int capacity, IEqualityComparer<TKey> comparer)
            => _dic = new Dictionary<TKey, TValue>(capacity, comparer);
        public EventDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
            => _dic = new Dictionary<TKey, TValue>(dictionary, comparer);

        public Dictionary<TKey, TValue>.ValueCollection Values => _dic.Values;
        public Dictionary<TKey, TValue>.KeyCollection Keys => _dic.Keys;
        public int Count => _dic.Count;

        public TValue this[TKey key]
        {
            get => _dic[key];
            set
            {
                TValue old = _dic[key];
                _dic[key] = value;
                Set?.Invoke(key, old, value);
                Changed?.Invoke();
            }
        }
        public void Add(TKey key, TValue value)
        {
            _dic.Add(key, value);
            Added?.Invoke(key, value);
            Changed?.Invoke();
        }
        public void Clear()
        {
            _dic.Clear();
            Cleared?.Invoke();
            Changed?.Invoke();
        }
        public bool Remove(TKey key)
        {
            if (!_dic.TryGetValue(key, out TValue? old))
                return false;
            bool success = _dic.Remove(key);
            if (success)
            {
                Removed?.Invoke(key, old);
                Changed?.Invoke();
            }
            return success;
        }

        public bool ContainsKey(TKey key)
            => _dic.ContainsKey(key);
        public bool ContainsValue(TValue value)
            => _dic.ContainsValue(value);
        public bool TryGetValue(TKey key, out TValue value)
            => _dic.TryGetValue(key, out value);

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => _dic.Keys;
        ICollection<TValue> IDictionary<TKey, TValue>.Values => _dic.Values;
        int ICollection<KeyValuePair<TKey, TValue>>.Count => _dic.Count;
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_dic).IsReadOnly;
        ICollection IDictionary.Keys => _dic.Keys;
        ICollection IDictionary.Values => _dic.Values;
        bool IDictionary.IsReadOnly => ((IDictionary)_dic).IsReadOnly;
        bool IDictionary.IsFixedSize => ((IDictionary)_dic).IsFixedSize;
        int ICollection.Count => _dic.Count;
        object ICollection.SyncRoot => ((ICollection)_dic).SyncRoot;
        bool ICollection.IsSynchronized => ((ICollection)_dic).IsSynchronized;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _dic.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _dic.Values;
        int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count => _dic.Count;

        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => this[key];
        object IDictionary.this[object key]
        {
            get => this[(TKey)key];
            set => this[(TKey)key] = (TValue)value; 
        }
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => this[key];
            set => this[key] = value;
        }

        bool IDictionary<TKey, TValue>.ContainsKey(TKey key) => _dic.ContainsKey(key);
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => _dic.Add(key, value);
        bool IDictionary<TKey, TValue>.Remove(TKey key) => _dic.Remove(key);
        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dic.TryGetValue(key, out value);
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dic).Add(item);
        void ICollection<KeyValuePair<TKey, TValue>>.Clear() => _dic.Clear();
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary)_dic).Contains(item);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary)_dic).CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dic).Remove(item);
        bool IDictionary.Contains(object key) => ((IDictionary)_dic).Contains(key);
        void IDictionary.Add(object key, object? value) => ((IDictionary)_dic).Add(key, value);
        void IDictionary.Clear() => _dic.Clear();
        IDictionaryEnumerator IDictionary.GetEnumerator() => _dic.GetEnumerator();
        void IDictionary.Remove(object key) => ((IDictionary)_dic).Remove(key);
        void ICollection.CopyTo(Array array, int index) => ((ICollection)_dic).CopyTo(array, index);
        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => _dic.ContainsKey(key);
        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => _dic.TryGetValue(key, out value);
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => _dic.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dic.GetEnumerator();
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) => _dic.GetObjectData(info, context);
        void IDeserializationCallback.OnDeserialization(object? sender) => _dic.OnDeserialization(sender);
    }
}