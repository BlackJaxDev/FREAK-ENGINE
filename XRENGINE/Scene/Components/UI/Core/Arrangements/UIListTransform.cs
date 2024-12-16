using XREngine.Data.Geometry;

namespace XREngine.Rendering.UI
{
    public partial class UIListTransform : UIBoundableTransform
    {
        private float? _childSize = null;
        private bool _horizontal = false;
        private float scrollbarSize = 10.0f;
        private float _spacing = 0.0f;
        private EListAlignment _alignment = EListAlignment.TopOrLeft;

        /// <summary>
        /// The width or height of each child component.
        /// If null, the size of each child is determined by the child's own size.
        /// </summary>
        public float? ItemSize
        {
            get => _childSize;
            set => SetField(ref _childSize, value);
        }
        /// <summary>
        /// If the the list should display left to right instead of top to bottom.
        /// </summary>
        public bool DisplayHorizontal
        {
            get => _horizontal;
            set => SetField(ref _horizontal, value);
        }
        /// <summary>
        /// The width or height of the scrollbar.
        /// </summary>
        public float ScrollbarSize
        {
            get => scrollbarSize;
            set => SetField(ref scrollbarSize, value);
        }
        /// <summary>
        /// The distance between each child component.
        /// </summary>
        public float ItemSpacing
        {
            get => _spacing;
            set => SetField(ref _spacing, value);
        }
        public EListAlignment ItemAlignment
        {
            get => _alignment;
            set => SetField(ref _alignment, value);
        }

        protected override void OnResizeChildComponents(BoundingRectangleF parentRegion)
        {
            //TODO: clip children that are outside of the parent region
            float y = parentRegion.Y;
            float x = parentRegion.X;
            lock (Children)
            {
                switch (ItemAlignment)
                {
                    case EListAlignment.TopOrLeft:
                        SizeChildrenLeftTop(parentRegion, ref y, ref x);
                        break;
                    case EListAlignment.Centered:
                        SizeChildrenCentered(parentRegion, ref y, ref x);
                        break;
                    case EListAlignment.BottomOrRight:
                        SizeChildrenRightBottom(parentRegion, ref y, ref x);
                        break;
                }
            }
        }

        private void SizeChildrenRightBottom(BoundingRectangleF parentRegion, ref float y, ref float x)
        {
            //TODO: verify this was implemented correctly
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                Scene.Transforms.TransformBase? child = Children[i];
                if (child is not UIBoundableTransform bc)
                    continue;
                if (DisplayHorizontal)
                {
                    float parentHeight = parentRegion.Height - scrollbarSize;
                    float size = ItemSize ?? bc.GetWidth();
                    bc.FitLayout(new BoundingRectangleF(x - size, y, size, parentHeight));
                    x -= size;
                    if (i > 0)
                        x -= ItemSpacing;
                }
                else
                {
                    float parentWidth = parentRegion.Width - scrollbarSize;
                    float size = ItemSize ?? bc.GetHeight();
                    bc.FitLayout(new BoundingRectangleF(x, y - size, parentWidth, size));
                    y -= size;
                    if (i > 0)
                        y -= ItemSpacing;
                }
            }
        }

        private void SizeChildrenCentered(BoundingRectangleF parentRegion, ref float y, ref float x)
        {
            //TODO: verify this was implemented correctly
            float totalSize = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                Scene.Transforms.TransformBase? child = Children[i];
                if (child is not UIBoundableTransform bc)
                    continue;
                if (DisplayHorizontal)
                {
                    float size = ItemSize ?? bc.GetWidth();
                    totalSize += size;
                    if (i < Children.Count - 1)
                        totalSize += ItemSpacing;
                }
                else
                {
                    float size = ItemSize ?? bc.GetHeight();
                    totalSize += size;
                    if (i < Children.Count - 1)
                        totalSize += ItemSpacing;
                }
            }

            float offset = DisplayHorizontal
                ? (parentRegion.Width - totalSize) / 2
                : (parentRegion.Height - totalSize) / 2;

            for (int i = 0; i < Children.Count; i++)
            {
                Scene.Transforms.TransformBase? child = Children[i];
                if (child is not UIBoundableTransform bc)
                    continue;
                if (DisplayHorizontal)
                {
                    float parentHeight = parentRegion.Height - scrollbarSize;
                    float size = ItemSize ?? bc.GetWidth();
                    bc.FitLayout(new BoundingRectangleF(x + offset, y, size, parentHeight));
                    offset += size;
                    if (i < Children.Count - 1)
                        offset += ItemSpacing;
                }
                else
                {
                    float parentWidth = parentRegion.Width - scrollbarSize;
                    float size = ItemSize ?? bc.GetHeight();
                    bc.FitLayout(new BoundingRectangleF(x, y + offset, parentWidth, size));
                    offset += size;
                    if (i < Children.Count - 1)
                        offset += ItemSpacing;
                }
            }
        }

        private void SizeChildrenLeftTop(BoundingRectangleF parentRegion, ref float y, ref float x)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                Scene.Transforms.TransformBase? child = Children[i];
                if (child is not UIBoundableTransform bc)
                    continue;

                if (DisplayHorizontal)
                {
                    float parentHeight = parentRegion.Height - scrollbarSize;
                    float size = ItemSize ?? bc.GetWidth();
                    bc.FitLayout(new BoundingRectangleF(x, y, size, parentHeight));

                    x += size;
                    if (i < Children.Count - 1)
                        x += ItemSpacing;
                }
                else
                {
                    float parentWidth = parentRegion.Width - scrollbarSize;
                    float size = ItemSize ?? bc.GetHeight();
                    bc.FitLayout(new BoundingRectangleF(x, y, parentWidth, size));

                    y += size;
                    if (i < Children.Count - 1)
                        y += ItemSpacing;
                }
            }
        }

        public override void VerifyPlacementInfo(UITransform childTransform)
        {
            if (childTransform.PlacementInfo is not UIListChildPlacementInfo)
                childTransform.PlacementInfo = new UIListChildPlacementInfo(childTransform);
        }
    }
}