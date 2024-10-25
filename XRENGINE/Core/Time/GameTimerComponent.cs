using XREngine.Components;

namespace XREngine.Timers
{
    public delegate void MultiFireAction(float totalElapsed, int fireNumber);
    public class GameTimerComponent : XRComponent
    {
        public bool IsRunning => _isRunning;

        public float SecondsBetweenFires
        {
            get => _secondsBetweenFires;
            set => _secondsBetweenFires = value;
        }

        //Set on start
        private MultiFireAction? _multiMethod;
        private Action? _singleMethod;
        private float _secondsBetweenFires;
        private float _currentSecondsBetweenFires;
        private int _fireMax;

        //State
        private int _fireNumber;
        private bool _isRunning;
        private float _totalElapsed;
        private float _elapsedSinceLastFire;

        private void Reset()
        {
            _fireNumber = 0;
            _fireMax = -1;
            _totalElapsed = 0;
            _elapsedSinceLastFire = 0;
            _secondsBetweenFires = 0;
            _currentSecondsBetweenFires = 0;
            _multiMethod = null;
            _singleMethod = null;
        }
        public void Cancel()
        {
            if (!_isRunning)
                return;

            Reset();
            _isRunning = false;

            UnregisterTick(ETickGroup.Normal, (int)ETickOrder.Timers, TickMulti);
            UnregisterTick(ETickGroup.Normal, (int)ETickOrder.Timers, TickSingle);
        }
        /// <summary>
        /// Executes a method once after the given time period.
        /// </summary>
        /// <param name="method">The method to execute.</param>
        /// <param name="seconds">How much time should pass before executing the method.</param>
        public void StartSingleFire(Action method, float seconds)
        {
            if (_isRunning)
                Cancel();
            else
                Reset();

            if (seconds <= 0)
                method();
            else
            {
                _isRunning = true;
                _singleMethod = method;
                _currentSecondsBetweenFires = seconds;

                RegisterTick(ETickGroup.Normal, (int)ETickOrder.Timers, TickSingle);
            }
        }
        /// <summary>
        /// Executes a single method multiple times with a given interval of time between each execution.
        /// </summary>
        /// <param name="method">The method to execute per fire.</param>
        /// <param name="secondsBetweenFires">How many seconds should pass before running the method again.</param>
        /// <param name="maxFires">The maximum number of times the method should execute before the timer stops completely. Pass a number less than 0 for infinite.</param>
        /// <param name="startSeconds">How many seconds should pass before running the method for the first time.</param>
        public void StartMultiFire(MultiFireAction method, float secondsBetweenFires, int maxFires = -1, float startSeconds = 0.0f)
        {
            if (_isRunning)
                Cancel();
            else
                Reset();

            if (maxFires == 0 || method is null)
                return;
            
            _multiMethod = method;
            _fireMax = maxFires;

            _secondsBetweenFires = secondsBetweenFires;
            _currentSecondsBetweenFires = startSeconds;
            _isRunning = true;

            RegisterTick(ETickGroup.Normal, (int)ETickOrder.Timers, TickMulti);
        }
        /// Executes a single method multiple times with a given interval of time between each execution.
        /// </summary>
        /// <param name="method">The method to execute per fire.</param>
        /// <param name="secondsBetweenFires">How many seconds should pass before running the method again.</param>
        /// <param name="maxFires">The maximum number of times the method should execute before the timer stops completely. Pass a number less than 0 for infinite.</param>
        /// <param name="startSeconds">How many seconds should pass before running the method for the first time.</param>
        public void StartMultiFire(Action method, float secondsBetweenFires, int maxFires = -1, float startSeconds = 0.0f)
        {
            if (_isRunning)
                Cancel();
            else
                Reset();

            if (maxFires == 0 || method is null)
                return;
            
            _singleMethod = method;
            _fireMax = maxFires;

            _secondsBetweenFires = secondsBetweenFires;
            _currentSecondsBetweenFires = startSeconds;
            _isRunning = true;

            RegisterTick(ETickGroup.Normal, (int)ETickOrder.Timers, TickMulti);
        }
        private void TickMulti()
        {
            float delta = Engine.Delta;
            _totalElapsed += delta;
            _elapsedSinceLastFire += delta;
            if (_elapsedSinceLastFire > _currentSecondsBetweenFires)
            {
                _currentSecondsBetweenFires = _secondsBetweenFires;
                _multiMethod?.Invoke(_totalElapsed, _fireNumber++);
                _singleMethod?.Invoke();
                _elapsedSinceLastFire = 0;
                if (_fireMax >= 0 && _fireNumber >= _fireMax)
                    Cancel();
            }
        }
        private void TickSingle()
        {
            float delta = Engine.Delta;
            _totalElapsed += delta;
            _elapsedSinceLastFire += delta;
            if (_elapsedSinceLastFire > _currentSecondsBetweenFires)
            {
                _currentSecondsBetweenFires = _secondsBetweenFires;
                _singleMethod?.Invoke();
                Cancel();
            }
        }
    }
}