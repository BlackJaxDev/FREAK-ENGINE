using XREngine.Data.Core;

namespace XREngine.Core
{
    public enum ECollectionChangedAction
    {
        Add = 0,
        Remove = 1,
        Replace = 2,
        Move = 3,
        Clear = 4,
    }
    public delegate void TCollectionChangedEventHandler<T>(object sender, TCollectionChangedEventArgs<T> e);
    public class TCollectionChangedEventArgs<T> : XRBase
    {
        public TCollectionChangedEventArgs(ECollectionChangedAction action)
        {
            Action = action;
            OldItems = NewItems = [];
        }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, T changedItem)
            : this(action, [changedItem]) { }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, IList<T> changedItems)
        {
            Action = action;
            NewItems = OldItems = changedItems;
        }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, T changedItem, int index)
            : this(action, [changedItem], index) { }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, IList<T> changedItems, int startingIndex)
            : this(action, changedItems)
        {
            NewRangeStartIndex = OldRangeStartIndex = startingIndex;
        }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, T newItem, T oldItem)
            : this(action, [newItem], [oldItem]) { }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, IList<T> newItems, IList<T> oldItems) : this(action)
        {
            NewItems = newItems;
            OldItems = oldItems;
        }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, T newItem, T oldItem, int index)
            : this(action, [newItem], [oldItem], index) { }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, IList<T> newItems, IList<T> oldItems, int startingIndex)
            : this(action, newItems, oldItems)
        {
            NewRangeStartIndex = OldRangeStartIndex = startingIndex;
        }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, T changedItem, int newIndex, int oldIndex)
            : this(action, [changedItem], [changedItem])
        {
            NewRangeStartIndex = newIndex;
            OldRangeStartIndex = oldIndex;
        }
        public TCollectionChangedEventArgs(ECollectionChangedAction action, IList<T> changedItems, int newStartingIndex, int oldStartingIndex)
            : this(action, changedItems, changedItems)
        {
            NewRangeStartIndex = newStartingIndex;
            OldRangeStartIndex = oldStartingIndex;
        }

        public ECollectionChangedAction Action { get; }
        public IList<T> OldItems { get; }
        public IList<T> NewItems { get; }
        public int NewRangeStartIndex { get; } = -1;
        public int OldRangeStartIndex { get; } = -1;
    }
}
