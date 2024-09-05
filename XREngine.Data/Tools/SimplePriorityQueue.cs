namespace XREngine.Data.Tools
{
    public class SimplePriorityQueue<T>
    {
        private List<KeyValuePair<T, float>> heap;

        public SimplePriorityQueue()
        {
            heap = new List<KeyValuePair<T, float>>();
        }

        public void Enqueue(T item, float priority)
        {
            heap.Add(new KeyValuePair<T, float>(item, priority));
            int index = heap.Count - 1;
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;

                if (heap[parentIndex].Value <= heap[index].Value)
                    break;

                (heap[parentIndex], heap[index]) = (heap[index], heap[parentIndex]);
                index = parentIndex;
            }
        }

        public T Dequeue()
        {
            if (heap.Count == 0)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            T result = heap[0].Key;
            int lastIndex = heap.Count - 1;
            heap[0] = heap[lastIndex];
            heap.RemoveAt(lastIndex);

            int index = 0;
            while (true)
            {
                int leftChildIndex = 2 * index + 1;
                int rightChildIndex = 2 * index + 2;
                int minChildIndex;

                if (leftChildIndex >= heap.Count)
                    break;

                if (rightChildIndex >= heap.Count)
                    minChildIndex = leftChildIndex;
                else
                    minChildIndex = heap[leftChildIndex].Value < heap[rightChildIndex].Value
                        ? leftChildIndex
                        : rightChildIndex;

                if (heap[index].Value <= heap[minChildIndex].Value)
                    break;

                (heap[minChildIndex], heap[index]) = (heap[index], heap[minChildIndex]);
                index = minChildIndex;
            }

            return result;
        }

        private void UpHeap(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (heap[parentIndex].Value <= heap[index].Value) break;

                (heap[parentIndex], heap[index]) = (heap[index], heap[parentIndex]);
                index = parentIndex;
            }
        }

        private void DownHeap(int index)
        {
            while (true)
            {
                int leftChildIndex = 2 * index + 1;
                int rightChildIndex = 2 * index + 2;
                int minChildIndex;

                if (leftChildIndex >= heap.Count) break;
                if (rightChildIndex >= heap.Count) minChildIndex = leftChildIndex;
                else minChildIndex = heap[leftChildIndex].Value < heap[rightChildIndex].Value ? leftChildIndex : rightChildIndex;

                if (heap[index].Value <= heap[minChildIndex].Value) break;

                KeyValuePair<T, float> temp = heap[index];
                heap[index] = heap[minChildIndex];
                heap[minChildIndex] = temp;
                index = minChildIndex;
            }
        }

        public void Remove(T item)
        {
            int index = heap.FindIndex(pair => pair.Key.Equals(item));
            if (index == -1) return;

            int lastIndex = heap.Count - 1;
            heap[index] = heap[lastIndex];
            heap.RemoveAt(lastIndex);

            if (index < lastIndex)
            {
                UpHeap(index);
                DownHeap(index);
            }
        }

        public void UpdatePriority(T item, float newPriority)
        {
            int index = heap.FindIndex(pair => pair.Key.Equals(item));
            if (index == -1) return;

            float oldPriority = heap[index].Value;
            heap[index] = new KeyValuePair<T, float>(item, newPriority);

            if (newPriority < oldPriority)
            {
                UpHeap(index);
            }
            else
            {
                DownHeap(index);
            }
        }

        public int Count()
        {
            return heap.Count;
        }
    }
}
