using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Rendering;

namespace XREngine
{
    /// <summary>
    /// This base class is for any object located within a world instance.
    /// </summary>
    public abstract class XRWorldObjectBase : XRObjectBase
    {
        private XRWorldInstance? _world;
        public virtual XRWorldInstance? World 
        {
            get => _world;
            internal set => SetField(ref _world, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(World):
                    foreach (var (group, order, tick) in _tickCache)
                        RegisterTick(group, order, tick);
                    break;
            }
        }
        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(World):
                        while (_tickCache.Count > 0)
                        {
                            var (group, order, tick) = _tickCache[^1];
                            UnregisterTick(group, order, tick);
                            _tickCache.RemoveAt(_tickCache.Count - 1);
                        }
                        break;
                }
            }
            return change;
        }

        private readonly List<(ETickGroup group, int order, Engine.TickList.DelTick tick)> _tickCache = [];

        public void RegisterTick(ETickGroup group, int order, Engine.TickList.DelTick tick)
        {
            _tickCache.Add((group, order, tick));
            World?.RegisterTick(group, order, tick);
        }
        public void UnregisterTick(ETickGroup group, int order, Engine.TickList.DelTick tick)
        {
            _tickCache.Remove((group, order, tick));
            World?.UnregisterTick(group, order, tick);
        }

        public void RegisterTick(ETickGroup group, ETickOrder order, Engine.TickList.DelTick tick)
            => RegisterTick(group, (int)order, tick);
        public void UnregisterTick(ETickGroup group, ETickOrder order, Engine.TickList.DelTick tick)
            => UnregisterTick(group, (int)order, tick);
    }
}
