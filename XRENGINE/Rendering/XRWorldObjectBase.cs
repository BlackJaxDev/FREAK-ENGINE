using System.Reflection;
using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Rendering;

namespace XREngine
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ReplicateOnTickAttribute(bool udp) : Attribute 
    {
        public bool UDP { get; } = udp;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ReplicateOnChangeAttribute(bool udp) : Attribute
    {
        public bool UDP { get; } = udp;
    }

    [Serializable]
    /// <summary>
    /// This base class is for any object located within a world instance.
    /// </summary>
    public abstract class XRWorldObjectBase : XRObjectBase
    {
        private static readonly Dictionary<string, (PropertyInfo prop, bool udp)> _replicateOnChangeProperties = [];
        private static readonly Dictionary<string, (PropertyInfo prop, bool udp)> _replicateOnTickProperties = [];

        static XRWorldObjectBase()
        {
            //Collect all properties with replicate attributes
            foreach (var prop in typeof(XRWorldObjectBase).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.GetCustomAttribute(typeof(ReplicateOnChangeAttribute), true) is ReplicateOnChangeAttribute changeAttr)
                    _replicateOnChangeProperties.Add(prop.Name, (prop, changeAttr.UDP));
                if (prop.GetCustomAttribute(typeof(ReplicateOnTickAttribute), true) is ReplicateOnTickAttribute tickAttr)
                    _replicateOnTickProperties.Add(prop.Name, (prop, tickAttr.UDP));
            }
        }
        public XRWorldObjectBase()
        {
            if (_replicateOnTickProperties.Count > 0)
                RegisterTick(ETickGroup.Normal, ETickOrder.Logic, BroadcastTickedProperties);
        }

        internal XRWorldInstance? _world = null;
        public XRWorldInstance? World 
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
                    {
                        if (_tickCache is not null)
                            foreach (var (group, order, tick) in _tickCache)
                                World?.RegisterTick(group, order, tick);
                    }
                    break;
            }
            if (propName is not null && _replicateOnChangeProperties.TryGetValue(propName, out var pair))
                BroadcastProperyUpdated(propName, field, pair.udp);
        }

        private void BroadcastTickedProperties()
        {
            foreach (var (prop, udp) in _replicateOnTickProperties.Values)
                BroadcastProperyUpdated(prop.Name, prop.GetValue(this), udp);
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
                        {
                            if (_tickCache is not null)
                                foreach (var (group, order, tick) in _tickCache)
                                    World?.UnregisterTick(group, order, tick);
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

        public void BroadcastSelf(bool udp)
            => Engine.Networking.Broadcast(this, udp);
        public void BroadcastProperyUpdated<T>(string? propName, T value, bool udp)
            => Engine.Networking.BroadcastPropertyUpdated(this, propName, value, udp);
        public void BroadcastData(string id, object data, bool udp)
            => Engine.Networking.BroadcastData(this, data, id, udp);

        public virtual void ReceiveData(string id, object data)
        {

        }

        public virtual void CopyFrom(XRWorldObjectBase newObj)
        {

        }

        public void SetReplicatedProperty(string propName, object value)
        {
            if (_replicateOnChangeProperties.TryGetValue(propName, out var pair) && pair.prop.PropertyType.IsAssignableFrom(value.GetType()))
                pair.prop.SetValue(this, value);
            
            if (_replicateOnTickProperties.TryGetValue(propName, out var pair2) && pair2.prop.PropertyType.IsAssignableFrom(value.GetType()))
                pair2.prop.SetValue(this, value);
        }
    }
}
