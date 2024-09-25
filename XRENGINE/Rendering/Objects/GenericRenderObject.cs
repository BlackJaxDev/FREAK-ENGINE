using XREngine.Data.Core;

namespace XREngine.Rendering
{
    /// <summary>
    /// This is the base class for generic render objects that aren't specific to any rendering api.
    /// Rendering APIs wrap this object to provide actual rendering functionality.
    /// </summary>
    public abstract class GenericRenderObject : XRObjectBase
    {
        private readonly EventList<AbstractRenderAPIObject> _apiWrappers = [];

        private static readonly Dictionary<Type, List<GenericRenderObject>> _roCache = [];

        /// <summary>
        /// True if this object is currently in use by any window rendering API.
        /// </summary>
        public bool InUse => APIWrappers.Count > 0;

        public int GetCacheIndex()
            => _roCache.TryGetValue(GetType(), out var list) 
                ? list.IndexOf(this)
                : -1;

        public void Generate()
        {
            foreach (var wrapper in APIWrappers)
                wrapper.Generate();
        }
        public override void Destroy()
        {
            base.Destroy();
            foreach (var wrapper in APIWrappers)
                wrapper.Destroy();
            if (_roCache.TryGetValue(GetType(), out var list))
                list.Remove(this);
        }

        /// <summary>
        /// This is a list of API-specific render objects attached to each active window that represent this object.
        /// </summary>
        public IReadOnlyList<AbstractRenderAPIObject> APIWrappers => _apiWrappers;

        public GenericRenderObject()
        {
            var type = GetType();
            if (!_roCache.TryGetValue(type, out var list))
                _roCache.Add(type, list = []);
            list.Add(this);

            //Create this object for all windows (but don't generate it yet)
            var wrappers = Engine.Rendering.CreateObjectsForAllWindows(this);

            //if (wrappers.Count > 0)
            //    Debug.Out($"Created {wrappers.Count} API wrappers for {GetDescribingName()}.");

            foreach (var wrapper in wrappers)
                if (wrapper is not null)
                    AddWrapper(wrapper);
        }
        ~GenericRenderObject()
        {
            Destroy();
        }

        public string GetDescribingName()
        {
            string name = $"{GetType().Name} {GetCacheIndex()}";
            if (!string.IsNullOrWhiteSpace(Name))
                name += $" '{Name}'";
            return name;
        }

        protected override void OnDestroying()
        {
            base.OnDestroying();

            lock (_apiWrappers)
            {
                //Destroy all API wrappers that wrap this object
                Debug.Out($"{GetDescribingName()} is being destroyed. Destroying {_apiWrappers.Count} API wrapper{(_apiWrappers.Count == 1 ? "" : "s")}.");
                foreach (var wrapper in _apiWrappers)
                    wrapper.Destroy();
            }
        }

        internal void AddWrapper(AbstractRenderAPIObject apiRO)
        {
            lock (_apiWrappers)
            {
                if (_apiWrappers.Contains(apiRO))
                    return;

                _apiWrappers.Add(apiRO);
                Debug.Out($"Added API wrapper for {GetDescribingName()} to window '{apiRO.Window.Window.Title}'.");
            }
        }
        internal void RemoveWrapper(AbstractRenderAPIObject apiRO)
        {
            lock (_apiWrappers)
            {
                if (_apiWrappers.Remove(apiRO))
                    Debug.Out($"Removed API wrapper for {GetDescribingName()} from window '{apiRO.Window.Window.Title}'.");
            }
        }
    }
}
