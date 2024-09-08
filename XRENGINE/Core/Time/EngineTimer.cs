﻿using Extensions;
using System.Diagnostics;
using XREngine.Data.Core;
using XREngine.Native;

namespace XREngine.Timers
{
    public partial class EngineTimer : XRBase
    {
        /// <summary>
        /// This is the delta used for physics and other fixed-timestep calculations.
        /// Fixed-timestep is consistent and does not vary based on rendering speed.
        /// </summary>
        public float FixedUpdateDelta { get; set; } = 0.033f;
        /// <summary>
        /// This is the desired FPS for physics and other fixed-timestep calculations.
        /// It does not vary.
        /// </summary>
        public float FixedUpdateHz
        {
            get => 1.0f / FixedUpdateDelta.ClampMin(0.0001f);
            set => FixedUpdateDelta = 1.0f / value.ClampMin(0.0001f);
        }

        private const float MaxFrequency = 500.0f; // Frequency cap for Update/RenderFrame events

        //Events to subscribe to
        /// <summary>
        /// Subscribe to this event for game logic updates.
        /// </summary>
        public event Action? UpdateFrame;
        /// <summary>
        /// Subscribe to this event to execute logic on the render thread right before buffers are swapped.
        /// </summary>
        public event Action? PreRenderFrame;
        /// <summary>
        /// Subscribe to this event to execute render commands that have been swapped for consumption.
        /// </summary>
        public event Action? RenderFrame;
        /// <summary>
        /// Subscribe to this event to swap update and render buffers, on the render thread.
        /// </summary>
        public event Action? SwapBuffers;
        /// <summary>
        /// Subscribe to this event to execute logic at a fixed rate completely separate from the update/render threads, such as physics.
        /// </summary>
        public event Action? FixedUpdate;

        private float _lastPreRenderTimestamp;
        private float _lastFixedUpdateTimestamp;

        private float _updateTimeDiff = 0.0f; // quantization error for UpdateFrame events
        private bool _isRunningSlowly; // true, when UpdatePeriod cannot reach TargetUpdatePeriod

        private readonly Stopwatch _watch = new();

        private ManualResetEventSlim 
            _swapDone = new(false),
            _preRenderDone = new(true),
            _updatingDone = new(false);

        public bool IsRunning => _watch.IsRunning;

        public DeltaManager Render { get; } = new();
        public DeltaManager Update { get; } = new();

        private CancellationTokenSource? _cancelRenderTokenSource = null;

        private Task? UpdateTask = null;
        private Task? PreRenderTask = null;
        private Task? RenderTask = null;
        private Task? SingleTask = null;
        private Task? FixedUpdateTask = null;

        //private static bool IsApplicationIdle() => NativeMethods.PeekMessage(out _, IntPtr.Zero, 0, 0, 0) == 0;

        //private static async Task IdleCallAsync(Action method, CancellationToken cancellationToken)
        //{
        //    while (!cancellationToken.IsCancellationRequested)
        //    {
        //        //if (IsApplicationIdle())
        //        method();
        //        await Task.Yield();
        //    }
        //}

        /// <summary>
        /// Runs the timer until Stop() is called.
        /// </summary>
        public void Run(Func<bool> runUntilPredicate)
        {
            if (IsRunning)
                return;

            _swapDone = new ManualResetEventSlim(false);
            _preRenderDone = new ManualResetEventSlim(true);
            _updatingDone = new ManualResetEventSlim(false);

            UpdateTask = Task.Run(UpdateThread);
            PreRenderTask = Task.Run(PreRenderThread);
            FixedUpdateTask = Task.Run(FixedUpdateThread);

            _watch.Start();
            Debug.Out($"Started game loop threads.");

            while (runUntilPredicate())
                RenderThread();
        }

