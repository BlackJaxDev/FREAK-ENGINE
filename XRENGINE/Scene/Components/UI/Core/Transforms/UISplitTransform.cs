using Extensions;
using System.Numerics;
using XREngine.Data.Geometry;

namespace XREngine.Rendering.UI
{
    /// <summary>
    /// A transform that splits two children into two regions.
    /// The user can drag the splitter to resize the regions.
    /// </summary>
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

        private float _splitterSize = 5.0f;
        public float SplitterSize
        {
            get => _splitterSize;
            set => SetField(ref _splitterSize, value);
        }

        private bool _canUserResize = true;
        public bool CanUserResize
        {
            get => _canUserResize;
            set => SetField(ref _canUserResize, value);
        }

        public UIBoundableTransform? First => Children.FirstOrDefault() as UIBoundableTransform;
        public UIBoundableTransform? Second => Children.LastOrDefault() as UIBoundableTransform;

        protected override void OnResizeChildComponents(BoundingRectangleF parentRegion)
        {
            var a = First;
            var b = Second;
            if (a == null || b == null)
                return;

            if (VerticalSplit)
            {
                float split = parentRegion.Height * SplitPercent;
                a.FitLayout(new(parentRegion.X, parentRegion.Y, parentRegion.Width, split));
                b.FitLayout(new(parentRegion.X, parentRegion.Y + split, parentRegion.Width, parentRegion.Height - split));
            }
            else
            {
                float split = parentRegion.Width * SplitPercent;
                a.FitLayout(new(parentRegion.X, parentRegion.Y, split, parentRegion.Height));
                b.FitLayout(new(parentRegion.X + split, parentRegion.Y, parentRegion.Width - split, parentRegion.Height));
            }
        }
    }
}
