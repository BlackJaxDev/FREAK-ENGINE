using XREngine.Components;

namespace XREngine.Networking
{
    public class VirtualizedListUIComponent : XRComponent
    {
        public List<string> Items { get; } = [];
        public List<int> SelectedItems { get; } = [];
        public List<int> VisibleItems { get; } = [];
        public float VisibleItemMinIndex { get; set; }
        
        public VirtualizedListUIComponent() { }

        public void AddItem(string? message)
        {

        }

        public void AddToLastItem(string? message)
        {

        }
    }
}