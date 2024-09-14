using XREngine.Data.Core;

namespace XREngine.Rendering
{
    /// <summary>
    /// This is the base class for generic render objects that aren't specific to any rendering api.
    /// Rendering APIs wrap this object to provide actual rendering functionality.
    /// </summary>
    public class GenericRenderObject : XRObjectBase
    {
        private EventList<AbstractRenderAPIObject>? _apiWrappers;
        private EventList<AbstractRenderAPIObject> APIWrappersInternal => _apiWrappers ??= []; //new EventList<AbstractRenderAPIObject>(/*Engine.Rendering.CreateObjectsForAllWindows(this)*/);

        /// <summary>
        /// True if this object is currently in use by any window rendering API.
        /// </summary>
        public bool InUse => APIWrappers.Count > 0;

        /// <summary>
        /// This is a list of API-specific render objects attached to each active window that represent this object.
        /// </summary>
        public IReadOnlyList<AbstractRenderAPIObject> APIWrappers => APIWrappersInternal;

        protected override void OnDestroying()
        {
            base.OnDestroying();

            //Destroy all API wrappers that wrap this object
            foreach (var wrapper in APIWrappersInternal)
                wrapper.Dispose();
        }

        internal void AddWrapper(AbstractRenderAPIObject apiRO)
        {
            if (!APIWrappersInternal.Contains(apiRO))
                APIWrappersInternal.Add(apiRO);
        }
        internal void RemoveWrapper(AbstractRenderAPIObject apiRO)
        {
            APIWrappersInternal.Remove(apiRO);
        }
    }
}
