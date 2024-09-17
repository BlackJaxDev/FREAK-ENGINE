using XREngine.Data.Core;

namespace XREngine.Rendering
{
    /// <summary>
    /// This is the base class for generic render objects that aren't specific to any rendering api.
    /// Rendering APIs wrap this object to provide actual rendering functionality.
    /// </summary>
    public class GenericRenderObject : XRObjectBase
    {
        private readonly EventList<AbstractRenderAPIObject> _apiWrappers = [];

        /// <summary>
        /// True if this object is currently in use by any window rendering API.
        /// </summary>
        public bool InUse => APIWrappers.Count > 0;

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
        }

        /// <summary>
        /// This is a list of API-specific render objects attached to each active window that represent this object.
        /// </summary>
        public IReadOnlyList<AbstractRenderAPIObject> APIWrappers => _apiWrappers;

        public GenericRenderObject()
        {
            //Create this object for all windows (but don't generate it yet)
            var wrappers = Engine.Rendering.CreateObjectsForAllWindows(this);

            if (wrappers.Count > 0)
                Debug.Out($"Created {wrappers.Count} API wrappers for '{Name}'.");

            foreach (var wrapper in wrappers)
                if (wrapper is not null)
                    AddWrapper(wrapper);
        }

        protected override void OnDestroying()
        {
            base.OnDestroying();

            //Destroy all API wrappers that wrap this object
            Debug.Out($"{nameof(GenericRenderObject)} '{Name}' is being destroyed. Destroying all {_apiWrappers.Count} API wrappers.");
            foreach (var wrapper in _apiWrappers)
                wrapper.Destroy();
        }

        internal void AddWrapper(AbstractRenderAPIObject apiRO)
        {
            if (!_apiWrappers.Contains(apiRO))
            {
                Debug.Out($"Adding API wrapper for '{Name}' to window '{apiRO.Window.Window.Title}'.");
                _apiWrappers.Add(apiRO);
            }
        }
        internal void RemoveWrapper(AbstractRenderAPIObject apiRO)
        {
            Debug.Out($"Removing API wrapper for '{Name}' from window '{apiRO.Window.Window.Title}'.");
            _apiWrappers.Remove(apiRO);
        }
    }
}
