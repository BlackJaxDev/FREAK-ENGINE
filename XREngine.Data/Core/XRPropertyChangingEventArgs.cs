using System.ComponentModel;

namespace XREngine.Data.Core
{
    public class XRPropertyChangingEventArgs<T>(string? propertyName, T currentValue, T newValue) : PropertyChangingEventArgs(propertyName)
    {
        public T CurrentValue { get; } = currentValue;
        public T NewValue { get; } = newValue;
        public bool AllowChange { get; set; } = true;
    }
}
