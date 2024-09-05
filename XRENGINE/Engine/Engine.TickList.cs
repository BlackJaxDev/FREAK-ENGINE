using Extensions;
using System.Collections.Concurrent;

namespace XREngine
{
    public static partial class Engine
    {
        public class TickList
        {
            public TickList(bool parallel)
            {
                _parallel = parallel;
                Tick = parallel ? ExecuteParallel : ExecuteSequential;
            }

            public bool Parallel
            {
                get => _parallel;
                set
                {
                    _parallel = value;
                    Tick = _parallel ? ExecuteParallel : ExecuteSequential;
                }
            }

            /// <summary>
            /// Ticks all items in this list.
            /// </summary>
            public Action Tick { get; private set; }

            public delegate void DelTick();

            private readonly List<DelTick> _methods = [];
            private readonly ConcurrentQueue<(bool Add, DelTick Func)> _queue = new();
            private bool _parallel = true;

            public int Count => _methods.Count;

            public void Add(DelTick tickMethod) 
                => _queue.Enqueue((true, tickMethod));
            public void Remove(DelTick tickMethod)
                => _queue.Enqueue((false, tickMethod));
            private void ExecuteParallel()
            {
                Dequeue();
                _methods.ForEachParallelIList(ExecTick);
            }
            private void ExecuteSequential()
            {
                Dequeue();
                _methods.ForEach(ExecTick);
            }
            private void ExecTick(DelTick func)
                => func();
            private void Dequeue()
            {
                //Add or remove the list of methods that tried to register to or unregister from this group while it was ticking.
                while (!_queue.IsEmpty && _queue.TryDequeue(out (bool Add, DelTick Func) result))
                {
                    if (result.Add)
                        _methods.Add(result.Func);
                    else
                        _methods.Remove(result.Func);
                }
            }
        }
    }
}
