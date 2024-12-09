using System.Reflection;

namespace XREngine.Animation
{
    /// <summary>
    /// Base class for animations that animate properties such as Vector3, bool and float.
    /// </summary>
    public abstract class BasePropAnim(float lengthInSeconds, bool looped) : BaseAnimation(lengthInSeconds, looped)
    {
        public const string PropAnimCategory = "Property Animation";

        /// <summary>
        /// Call to set this animation's current value to an object's property and then advance the animation by the given delta.
        /// </summary>
        public void Tick(object obj, FieldInfo field, float delta)
        {
            field.SetValue(obj, GetCurrentValueGeneric());
            Tick(delta);
        }
        /// <summary>
        /// Call to set this animation's current value to an object's property and then advance the animation by the given delta.
        /// </summary>
        public void Tick(object obj, PropertyInfo property, float delta)
        {
            property.SetValue(obj, GetCurrentValueGeneric());
            Tick(delta);
        }
        /// <summary>
        /// Call to set this animation's current value to an object's method that takes it as a single argument and then advance the animation by the given delta.
        /// </summary>
        public void Tick(object obj, MethodInfo method, float delta, int valueArgumentIndex, object?[] methodArguments)
        {
            methodArguments[valueArgumentIndex] = GetCurrentValueGeneric();
            method.Invoke(obj, methodArguments);
            Tick(delta);
        }

        /// <summary>
        /// Retrieves the value for the animation's current time.
        /// Used by the internal animation implementation to set property/field values and call methods,
        /// so must be overridden.
        /// </summary>
        protected abstract object? GetCurrentValueGeneric();
        /// <summary>
        /// Retrieves the value for the given second.
        /// Used by the internal animation implementation to set property/field values and call methods,
        /// so must be overridden.
        /// </summary>
        protected abstract object? GetValueGeneric(float second);
    }
}