        /// <summary>
        /// Update is always running game logic as fast as requested.
        /// No fences here.
        /// </summary>
        private void UpdateThread()
        {
            while (IsRunning)
                DispatchUpdate();
        }
        /// <summary>
        /// This thread waits for the render thread to finish swapping the last frame's prerender buffers, 
        /// then dispatches a prerender to collect the next frame's batch of render commands while this frame renders.
        /// </summary>
        private void PreRenderThread()
        {
            while (IsRunning)
            {
                //Wait for last frame's swap to finish
                _swapDone.Wait();
                _swapDone.Reset();

                //Dispatch prerender, which collects visible objects and generates render commands for the game's current state.
                DispatchPreRender();
                _preRenderDone.Set();
            }
        }
        /// <summary>
        /// This thread runs at a fixed rate, executing logic that should not be tied to the update/render threads.
        /// Typical events occuring here are logic like physics calculations.
        /// </summary>
        private void FixedUpdateThread()
        {
            while (IsRunning)
            {
                float timestamp = Time();
                float elapsed = (timestamp - _lastFixedUpdateTimestamp).Clamp(0.0f, 1.0f);
                if (elapsed >= FixedUpdateDelta)
                {
                    FixedUpdate?.Invoke();
                    _lastFixedUpdateTimestamp = timestamp;
                }
            }
        }
        /// <summary>
        /// Waits for the prerender to finish, then swaps buffers and dispatches a render.
        /// </summary>
        public void RenderThread()
        {
            //Wait for prerender to finish
            _preRenderDone.Wait();
            _preRenderDone.Reset();

            //Swap update/render buffers
            DispatchSwapBuffers();
            _swapDone.Set();

            //Suspend this thread until a render is dispatched
            while (!DispatchRender()) ;
        }

        public void Stop()
        {
            _watch.Stop();

            _preRenderDone?.Set();
            _swapDone?.Set();
            //_updatingDone?.Set();

            UpdateTask?.Wait();
            UpdateTask = null;

            PreRenderTask?.Wait();
            PreRenderTask = null;

            RenderTask?.Wait();
            RenderTask = null;

            SingleTask?.Wait();
            SingleTask = null;

            FixedUpdateTask?.Wait();
            FixedUpdateTask = null;
        }

        /// <summary>
        /// Retrives the current timestamp from the stopwatch.
        /// </summary>
        /// <returns></returns>
        public float Time() => (float)_watch.Elapsed.TotalSeconds;

        private bool DispatchRender()
        {
            float timestamp = Time();
            float elapsed = (timestamp - Render.LastTimestamp).Clamp(0.0f, 1.0f);
            bool dispatch = elapsed > 0.0f && elapsed >= TargetRenderPeriod;
            if (dispatch)
            {
                Render.Delta = elapsed;
                Render.LastTimestamp = timestamp;
                RenderFrame?.Invoke();

                timestamp = Time();
                Render.ElapsedTime = timestamp - Render.LastTimestamp;
            }
            return dispatch;
        }
        private void DispatchPreRender()
        {
            float timestamp = Time();
            float elapsed = (timestamp - _lastPreRenderTimestamp).Clamp(0.0f, 1.0f);
            _lastPreRenderTimestamp = timestamp;
            PreRenderFrame?.Invoke();
        }
        private void DispatchSwapBuffers() => SwapBuffers?.Invoke();
        private void DispatchFixedUpdate() => FixedUpdate?.Invoke();
        private void DispatchUpdate()
        {
            int runningSlowlyRetries = 4;

            float timestamp = Time();
            float elapsed = (timestamp - Update.LastTimestamp).Clamp(0.0f, 1.0f);

            //Raise UpdateFrame events until we catch up with the target update period
            while (IsRunning && elapsed > 0.0f && elapsed + _updateTimeDiff >= TargetUpdatePeriod)
            {
                Update.Delta = elapsed;
                Update.LastTimestamp = timestamp;
                UpdateFrame?.Invoke();

                timestamp = Time();
                Update.ElapsedTime = timestamp - Update.LastTimestamp;

                // Calculate difference (positive or negative) between
                // actual elapsed time and target elapsed time. We must
                // compensate for this difference.
                _updateTimeDiff += elapsed - TargetUpdatePeriod;

                if (TargetUpdatePeriod <= double.Epsilon)
                {
                    // According to the TargetUpdatePeriod documentation,
                    // a TargetUpdatePeriod of zero means we will raise
                    // UpdateFrame events as fast as possible (one event
                    // per ProcessEvents() call)
                    break;
                }

                _isRunningSlowly = _updateTimeDiff >= TargetUpdatePeriod;
                if (_isRunningSlowly && --runningSlowlyRetries == 0)
                {
                    // If UpdateFrame consistently takes longer than TargetUpdateFrame
                    // stop raising events to avoid hanging inside the UpdateFrame loop.
                    break;
                }

                // Prepare for next loop
                elapsed = (timestamp - Update.LastTimestamp).Clamp(0.0f, 1.0f);
            }
        }

