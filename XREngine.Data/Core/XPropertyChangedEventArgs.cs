using System.ComponentModel;

namespace XREngine.Data.Core
{
    public class XPropertyChangedEventArgs<T>(string? propertyName, T prev, T @new) : PropertyChangedEventArgs(propertyName), IChangingValueEventArgs
    {
        public T Previous = prev;
        public T New = @new;

        object? IChangingValueEventArgs.Previous => Previous;
        object? IChangingValueEventArgs.New => New;
    }
}
