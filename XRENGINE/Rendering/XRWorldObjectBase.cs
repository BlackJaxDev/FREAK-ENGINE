using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Rendering;

namespace XREngine
{
    [Serializable]
    /// <summary>
    /// This base class is for any object located within a world instance.
    /// </summary>
    public abstract class XRWorldObjectBase : XRObjectBase
    {
        internal XRWorldInstance? _world;
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
                    if (World is not null)
                        foreach (var (group, order, tick) in _tickCache)
                            World?.RegisterTick(group, order, tick);
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
                        if (World is not null)
                            foreach (var (group, order, tick) in _tickCache)
                                World?.UnregisterTick(group, order, tick);
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

        protected internal void ClearTicks()
        {
            while (_tickCache.Count > 0)
            {
                (ETickGroup group, int order, Engine.TickList.DelTick tick) tick = _tickCache[0];
                UnregisterTick(tick.group, tick.order, tick.tick);
            }
        }

        public void RegisterTick(ETickGroup group, ETickOrder order, Engine.TickList.DelTick tick)
            => RegisterTick(group, (int)order, tick);
        public void UnregisterTick(ETickGroup group, ETickOrder order, Engine.TickList.DelTick tick)
            => UnregisterTick(group, (int)order, tick);

        public void RegisterAnimationTick(Action<XRWorldObjectBase> tick, int order = (int)ETickOrder.Animation, ETickGroup group = ETickGroup.Normal)
            => RegisterTick(group, order, () => tick(this));
        public void RegisterAnimationTick(Action<XRWorldObjectBase> tick, ETickOrder order = ETickOrder.Animation, ETickGroup group = ETickGroup.Normal)
            => RegisterAnimationTick(tick, (int)order, group);

        public void RegisterAnimationTick<T>(Action<T> tick, int order, ETickGroup group = ETickGroup.Normal) where T : XRWorldObjectBase
            => RegisterTick(group, order, () => tick((T)this));
        public void RegisterAnimationTick<T>(Action<T> tick, ETickGroup group = ETickGroup.Normal) where T : XRWorldObjectBase
            => RegisterAnimationTick(tick, (int)ETickOrder.Animation, group);
    }
}
