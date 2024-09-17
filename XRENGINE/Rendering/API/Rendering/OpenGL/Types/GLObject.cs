using System.Collections.Generic;

namespace XREngine.Rendering.OpenGL
{
    public unsafe partial class OpenGLRenderer
    {
        /// <summary>
        /// Generic OpenGL object base class for specific derived generic render objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract class GLObject<T> : GLObjectBase where T : GenericRenderObject
        {
            //We want to set the property instead of the field here just in case subclasses override it.
            //It will never be set to null because the constructor requires a non-null value.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public GLObject(OpenGLRenderer renderer, T data) : base(renderer)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            {
                _data = data;
                LinkData();
            }
            
            protected override GenericRenderObject Data_Internal => Data;

            private T _data;
            public T Data
            {
                get => _data;
                protected set
                {
                    if (value == _data)
                        return;

                    UnlinkData();
                    _data = value;
                    LinkData();
                }
            }

            protected virtual void UnlinkData()
                => _data.RemoveWrapper(this);
            protected virtual void LinkData()
                => _data.AddWrapper(this);

            protected internal override void PostGenerated()
            {
                base.PostGenerated();

                if (TryGetBindingId(out var bindingId) && bindingId > 0)
                {
                    if (Cache.ContainsKey(bindingId))
                    {
                        Debug.LogWarning($"OpenGL {Type} object with binding id {bindingId} already exists in cache.");
                        Cache[bindingId] = this;
                    }
                    else
                        Cache.Add(bindingId, this);
                }
                else
                    Debug.LogWarning($"Failed to generate OpenGL {Type} object.");
            }
            protected internal override void PostDeleted()
            {
                if (TryGetBindingId(out var bindingId))
                    Cache.Remove(bindingId);

                base.PostDeleted();
            }

            public static EventDictionary<uint, GLObject<T>> Cache { get; } = [];
        }
    }
}