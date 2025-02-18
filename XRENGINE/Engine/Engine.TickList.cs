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
                //float time = ElapsedTime;
                //Use tasks
                //Task[] tasks = new Task[_methods.Count];
                //for (int i = 0; i < _methods.Count; i++)
                //    tasks[i] = Task.Run(() => ExecTick(_methods[i]));
                //Task.WaitAll(tasks);
                _methods.ForEachParallelIList(ExecTick);
                //Debug.Out($"TickList Parallel: {Math.Round((ElapsedTime - time) * 1000.0f, 2)} ms");
            }
            private void ExecuteSequential()
            {
                Dequeue();
                //float time = ElapsedTime;
                _methods.ForEach(ExecTick);
                //Debug.Out($"TickList Sequential: {Math.Round((ElapsedTime - time) * 1000.0f, 2)} ms");
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
