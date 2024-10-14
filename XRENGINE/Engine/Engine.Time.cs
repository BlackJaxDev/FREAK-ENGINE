using XREngine.Timers;

namespace XREngine
{
    public static partial class Engine
    {
        public static float Delta => Time.Timer.Update.Delta;
        public static float DilatedDelta => Time.Timer.Update.DilatedDelta;
        public static float SmoothedDelta => Time.Timer.Update.SmoothedDelta;
        public static float SmoothedDilatedDelta => Time.Timer.Update.SmoothedDilatedDelta;
        public static float FixedDelta => Time.Timer.FixedUpdateDelta;
        public static float ElapsedTime => Time.Timer.Time();

        public static class Time
        {
            public static EngineTimer Timer { get; } = new EngineTimer();

            static Time() => Timer = new EngineTimer();

            private static readonly List<DateTime> _debugTimers = [];
            /// <summary>
            /// Starts a quick timer to track the number of sceonds elapsed.
            /// Returns the id of the timer.
            /// </summary>
            public static int StartDebugTimer()
            {
                int id = _debugTimers.Count;
                _debugTimers.Add(DateTime.Now);
                return id;
            }
            /// <summary>
            /// Ends the timer and returns the amount of time elapsed, in seconds.
            /// </summary>
            /// <param name="id">The id of the timer.</param>
            public static float EndDebugTimer(int id)
            {
                float seconds = (float)(DateTime.Now - _debugTimers[id]).TotalSeconds;
                _debugTimers.RemoveAt(id);
                return seconds;
            }

            /// <summary>
            /// Given the game's and user's settings, updates the core game engine timer settings.
            /// </summary>
            /// <param name="gameSet"></param>
            /// <param name="userSet"></param>
            public static void Initialize(GameStartupSettings gameSet, UserSettings userSet)
                => UpdateTimer(userSet?.TargetFramesPerSecond ?? 0.0f, userSet?.TargetUpdatesPerSecond ?? 0.0f);

            /// <summary>
            /// Updates the core game engine timer settings.
            /// </summary>
            /// <param name="singleThreaded"></param>
            /// <param name="targetRenderFrequency"></param>
            /// <param name="targetUpdateFrequency"></param>
            public static void UpdateTimer(
                float targetRenderFrequency,
                float targetUpdateFrequency)
            {
                Timer.TargetRenderFrequency = targetRenderFrequency;
                Timer.TargetUpdateFrequency = targetUpdateFrequency;
            }
        }
    }
}
