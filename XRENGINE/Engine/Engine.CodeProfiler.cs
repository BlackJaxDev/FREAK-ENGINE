using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using XREngine.Core;

namespace XREngine
{
    public static partial class Engine
    {
        public class CodeProfiler
        {
            public CodeProfiler()
            {
                Time.Timer.SwapBuffers += ClearFrameLog;
            }
            ~CodeProfiler()
            {
                Time.Timer.SwapBuffers -= ClearFrameLog;
            }

            public class CodeProfilerTimer(string? name = null) : IPoolable
            {
                public float StartTime { get; private set; } = new();
                public float EndTime { get; private set; } = new();
                public float ElapsedSec => MathF.Round(EndTime - StartTime, 2);
                public float ElapsedMs => MathF.Round((EndTime - StartTime) * 1000.0f, 2);
                public string? Name { get; set; } = name;

                public void OnPoolableDestroyed() { }
                public void OnPoolableReleased()
                    => EndTime = Time.Timer.Time();
                public void OnPoolableReset()
                    => StartTime = Time.Timer.Time();
            }

            public bool EnableFrameLogging { get; set; } = true;
            public float DebugOutputMinElapsedMs { get; set; } = 0.0f;
            public ConcurrentQueue<(int threadId, string methodName, float elapsedMs)> FrameLog { get; } = [];

            public void ClearFrameLog()
                => FrameLog.Clear();
            private void LogForFrame(string? name, float elapsedMs)
                => FrameLog.Enqueue((Environment.CurrentManagedThreadId, name ?? string.Empty, elapsedMs));

            private readonly ResourcePool<StateObject> _statePool = new(() => new StateObject(null));
            private readonly ResourcePool<CodeProfilerTimer> _timerPool = new(() => new CodeProfilerTimer());
            private readonly ConcurrentDictionary<Guid, CodeProfilerTimer> _asyncTimers = [];

            public delegate void DelTimerCallback(string? methodName, float elapsedMs);
            /// <summary>
            /// Starts a timer and returns a StateObject that will stop the timer when it is disposed.
            /// </summary>
            /// <param name="callback"></param>
            /// <param name="methodName"></param>
            /// <returns></returns>
            public StateObject Start(DelTimerCallback? callback = null, bool printDebugIfNoCallback = true, [CallerMemberName] string? methodName = null)
            {
                var state = _statePool.Take();
                if (!EnableFrameLogging)
                    return state;

                var entry = _timerPool.Take();
                entry.Name = methodName;
                void onStateEnded() => Stop(state, entry, callback, printDebugIfNoCallback);
                state.OnStateEnded = onStateEnded;
                return state;
            }
            /// <summary>
            /// Stops a timer and calls the callback if available.
            /// </summary>
            /// <param name="entry"></param>
            /// <param name="callback"></param>
            private void Stop(StateObject state, CodeProfilerTimer entry, DelTimerCallback? callback, bool printDebugIfNoCallback = true)
            {
                _statePool.Release(state);
                _timerPool.Release(entry);
                if (callback is null)
                {
                    if (printDebugIfNoCallback)
                    {
                        float elapsedMs = entry.ElapsedMs;
                        if (elapsedMs >= DebugOutputMinElapsedMs)
                            Debug.Out($"{entry.Name} took {entry.ElapsedMs}ms");
                    }
                }
                else
                    callback.Invoke(entry.Name, entry.ElapsedMs);
                LogForFrame(entry.Name, entry.ElapsedMs);
            }
            /// <summary>
            /// Starts an async timer and returns the id of the timer.
            /// </summary>
            /// <param name="callback"></param>
            /// <param name="methodName"></param>
            /// <returns></returns>
            public Guid StartAsync(DelTimerCallback? callback = null, [CallerMemberName] string? methodName = null)
            {
                Guid id = Guid.NewGuid();
                if (!EnableFrameLogging)
                    return id;

                var entry = _timerPool.Take();
                entry.Name = methodName ?? string.Empty;
                _asyncTimers.TryAdd(id, entry);
                return id;
            }
            /// <summary>
            /// Stops an async timer by id and returns the elapsed time in milliseconds.
            /// </summary>
            /// <param name="id"></param>
            /// <param name="methodName"></param>
            /// <returns></returns>
            public float StopAsync(Guid id, out string? methodName, bool printDebug = true)
            {
                if (!EnableFrameLogging)
                {
                    methodName = string.Empty;
                    return 0.0f;
                }

                if (_asyncTimers.TryRemove(id, out var entry))
                {
                    LogForFrame(entry.Name, entry.ElapsedMs);
                    _timerPool.Release(entry);
                }
                methodName = entry?.Name;
                if (printDebug)
                {
                    float elapsedMs = entry?.ElapsedMs ?? 0.0f;
                    if (elapsedMs >= DebugOutputMinElapsedMs)
                        Debug.Out($"{methodName} took {elapsedMs}ms");
                }
                return entry?.ElapsedMs ?? 0.0f;
            }
            /// <summary>
            /// Stops an async timer by id and returns the elapsed time in milliseconds.
            /// </summary>
            /// <param name="id"></param>
            /// <param name="methodName"></param>
            /// <returns></returns>
            public float StopAsync(Guid id, bool printDebug = true)
            {
                if (!EnableFrameLogging)
                    return 0.0f;

                if (_asyncTimers.TryRemove(id, out var entry))
                {
                    LogForFrame(entry.Name, entry.ElapsedMs);
                    _timerPool.Release(entry);
                }
                string methodName = entry?.Name ?? string.Empty;
                if (printDebug)
                {
                    float elapsedMs = entry?.ElapsedMs ?? 0.0f;
                    if (elapsedMs >= DebugOutputMinElapsedMs)
                        Debug.Out($"{methodName} took {elapsedMs}ms");
                }
                return entry?.ElapsedMs ?? 0.0f;
            }
        }
    }
}
