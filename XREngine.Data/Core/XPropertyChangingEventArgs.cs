using System.ComponentModel;

namespace XREngine.Data.Core
{
    public class XPropertyChangingEventArgs<T>(string? propertyName, T prev, T @new) : PropertyChangingEventArgs(propertyName), IChangingValueEventArgs
    {
        public T Previous = prev;
        public T New = @new;
        public bool AllowChange = true;

        object? IChangingValueEventArgs.Previous => Previous;
        object? IChangingValueEventArgs.New => New;
    }
}
