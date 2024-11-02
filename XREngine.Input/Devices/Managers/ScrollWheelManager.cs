using Extensions;

namespace XREngine.Input.Devices
{
    public delegate void DelMouseScroll(bool down);
    [Serializable]
    public class ScrollWheelManager : InputManagerBase
    {
        private readonly List<DelMouseScroll> _onUpdate = [];

        private float _lastValue = 0.0f;

        internal void Tick(float value, float delta)
        {
            if (value.EqualTo(_lastValue))
                return;

            if (value < _lastValue)
            {
                OnUpdate(true);
                _lastValue = value;
            }
            else if (value > _lastValue)
            {
                OnUpdate(false);
                _lastValue = value;
            }
        }
        public void Register(DelMouseScroll func, bool unregister)
        {
            lock (_onUpdate)
            {
                if (unregister)
                {
                    int index = _onUpdate.FindIndex(x => x == func);
                    if (index >= 0 && index < _onUpdate.Count)
                        _onUpdate.RemoveAt(index);
                }
                else
                    _onUpdate.Add(func);
            }
        }
        private void OnUpdate(bool down)
        {
            lock (_onUpdate)
            {
                for (int x = 0; x < _onUpdate.Count; ++x)
                    _onUpdate[x](down);
            }
        }
    }
}