        private float _targetRenderPeriod;
        /// <summary>
        /// Gets or sets a float representing the target render frequency, in hertz.
        /// </summary>
        /// <remarks>
        /// <para>A value of 0.0 indicates that RenderFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
        /// <para>Values lower than 1.0Hz are clamped to 0.0. Values higher than 500.0Hz are clamped to 200.0Hz.</para>
        /// </remarks>
        public float TargetRenderFrequency
        {
            get => _targetRenderPeriod == 0.0f ? 0.0f : 1.0f / _targetRenderPeriod;
            set
            {
                if (value < 1.0f)
                {
                    _targetRenderPeriod = 0.0f;
                    Debug.Out("Target render frequency set to unrestricted.");
                }
                else if (value < MaxFrequency)
                {
                    _targetRenderPeriod = 1.0f / value;
                    Debug.Out("Target render frequency set to {0}Hz.", value.ToString());
                }
                else
                {
                    _targetRenderPeriod = 1.0f / MaxFrequency;
                    Debug.Out("Target render frequency clamped to {0}Hz.", MaxFrequency.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets a float representing the target render period, in seconds.
        /// </summary>
        /// <remarks>
        /// <para>A value of 0.0 indicates that RenderFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
        /// <para>Values lower than 0.002 seconds (500Hz) are clamped to 0.0. Values higher than 1.0 seconds (1Hz) are clamped to 1.0.</para>
        /// </remarks>
        public float TargetRenderPeriod
        {
            get => _targetRenderPeriod;
            set
            {
                if (value < 1.0f / MaxFrequency)
                {
                    _targetRenderPeriod = 0.0f;
                    Debug.Out("Target render frequency set to unrestricted.");
                }
                else if (value < 1.0f)
                {
                    _targetRenderPeriod = value;
                    Debug.Out("Target render frequency set to {0}Hz.", TargetRenderFrequency.ToString());
                }
                else
                {
                    _targetRenderPeriod = 1.0f;
                    Debug.Out("Target render frequency clamped to 1Hz.");
                }
            }
        }

        private float _targetUpdatePeriod;
        /// <summary>
        /// Gets or sets a float representing the target update frequency, in hertz.
        /// </summary>
        /// <remarks>
        /// <para>A value of 0.0 indicates that UpdateFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
        /// <para>Values lower than 1.0Hz are clamped to 0.0. Values higher than 500.0Hz are clamped to 500.0Hz.</para>
        /// </remarks>
        public float TargetUpdateFrequency
        {
            get => _targetUpdatePeriod == 0.0f ? 0.0f : 1.0f / _targetUpdatePeriod;
            set
            {
                if (value < 1.0)
                {
                    _targetUpdatePeriod = 0.0f;
                    Debug.Out("Target update frequency set to unrestricted.");
                }
                else if (value < MaxFrequency)
                {
                    _targetUpdatePeriod = 1.0f / value;
                    Debug.Out("Target update frequency set to {0}Hz.", value);
                }
                else
                {
                    _targetUpdatePeriod = 1.0f / MaxFrequency;
                    Debug.Out("Target update frequency clamped to {0}Hz.", MaxFrequency);
                }
            }
        }

        /// <summary>
        /// Gets or sets a float representing the target update period, in seconds.
        /// </summary>
        /// <remarks>
        /// <para>A value of 0.0 indicates that UpdateFrame events are generated at the maximum possible frequency (i.e. only limited by the hardware's capabilities).</para>
        /// <para>Values lower than 0.002 seconds (500Hz) are clamped to 0.0. Values higher than 1.0 seconds (1Hz) are clamped to 1.0.</para>
        /// </remarks>
        public float TargetUpdatePeriod
        {
            get => _targetUpdatePeriod;
            set
            {
                if (value < 1.0f / MaxFrequency)
                {
                    _targetUpdatePeriod = 0.0f;
                    Debug.Out("Target update frequency set to unrestricted.");
                }
                else if (value < 1.0)
                {
                    _targetUpdatePeriod = value;
                    Debug.Out("Target update frequency set to {0}Hz.", TargetUpdateFrequency);
                }
                else
                {
                    _targetUpdatePeriod = 1.0f;
                    Debug.Out("Target update frequency clamped to 1Hz.");
                }
            }
        }
    }
}