﻿using XREngine.Data.Core;

namespace XREngine.Rendering
{
    /// <summary>
    /// This is the base class for generic render objects that aren't specific to any rendering api.
    /// Rendering APIs wrap this object to provide actual rendering functionality.
    /// </summary>
    public class GenericRenderObject : XRObjectBase
    {
        private readonly EventList<AbstractRenderAPIObject> _apiWrappers;

        /// <summary>
        /// True if this object is currently in use by any window rendering API.
        /// </summary>
        public bool InUse => APIWrappers.Count > 0;

        /// <summary>
        /// This is a list of API-specific render objects attached to each active window that represent this object.
        /// </summary>
        public IReadOnlyList<AbstractRenderAPIObject> APIWrappers => _apiWrappers;

        public GenericRenderObject()
            => _apiWrappers = new EventList<AbstractRenderAPIObject>(Engine.Rendering.CreateObjectsForAllWindows(this));

        protected override void OnDestroying()
        {
            base.OnDestroying();

            //Destroy all API wrappers that wrap this object
            foreach (var wrapper in _apiWrappers)
                wrapper.Dispose();
        }

        internal void AddWrapper(AbstractRenderAPIObject apiRO)
        {
            if (!_apiWrappers.Contains(apiRO))
                _apiWrappers.Add(apiRO);
        }
        internal void RemoveWrapper(AbstractRenderAPIObject apiRO)
        {
            _apiWrappers.Remove(apiRO);
        }
    }
}