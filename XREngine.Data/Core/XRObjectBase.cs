namespace XREngine.Data.Core
{
    /// <summary>
    /// This base class is for any object that is managed by the engine, has a unique ID, and should be destroyed after use.
    /// </summary>
    [Serializable]
    public abstract class XRObjectBase : XRBase, IDisposable
    {
        public Guid ID { get; protected set; } = Guid.NewGuid();
        
        private static EventDictionary<Guid, XRObjectBase> ObjectsCacheInternal { get; } = [];
        public static IReadOnlyDictionary<Guid, XRObjectBase> ObjectsCache => ObjectsCacheInternal;

        public static void DestroyAllObjects()
        {
            foreach (var obj in ObjectsCacheInternal.Values)
                obj.Destroy();
            ObjectsCacheInternal.Clear();
        }
        public static void ValidateObjectCache()
        {
            foreach (var obj in ObjectsCacheInternal.Values)
                if (obj.IsDestroyed)
                    ObjectsCacheInternal.Remove(obj.ID);
        }

        public XRObjectBase()
        {
            int tries = 0;
            while (ObjectsCacheInternal.ContainsKey(ID))
            {
                //Collision, update ID and try again
                ID = Guid.NewGuid();
                if (tries++ > 10)
                    throw new Exception("Failed to generate a unique ID for an object."); //Highly unlikely
            }
            ObjectsCacheInternal.Add(ID, this);
        }
        ~XRObjectBase()
        {
            Destroy();
        }

        /// <summary>
        /// Event that is called when the object is destroyed.
        /// </summary>
        public XREvent<XRObjectBase> Destroyed;
        /// <summary>
        /// Event that is called when the object has been requested to be destroyed.
        /// All listeners must return true for the object to be destroyed.
        /// </summary>
        public XRBoolEvent<XRObjectBase> Destroying;

        private string? _name;
        public string? Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        /// <summary>
        /// True if the object has been destroyed and no longer exists in a valid state.
        /// </summary>
        public bool IsDestroyed { get; private set; } = false;

        public void Destroy()
        {
            if (IsDestroyed || !Destroying.Invoke(this))
                return;

            OnDestroying();
            ObjectsCacheInternal.Remove(ID);
            IsDestroyed = true;

            Destroyed.Invoke(this);
        }

        /// <summary>
        /// Called when the object is being destroyed.
        /// </summary>
        protected virtual void OnDestroying() { }

        void IDisposable.Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
    }
}
