using Extensions;
using System.Numerics;
using XREngine.Rendering.UI;

namespace XREngine.Networking
{
    public class VirtualizedConsoleUIComponent : UITextComponent
    {
        private readonly List<string> _items = [];
        public List<string> Items => _items;

        private readonly List<int> _visibleItems = [];
        public List<int> VisibleItems => _visibleItems;

        private float _topOffset = 0.0f;
        public float TopOffset
        {
            get => _topOffset;
            set => SetField(ref _topOffset, value);
        }

        private int _visibleItemMinIndex;
        public int VisibleItemMinIndex
        {
            get => _visibleItemMinIndex;
            private set => SetField(ref _visibleItemMinIndex, value);
        }

        private bool _autoScroll = true;
        public bool AutoScroll
        {
            get => _autoScroll;
            set => SetField(ref _autoScroll, value);
        }

        private float _fontHeight = 20.0f;
        public float FontHeight => _fontHeight;

        public int MaxVisibleItemCount
            => (int)MathF.Ceiling(BoundableTransform.ActualHeight / FontHeight);

        public VirtualizedConsoleUIComponent() { }

        //protected override void OnTransformChanged()
        //{
        //    base.OnTransformChanged();  
        //    BoundableTransform.NormalizedPivot = new Vector2(0, 0);
        //}

        /// <summary>
        /// Returns whether the item at the given index is visible.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsVisible(int index)
        {
            //Use indices instead of contains for performance, and because visible items may not be updated yet.
            int max = Math.Min(_items.Count - _visibleItemMinIndex, MaxVisibleItemCount);
            return index >= _visibleItemMinIndex && index < _visibleItemMinIndex + max;
        }

        public void AddItem(string? message)
        {
            bool anyItemsOffscreenLower = AutoScroll && AnyItemsOffscreenLower();
            _items.Add(message ?? "");
            if (IsVisible(_items.Count - 1))
                UpdateVisibleText();
            if (AutoScroll && !anyItemsOffscreenLower && AnyItemsOffscreenLower())
                Scroll(FontHeight);
        }

        public void AddToLastItem(string? message)
        {
            if (_items.Count > 0)
                _items[^1] += message ?? "";
            if (IsVisible(_items.Count - 1))
                UpdateVisibleText();
        }

        public void Scroll(float amount)
        {
            TopOffset += amount;
            if (TopOffset < 0)
                TopOffset = 0;
            else if (TopOffset > BoundableTransform.ActualHeight)
                TopOffset = BoundableTransform.ActualHeight;
            float fontHeight = FontHeight;
            int hiddenItemCount = ((int)MathF.Floor(TopOffset / fontHeight)).ClampMax(_items.Count);
            float hiddenOffset = hiddenItemCount * fontHeight;
            float topDelta = TopOffset - hiddenOffset;
            float yOffset = -topDelta;
            BoundableTransform.LocalPivotTranslation = new Vector2(0, yOffset);
            VisibleItemMinIndex = hiddenItemCount;
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Font):
                case nameof(FontSize):
                    _fontHeight = Font?.MeasureString("A", FontSize ?? 0.0f).Y ?? 0;
                    UpdateVisibleText();
                    break;
                case nameof(VisibleItemMinIndex):
                    UpdateVisibleText();
                    break;
            }
        }

        private void UpdateVisibleText()
        {
            string text = "";
            _visibleItems.Clear();
            int max = Math.Min(_items.Count - _visibleItemMinIndex, MaxVisibleItemCount);
            for (int i = 0; i < max; i++)
            {
                int index = _visibleItemMinIndex + i;
                _visibleItems.Add(index);
                text += _items[index] + Environment.NewLine;
            }
            Text = text;
        }

        /// <summary>
        /// Returns whether any items are offscreen above the visible area.
        /// "Upper" refers to the start of the list, because the list is displayed from top to bottom.
        /// </summary>
        /// <returns></returns>
        public bool AnyItemsOffscreenUpper()
            => _visibleItemMinIndex > 0;

        /// <summary>
        /// Returns whether any items are offscreen below the visible area.
        /// "Lower" refers to the end of the list, because the list is displayed from top to bottom.
        /// </summary>
        /// <returns></returns>
        public bool AnyItemsOffscreenLower()
            => (_items.Count - _visibleItemMinIndex) > MaxVisibleItemCount;
    }
}