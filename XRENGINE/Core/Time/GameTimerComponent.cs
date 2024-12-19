using XREngine.Components;
using XREngine.Data.Core;

namespace XREngine.Timers
{
    public delegate void MultiFireAction(TimeSpan totalElapsed, int fireNumber);
    /// <summary>
    /// A timer that can execute a method once or multiple times with a given interval of time between each execution.
    /// Uses the attached component for ticking.
    /// </summary>
    /// <param name="owningComponent"></param>
    public class GameTimer(XRComponent owningComponent) : XRBase
    {
        public bool IsRunning => _isRunning;
        public XRComponent OwningComponent => owningComponent;

        public TimeSpan TimeBetweenFires
        {
            get => _timeBetweenFires;
            set => SetField(ref _timeBetweenFires, value);
        }

        //Set on start
        private MultiFireAction? _multiMethod;
        private Action? _singleMethod;
        private TimeSpan _timeBetweenFires;
        private TimeSpan _currentTimeBetweenFires;
        private int _fireMax;

        //State
        private int _fireNumber;
        private bool _isRunning;
        private TimeSpan _totalElapsed;
        private TimeSpan _elapsedSinceLastFire;

        private void Reset()
        {
            _fireNumber = 0;
            _fireMax = -1;
            _totalElapsed = TimeSpan.Zero;
            _elapsedSinceLastFire = TimeSpan.Zero;
            _timeBetweenFires = TimeSpan.Zero;
            _currentTimeBetweenFires = TimeSpan.Zero;
            _multiMethod = null;
            _singleMethod = null;
        }

        public void Cancel()
        {
            if (!_isRunning || _tickAction is null)
                return;

            Reset();
            _isRunning = false;

            OwningComponent.UnregisterTick(_tickGroup, _tickOrder, _tickAction);
        }

        private Engine.TickList.DelTick? _tickAction;
        private ETickGroup _tickGroup;
        private int _tickOrder;

        /// <summary>
        /// Executes a method once after the given time period.
        /// </summary>
        /// <param name="method">The method to execute.</param>
        /// <param name="timeToWait">How much time should pass before executing the method.</param>
        public void StartSingleFire(
            Action method,
            TimeSpan timeToWait,
            ETickGroup tickGroup = ETickGroup.Normal,
            int tickOrder = (int)ETickOrder.Timers)
        {
            if (_isRunning)
                Cancel();
            else
                Reset();

            if (timeToWait <= TimeSpan.Zero)
                method();
            else
            {
                _isRunning = true;
                _singleMethod = method;
                _currentTimeBetweenFires = timeToWait;
                
                OwningComponent.RegisterTick(_tickGroup = tickGroup, _tickOrder = tickOrder, _tickAction = TickSingle);
            }
        }

        /// <summary>
        /// Executes a single method multiple times with a given interval of time between each execution.
        /// </summary>
        /// <param name="method">The method to execute per fire.</param>
        /// <param name="timeBetweenFires">How many seconds should pass before running the method again.</param>
        /// <param name="maxFires">The maximum number of times the method should execute before the timer stops completely. Pass a number less than 0 for infinite.</param>
        /// <param name="startTime">How many seconds should pass before running the method for the first time.</param>
        public void StartMultiFire(
            MultiFireAction method,
            TimeSpan timeBetweenFires,
            int maxFires = -1,
            TimeSpan? startTime = null,
            ETickGroup tickGroup = ETickGroup.Normal,
            int tickOrder = (int)ETickOrder.Timers)
        {
            if (_isRunning)
                Cancel();
            else
                Reset();

            if (maxFires == 0 || method is null)
                return;
            
            _multiMethod = method;
            _fireMax = maxFires;

            _timeBetweenFires = timeBetweenFires;
            _currentTimeBetweenFires = startTime ?? TimeSpan.Zero;
            _isRunning = true;

            OwningComponent.RegisterTick(_tickGroup = tickGroup, _tickOrder = tickOrder, _tickAction = TickMulti);
        }

        /// Executes a single method multiple times with a given interval of time between each execution.
        /// </summary>
        /// <param name="method">The method to execute per fire.</param>
        /// <param name="secondsBetweenFires">How many seconds should pass before running the method again.</param>
        /// <param name="maxFires">The maximum number of times the method should execute before the timer stops completely. Pass a number less than 0 for infinite.</param>
        /// <param name="startSeconds">How many seconds should pass before running the method for the first time.</param>
        public void StartMultiFire(
            Action method,
            TimeSpan timeBetweenFires,
            int maxFires = -1,
            TimeSpan? startTime = null,
            ETickGroup tickGroup = ETickGroup.Normal,
            int tickOrder = (int)ETickOrder.Timers)
        {
            if (_isRunning)
                Cancel();
            else
                Reset();

            if (maxFires == 0 || method is null)
                return;
            
            _singleMethod = method;
            _fireMax = maxFires;

            _timeBetweenFires = timeBetweenFires;
            _currentTimeBetweenFires = startTime ?? TimeSpan.Zero;
            _isRunning = true;

            OwningComponent.RegisterTick(_tickGroup = tickGroup, _tickOrder = tickOrder, _tickAction = TickMulti);
        }

        private void TickMulti()
        {
            var delta = TimeSpan.FromSeconds(Engine.Delta);
            _totalElapsed += delta;
            _elapsedSinceLastFire += delta;

            if (_elapsedSinceLastFire <= _currentTimeBetweenFires)
                return;
            
            _currentTimeBetweenFires = _timeBetweenFires;
            _multiMethod?.Invoke(_totalElapsed, _fireNumber++);
            _singleMethod?.Invoke();
            _elapsedSinceLastFire = TimeSpan.Zero;

            if (_fireMax >= 0 && _fireNumber >= _fireMax)
                Cancel();
        }

        private void TickSingle()
        {
            var delta = TimeSpan.FromSeconds(Engine.Delta);
            _totalElapsed += delta;
            _elapsedSinceLastFire += delta;

            if (_elapsedSinceLastFire <= _currentTimeBetweenFires)
                return;
            
            _currentTimeBetweenFires = _timeBetweenFires;
            _singleMethod?.Invoke();

            Cancel();
        }
    }
}