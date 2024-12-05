using Extensions;

namespace XREngine.Rendering.UI
{
    public class UISplitTransform : UIBoundableTransform
    {
        private bool _verticalSplit = false;
        public bool VerticalSplit
        {
            get => _verticalSplit;
            set => SetField(ref _verticalSplit, value);
        }

        private float _splitPercent = 0.5f;
        public float SplitPercent
        {
            get => _splitPercent;
            set => SetField(ref _splitPercent, value.Clamp(0.0f, 1.0f));
        }

        private UIBoundableTransform? _first;
        public UIBoundableTransform? First
        {
            get => _first;
            set => SetField(ref _first, value);
        }

        private UIBoundableTransform? _second;
        public UIBoundableTransform? Second
        {
            get => _second;
            set => SetField(ref _second, value);
        }
    }
}
