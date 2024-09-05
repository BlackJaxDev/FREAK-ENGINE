namespace XREngine.Data.Core
{
    /// <summary>
    /// Interface for changing / changed event handler args classes.
    /// </summary>
    public interface IChangingValueEventArgs
    {
        /// <summary>
        /// The old value of the field
        /// </summary>
        object? Previous { get; }
        /// <summary>
        /// The new value of the field
        /// </summary>
        object? New { get; }
    }
}
