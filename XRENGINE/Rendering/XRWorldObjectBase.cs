using System.Collections.Concurrent;
using System.Reflection;
using XREngine.Components;
using XREngine.Data.Core;
using XREngine.Rendering;

namespace XREngine
{
    /// <summary>
    /// Indicates that this property should be replicated to the network on every tick.
    /// Use for properties that change frequently - UDP protocol will be used.
    /// The SetField method is not required to change the property value.
    /// </summary>
    /// <param name="udp"></param>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReplicateOnTickAttribute : Attribute 
    {

    }

    /// <summary>
    /// Indicates that this property should be replicated to the network when it changes.
    /// Use for properties that change infrequently - UDP or TCP protocol can be used.
    /// MUST use the SetField method to change the property value.
    /// </summary>
    /// <param name="udp"></param>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReplicateOnChangeAttribute(bool udp = true) : Attribute
    {
        public bool UDP { get; } = udp;
    }

    [Serializable]
    /// <summary>
    /// This base class is for any object located within a world instance.
    /// </summary>
    public abstract class XRWorldObjectBase : XRObjectBase
    {
        private static readonly ConcurrentDictionary<Type, ReplicationInfo> _replicatedTypes = [];
        public class ReplicationInfo
        {
            internal readonly Dictionary<string, (PropertyInfo prop, bool udp)> _replicateOnChangeProperties = [];
            internal readonly Dictionary<string, PropertyInfo> _replicateOnTickProperties = [];

            public IReadOnlyDictionary<string, (PropertyInfo prop, bool udp)> ReplicateOnChangeProperties => _replicateOnChangeProperties;
            public IReadOnlyDictionary<string, PropertyInfo> ReplicateOnTickProperties => _replicateOnTickProperties;
        }

        public ReplicationInfo? GetReplicationInfo()
            => _replicatedTypes.TryGetValue(GetType(), out var repl) ? repl : null;

        public bool IsReplicateOnChangeProperty(string propName)
            => _replicatedTypes.TryGetValue(GetType(), out var repl) && repl.ReplicateOnChangeProperties.ContainsKey(propName);
        public bool IsReplicateOnTickProperty(string propName)
            => _replicatedTypes.TryGetValue(GetType(), out var repl) && repl.ReplicateOnTickProperties.ContainsKey(propName);

        private bool _hasNetworkAuthority = true;
        /// <summary>
        /// If true, this object has authority over its properties and will replicate them to the network.
        /// If false, this object will not replicate its properties to the network and will only receive updates.
        /// </summary>
        public bool HasNetworkAuthority 
        {
            get => _hasNetworkAuthority;
            internal set => SetField(ref _hasNetworkAuthority, value);
        }

        static XRWorldObjectBase()
        {
            CollectReplicableProperties();
        }

        private static void CollectReplicableProperties()
        {
            _replicatedTypes.Clear();
            BindingFlags replicableFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            //Get all types deriving from XRWorldObjectBase
            var baseType = typeof(XRWorldObjectBase);
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
            //var derivedTypes = allTypes.Where(t => t.IsAssignableTo(baseType));
            //var allProperties = derivedTypes.SelectMany(t => t.GetProperties(replicableFlags));
            void TestType(Type t)
            {
                if (!t.IsAssignableTo(baseType))
                    return;

                var props = t.GetProperties(replicableFlags);
                if (props.Length == 0)
                    return;

                ReplicationInfo? repl = null;
                foreach (var prop in props)
                {
                    if (prop.GetCustomAttribute(typeof(ReplicateOnChangeAttribute), true) is ReplicateOnChangeAttribute changeAttr)
                    {
                        repl ??= new ReplicationInfo();
                        repl._replicateOnChangeProperties.Add(prop.Name, (prop, changeAttr.UDP));
                    }
                    if (prop.GetCustomAttribute(typeof(ReplicateOnTickAttribute), true) is ReplicateOnTickAttribute)
                    {
                        repl ??= new ReplicationInfo();
                        repl._replicateOnTickProperties.Add(prop.Name, prop);
                    }
                }

                if (repl is not null)
                    _replicatedTypes.AddOrUpdate(t, repl, (k, v) => repl);
            }
            Parallel.ForEach(allTypes, TestType);
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

            var repl = GetReplicationInfo();
            if (repl is null)
                return;

            if (propName is not null && repl.ReplicateOnChangeProperties.TryGetValue(propName, out var pair))
                EnqueuePropertyReplication(propName, field, pair.udp);
        }

        private void BroadcastTickedProperties()
        {
            var repl = GetReplicationInfo();
            if (repl is null)
                return;

            foreach (var prop in repl.ReplicateOnTickProperties.Values)
                EnqueuePropertyReplication(prop.Name, prop.GetValue(this), true);
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

        public void RegisterAnimationTick(Action<XRWorldObjectBase> tick, ETickGroup group = ETickGroup.Normal)
            => RegisterTick(group, ETickOrder.Animation, () => tick(this));
        public void RegisterAnimationTick<T>(Action<T> tick, ETickGroup group = ETickGroup.Normal) where T : XRWorldObjectBase
            => RegisterTick(group, ETickOrder.Animation, () => tick((T)this));

        /// <summary>
        /// Tells the engine to replicate this object to the network.
        /// </summary>
        /// <param name="udp"></param>
        public void EnqueueSelfReplication(bool udp)
            => Engine.Networking.Broadcast(this, udp);
        /// <summary>
        /// Tells the engine to replicate a specific property to the network.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propName"></param>
        /// <param name="value"></param>
        /// <param name="udp"></param>
        public void EnqueuePropertyReplication<T>(string? propName, T value, bool udp)
            => Engine.Networking.BroadcastPropertyUpdated(this, propName, value, udp);
        /// <summary>
        /// Tells the engine to replicate some data to the network.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <param name="udp"></param>
        public void EnqueueDataReplication(string id, object data, bool udp)
            => Engine.Networking.BroadcastData(this, data, id, udp);

        /// <summary>
        /// Called by data replication to receive generic data from the network.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public virtual void ReceiveData(string id, object data)
        {

        }

        /// <summary>
        /// Called by full replication to copy all properties from the replicated object.
        /// </summary>
        /// <param name="newObj"></param>
        public virtual void CopyFrom(XRWorldObjectBase newObj)
        {

        }

        /// <summary>
        /// Called by property replication to set a specific property from the network.
        /// </summary>
        /// <param name="propName"></param>
        /// <param name="value"></param>
        public void SetReplicatedProperty(string propName, object value)
        {
            var repl = GetReplicationInfo();
            if (repl is null)
                return;

            if (repl.ReplicateOnChangeProperties.TryGetValue(propName, out var pair) && 
                pair.prop.PropertyType.IsAssignableFrom(value.GetType()))
                pair.prop.SetValue(this, value);
            
            if (repl.ReplicateOnTickProperties.TryGetValue(propName, out var prop) && 
                prop.PropertyType.IsAssignableFrom(value.GetType()))
                prop.SetValue(this, value);
        }
    }
}
