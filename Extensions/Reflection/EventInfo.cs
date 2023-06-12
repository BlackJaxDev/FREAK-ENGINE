using System;
using System.Reflection;

namespace Extensions
{
    public static partial class Ext
    {
        /// <summary>
        /// Retrieves all method delegates that have been added as subscribers to this event for the given object.
        /// </summary>
        /// <param name="eventInfo">The event to retrieve subscribers from.</param>
        /// <param name="obj">The object that owns the event.</param>
        /// <returns>All subscribing delegates.</returns>
        public static Delegate[] GetSubscribedMethods(this EventInfo eventInfo, object obj)
        {
            FieldInfo field = eventInfo.DeclaringType.GetField(eventInfo.Name,
                BindingFlags.NonPublic |
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.GetField);
            Delegate d = field.GetValue(obj) as Delegate;
            return d?.GetInvocationList() ?? new Delegate[0];
        }
    }
}
