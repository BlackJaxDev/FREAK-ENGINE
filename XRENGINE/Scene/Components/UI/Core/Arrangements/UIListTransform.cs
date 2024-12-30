using XREngine.Data.Geometry;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    public partial class UIListTransform : UIBoundableTransform
    {
        private float? _childSize = null;
        private bool _horizontal = false;
        private float _spacing = 0.0f;
        private EListAlignment _alignment = EListAlignment.TopOrLeft;
        private bool _virtual = false;
        private float _upperVirtualBound = 0.0f;
        private float _lowerVirtualBound = 0.0f;

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
        /// The distance between each child component.
        /// </summary>
        public float ItemSpacing
        {
            get => _spacing;
            set => SetField(ref _spacing, value);
        }
        /// <summary>
        /// The alignment of the child components.
        /// </summary>
        public EListAlignment ItemAlignment
        {
            get => _alignment;
            set => SetField(ref _alignment, value);
        }
        /// <summary>
        /// If true, items will be pooled and culled if they are outside of the parent region.
        /// </summary>
        public bool Virtual
        {
            get => _virtual;
            set => SetField(ref _virtual, value);
        }
        /// <summary>
        /// The upper bound of the virtual region.
        /// </summary>
        public float UpperVirtualBound
        {
            get => _upperVirtualBound;
            set => SetField(ref _upperVirtualBound, value);
        }
        /// <summary>
        /// The lower bound of the virtual region.
        /// </summary>
        public float LowerVirtualBound
        {
            get => _lowerVirtualBound;
            set => SetField(ref _lowerVirtualBound, value);
        }
        public float VirtualRegionSize => UpperVirtualBound - LowerVirtualBound;
        public void SetVirtualBounds(float upper, float lower)
        {
            UpperVirtualBound = upper;
            LowerVirtualBound = lower;
        }
        public void SetVirtualBoundsRelativeToTop(float size)
        {
            LowerVirtualBound = UpperVirtualBound - size;
        }
        public void SetVirtualBoundsRelativeToBottom(float size)
        {
            UpperVirtualBound = LowerVirtualBound + size;
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
                TransformBase? child = Children[i];
                if (child is not UIBoundableTransform bc || bc.PlacementInfo is not UIListChildPlacementInfo placementInfo)
                    continue;

                if (DisplayHorizontal)
                {
                    float parentHeight = parentRegion.Height;
                    float size = ItemSize ?? bc.GetWidth();
                    x -= size;
                    placementInfo.Offset = x;

                    bc.FitLayout(new BoundingRectangleF(x, y, size, parentHeight));

                    if (i > 0)
                        x -= ItemSpacing;
                }
                else
                {
                    float parentWidth = parentRegion.Width;
                    float size = ItemSize ?? bc.GetHeight();
                    y -= size;
                    placementInfo.Offset = y;

                    bc.FitLayout(new BoundingRectangleF(x, y, parentWidth, size));

                    if (i > 0)
                        y -= ItemSpacing;
                }
            }
        }

        private void SizeChildrenCentered(BoundingRectangleF parentRegion, ref float y, ref float x)
        {
            float[] sizes = new float[Children.Count];
            float totalSize = CalcTotalSize(sizes);

            if (DisplayHorizontal)
                x += (parentRegion.Width - totalSize) / 2.0f;
            else
                y += (parentRegion.Height - totalSize) / 2.0f;

            for (int i = 0; i < Children.Count; i++)
            {
                TransformBase? child = Children[i];
                if (child is not UIBoundableTransform bc || bc.PlacementInfo is not UIListChildPlacementInfo placementInfo)
                    continue;

                if (DisplayHorizontal)
                {
                    float parentHeight = parentRegion.Height;
                    float size = sizes[i];
                    placementInfo.Offset = x;

                    FitLayoutHorizontal(y, x, bc, parentHeight, size);
                    Increment(ref x, i, size);
                }
                else
                {
                    float parentWidth = parentRegion.Width;
                    float size = sizes[i];
                    placementInfo.Offset = y;

                    FitLayoutVertical(y, x, bc, parentWidth, size);
                    Increment(ref y, i, size);
                }
            }
        }

        private float CalcTotalSize(float[] sizes)
        {
            float totalSize = 0.0f;
            for (int i = 0; i < Children.Count; i++)
            {
                TransformBase? child = Children[i];
                if (child is not UIBoundableTransform bc)
                    continue;

                float size = ItemSize ?? (DisplayHorizontal ? bc.GetWidth() : bc.GetHeight());

                sizes[i] = size;
                totalSize += size;
                if (i < Children.Count - 1)
                    totalSize += ItemSpacing;
            }

            return totalSize;
        }

        private void Increment(ref float value, int i, float size)
        {
            value += size;
            if (i < Children.Count - 1)
                value += ItemSpacing;
        }

        private void SizeChildrenLeftTop(BoundingRectangleF parentRegion, ref float y, ref float x)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                TransformBase? child = Children[i];
                if (child is not UIBoundableTransform bc || bc.PlacementInfo is not UIListChildPlacementInfo placementInfo)
                    continue;

                if (DisplayHorizontal)
                {
                    float parentHeight = parentRegion.Height;
                    float size = ItemSize ?? bc.GetWidth();
                    placementInfo.Offset = x;

                    FitLayoutHorizontal(y, x, bc, parentHeight, size);
                    Increment(ref x, i, size);
                }
                else
                {
                    float parentWidth = parentRegion.Width;
                    float size = ItemSize ?? bc.GetHeight();
                    placementInfo.Offset = y;

                    FitLayoutVertical(y, x, bc, parentWidth, size);
                    Increment(ref y, i, size);
                }
            }
        }

        private void FitLayoutHorizontal(float y, float x, UIBoundableTransform bc, float parentHeight, float size)
        {
            if (Virtual)
            {
                if (x + size < LowerVirtualBound || x > UpperVirtualBound)
                {
                    bc.Visibility = EVisibility.Collapsed;
                    if (bc.SceneNode is not null)
                        bc.SceneNode.IsActiveSelf = false;
                }
                else
                {
                    bc.Visibility = EVisibility.Visible;
                    if (bc.SceneNode is not null)
                        bc.SceneNode.IsActiveSelf = true;

                    bc.FitLayout(new BoundingRectangleF(x, y, size, parentHeight));
                }
            }
            else
            {
                bc.FitLayout(new BoundingRectangleF(x, y, size, parentHeight));
            }
        }

        private void FitLayoutVertical(float y, float x, UIBoundableTransform bc, float parentWidth, float size)
        {
            if (Virtual)
            {
                if (y + size < LowerVirtualBound || y > UpperVirtualBound)
                {
                    bc.Visibility = EVisibility.Collapsed;
                    if (bc.SceneNode is not null)
                        bc.SceneNode.IsActiveSelf = false;
                }
                else
                {
                    bc.Visibility = EVisibility.Visible;
                    if (bc.SceneNode is not null)
                        bc.SceneNode.IsActiveSelf = true;

                    bc.FitLayout(new BoundingRectangleF(x, y, parentWidth, size));
                }
            }
            else
            {
                bc.FitLayout(new BoundingRectangleF(x, y, parentWidth, size));
            }
        }

        public override void VerifyPlacementInfo(UITransform childTransform, ref UIChildPlacementInfo? placementInfo)
        {
            if (placementInfo is not UIListChildPlacementInfo)
                placementInfo = new UIListChildPlacementInfo(childTransform);
        }
    }
}