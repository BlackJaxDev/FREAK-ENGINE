namespace XREngine.Data.Tools
{
    public class SimplePriorityQueue<T, TKey> where TKey : IComparable<TKey>
    {
        private SortedDictionary<TKey, List<T>> sortedDictionary;

        public SimplePriorityQueue()
        {
            sortedDictionary = new SortedDictionary<TKey, List<T>>();
        }

        public void Enqueue(T item, TKey priority)
        {
            if (!sortedDictionary.ContainsKey(priority))
            {
                sortedDictionary[priority] = new List<T>();
            }
            sortedDictionary[priority].Add(item);
        }

        public T Dequeue()
        {
            if (sortedDictionary.Count == 0)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            TKey minKey = default;
            foreach (var key in sortedDictionary.Keys)
            {
                minKey = key;
                break;
            }

            List<T> itemList = sortedDictionary[minKey];
            T item = itemList[0];
            itemList.RemoveAt(0);

            if (itemList.Count == 0)
            {
                sortedDictionary.Remove(minKey);
            }

            return item;
        }

        public int Count()
        {
            int count = 0;
            foreach (var itemList in sortedDictionary.Values)
            {
                count += itemList.Count;
            }
            return count;
        }
        public bool Contains(T item)
        {
            foreach (var itemList in sortedDictionary.Values)
            {
                if (itemList.Contains(item))
                {
                    return true;
                }
            }
            return false;
        }

        public void Remove(T item)
        {
            TKey? keyToRemove = default;
            bool found = false;
            foreach (var keyValuePair in sortedDictionary)
            {
                if (keyValuePair.Value.Remove(item))
                {
                    keyToRemove = keyValuePair.Key;
                    found = true;
                    break;
                }
            }

            if (keyToRemove != null && found && sortedDictionary[keyToRemove].Count == 0)
            {
                sortedDictionary.Remove(keyToRemove);
            }
        }

        public void UpdatePriority(T item, TKey newPriority)
        {
            Remove(item);
            Enqueue(item, newPriority);
        }
    }
}
