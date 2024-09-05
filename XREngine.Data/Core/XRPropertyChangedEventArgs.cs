using System.ComponentModel;

namespace XREngine.Data.Core
{
    public class XRPropertyChangedEventArgs<T>(string? propertyName, T prev, T newValue) : PropertyChangedEventArgs(propertyName)
    {
        public T PreviousValue { get; } = prev;
        public T NewValue { get; } = newValue;
    }
}
