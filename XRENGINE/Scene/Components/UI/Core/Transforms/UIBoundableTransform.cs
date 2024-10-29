using Extensions;
using System.Numerics;
using XREngine.Data.Geometry;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    public class UIBoundableTransform : UITransform
    {
        public UIBoundableTransform() : base(null)
        {
            _originPercent = Vector2.Zero;
            _size = Vector2.Zero;
            _minSize = Vector2.Zero;
            _maxSize = Vector2.Zero;
            _margins = Vector4.Zero;
            _padding = Vector4.Zero;
            _verticalAlign = EVerticalAlign.Positional;
            _horizontalAlign = EHorizontalAlign.Positional;
        }
        
        protected Vector2 _actualSize = new();
        /// <summary>
        /// This is the size of the component after layout has been applied.
        /// </summary>
        public Vector2 ActualSize
        {
            get => _actualSize;
            protected set => SetField(ref _actualSize, value);
        }
        /// <summary>
        /// The width of the component after layout has been applied.
        /// </summary>
        public float ActualWidth => ActualSize.X;
        /// <summary>
        /// The height of the component after layout has been applied.
        /// </summary>
        public float ActualHeight => ActualSize.Y;

        private EVerticalAlign _verticalAlign = EVerticalAlign.Positional;
        /// <summary>
        /// How to vertically align this component within its parent.
        /// </summary>
        public EVerticalAlign VerticalAlignment
        {
            get => _verticalAlign;
            set
            {
                SetField(ref _verticalAlign, value);
                InvalidateLayout();
            }
        }

        private EHorizontalAlign _horizontalAlign = EHorizontalAlign.Positional;
        /// <summary>
        /// How to horizontally align this component within its parent.
        /// </summary>
        public EHorizontalAlign HorizontalAlignment
        {
            get => _horizontalAlign;
            set
            {
                SetField(ref _horizontalAlign, value);
                InvalidateLayout();
            }
        }

        private Vector2 _size;
        /// <summary>
        /// The requested width and height of this component before layouting.
        /// </summary>
        public Vector2 Size
        {
            get => _size;
            set => SetField(ref _size, value);
        }
        /// <summary>
        /// The requested width of this component before layouting.
        /// </summary>
        public float Width
        {
            get => Size.X;
            set => Size = new Vector2(value, Size.Y);
        }
        /// <summary>
        /// The requested height of this component before layouting.
        /// </summary>
        public float Height
        {
            get => Size.Y;
            set => Size = new Vector2(Size.X, value);
        }

        private Vector2 _minSize;
        /// <summary>
        /// The minimum width and height of this component.
        /// </summary>
        public Vector2 MinSize
        {
            get => _minSize;
            set => SetField(ref _minSize, value);
        }

        private Vector2 _maxSize;
        /// <summary>
        /// The maximum width and height of this component.
        /// </summary>
        public Vector2 MaxSize
        {
            get => _maxSize;
            set => SetField(ref _maxSize, value);
        }

        private Vector2 _originPercent = Vector2.Zero;
        /// <summary>
        /// The origin of this component as a percentage of its size.
        /// </summary>
        public Vector2 OriginPercent
        {
            get => _originPercent;
            set => SetField(ref _originPercent, value);
        }
        /// <summary>
        /// This is the origin of the component after layouting.
        /// </summary>
        public Vector2 OriginTranslation
        {
            get => OriginPercent * ActualSize;
            set
            {
                float x = ActualSize.X.IsZero() ? 0.0f : value.X / ActualSize.X;
                float y = ActualSize.Y.IsZero() ? 0.0f : value.Y / ActualSize.Y;
                OriginPercent = new(x, y);
            }
        }
        public float OriginTranslationX
        {
            get => OriginPercent.X * ActualWidth;
            set => OriginPercent = new Vector2(ActualWidth.IsZero() ? 0.0f : value / ActualWidth, OriginPercent.Y);
        }
        public float OriginTranslationY
        {
            get => OriginPercent.Y * ActualHeight;
            set => OriginPercent = new Vector2(OriginPercent.X, ActualHeight.IsZero() ? 0.0f : value / ActualHeight);
        }

        private Vector4 _margins;
        /// <summary>
        /// The outside margins of this component. X = left, Y = bottom, Z = right, W = top.
        /// </summary>
        public virtual Vector4 Margins
        {
            get => _margins;
            set => SetField(ref _margins, value);
        }

        private Vector4 _padding;
        /// <summary>
        /// The inside padding of this component. X = left, Y = bottom, Z = right, W = top.
        /// </summary>
        public virtual Vector4 Padding
        {
            get => _padding;
            set => SetField(ref _padding, value);
        }

        private Vector2 _parentPaddingOffset = Vector2.Zero;
        private Vector2 ParentPaddingOffset
        {
            get => _parentPaddingOffset;
            set => SetField(ref _parentPaddingOffset, value);
        }

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Margins):
                case nameof(Padding):
                case nameof(Size):
                case nameof(MinSize):
                case nameof(MaxSize):
                case nameof(OriginPercent):
                case nameof(ParentPaddingOffset):
                    InvalidateLayout();
                    break;
            }
        }

        protected override Matrix4x4 CreateLocalMatrix()
            => base.CreateLocalMatrix() * Matrix4x4.CreateTranslation(ParentPaddingOffset.X - OriginTranslationX, ParentPaddingOffset.Y - OriginTranslationY, 0.0f);
        
        protected override void OnResizeActual(BoundingRectangleF parentBounds)
        {
            switch (HorizontalAlignment)
            {
                case EHorizontalAlign.Stretch:
                    _actualSize.X = parentBounds.Width;
                    _translation.X = 0.0f;
                    break;
                case EHorizontalAlign.Left:
                    _actualSize.X = Size.X;
                    _translation.X = 0.0f;
                    break;
                case EHorizontalAlign.Right:
                    _actualSize.X = Size.X;
                    _translation.X = parentBounds.Width - Size.X;
                    break;
                case EHorizontalAlign.Center:
                    _actualSize.X = Size.X;
                    float extra = parentBounds.Width - Size.X;
                    _translation.X = extra * 0.5f;
                    break;
                case EHorizontalAlign.Positional:
                    _actualSize.X = Size.X;
                    break;
            }

            switch (VerticalAlignment)
            {
                case EVerticalAlign.Stretch:
                    _actualSize.Y = parentBounds.Height;
                    _translation.Y = 0.0f;
                    break;
                case EVerticalAlign.Bottom:
                    _actualSize.Y = Size.Y;
                    _translation.Y = 0.0f;
                    break;
                case EVerticalAlign.Top:
                    _actualSize.Y = Size.Y;
                    _translation.Y = parentBounds.Height - Size.Y;
                    break;
                case EVerticalAlign.Center:
                    _actualSize.Y = Size.Y;
                    float extra = parentBounds.Height - Size.Y;
                    _translation.Y = extra * 0.5f;
                    break;
                case EVerticalAlign.Positional:
                    _actualSize.Y = Size.Y;
                    break;
            }

            //X = left, Y = bottom, Z = right, W = top
            //if (Margins != null)
            //{
            //    _actualTranslation.X += Margins.X;
            //    _actualTranslation.Y += Margins.Y;
            //    _actualSize.X -= Margins.X + Margins.Z;
            //    _actualSize.Y -= Margins.Y + Margins.W;
            //}
        }

        private BoundingRectangleF _bounds = new();

        public override void FitLayout(BoundingRectangleF parentBounds)
        {
            //Set the bounds to the parent bounds.
            //This will be adjusted by the padding after the local matrix is recalculated.
            _bounds = parentBounds;
            ParentPaddingOffset = parentBounds.Translation;
            OnResizeActual(parentBounds);
            MarkLocalModified();
        }

        protected override void OnLocalMatrixChanged()
        {
            base.OnLocalMatrixChanged();
            //Update the bounds to account for the padding.
            ApplyPadding(ref _bounds);
            OnResizeChildComponents(_bounds);
            RemakeAxisAlignedRegion();
        }

        private void ApplyPadding(ref BoundingRectangleF bounds)
        {
            var pad = Padding;
            float left = pad.X;
            float bottom = pad.Y;
            float right = pad.Z;
            float top = pad.W;

            Vector2 size = ActualSize;
            Vector2 pos = Translation;

            pos += new Vector2(left, bottom);
            size -= new Vector2(left + right, bottom + top);
            bounds = new BoundingRectangleF(pos, size);
        }

        protected virtual void RemakeAxisAlignedRegion()
        {
            Matrix4x4 mtx = WorldMatrix * Matrix4x4.CreateScale(ActualSize.X, ActualSize.Y, 1.0f);

            Vector3 minPos = Vector3.Transform(Vector3.Zero, mtx);
            Vector3 maxPos = Vector3.Transform(Vector3.One, mtx); //This is Vector2.One on purpose, we only want Z to be 0

            Vector2 min = new(Math.Min(minPos.X, maxPos.X), Math.Min(minPos.Y, maxPos.Y));
            Vector2 max = new(Math.Max(minPos.X, maxPos.X), Math.Max(minPos.Y, maxPos.Y));

            RenderInfo2D.CullingVolume = BoundingRectangleF.FromMinMaxSides(min.X, max.X, min.Y, max.Y, 0.0f, 0.0f);
            //Engine.PrintLine($"Axis-aligned region remade: {_axisAlignedRegion.Translation} {_axisAlignedRegion.Extents}");
        }
        public UITransform? FindDeepestComponent(Vector2 worldPoint, bool includeThis)
        {
            try
            {
                lock (Children)
                {
                    foreach (var c in Children)
                    {
                        if (c is not UIBoundableTransform uiComp)
                            continue;

                        UITransform? comp = uiComp.FindDeepestComponent(worldPoint, true);
                        if (comp != null)
                            return comp;
                    }
                }
            }
            catch
            {

            }
            finally
            {
                //_childLocker.ExitReadLock();
            }

            if (includeThis && Contains(worldPoint))
                return this;

            return null;
        }
        public List<UIBoundableTransform> FindAllIntersecting(Vector2 worldPoint, bool includeThis)
        {
            List<UIBoundableTransform> list = [];
            FindAllIntersecting(worldPoint, includeThis, list);
            return list;
        }
        public void FindAllIntersecting(Vector2 worldPoint, bool includeThis, List<UIBoundableTransform> results)
        {
            try
            {
                lock (Children)
                {
                    foreach (var c in Children)
                        if (c is UIBoundableTransform uiTfm)
                            uiTfm.FindAllIntersecting(worldPoint, true, results);
                }
            }
            catch// (Exception ex)
            {
                //Engine.LogException(ex);
            }
            finally
            {
                //_childLocker.ExitReadLock();
            }

            if (includeThis && Contains(worldPoint))
                results.Add(this);
        }

        protected override void OnChildAdded(TransformBase item)
        {
            base.OnChildAdded(item);

            //if (item is IRenderable c)
            //    c.RenderedObjects.LayerIndex = RenderInfo2D.LayerIndex;
        }

        public override float CalcAutoWidth()
            => Size.X.Clamp(MinSize.X, MaxSize.X);
        public override float CalcAutoHeight()
            => Size.Y.Clamp(MinSize.Y, MaxSize.Y);

        public override Vector2 ClosestPoint(Vector2 worldPoint)
            => ScreenToLocal(worldPoint).Clamp(-OriginTranslation, ActualSize - OriginTranslation);

        public override bool Contains(Vector2 worldPoint)
            => ActualSize.Contains(ScreenToLocal(worldPoint));

        /// <summary>
        /// Returns true if the given world point projected perpendicularly to the HUD as a 2D point is contained within this component and the Z value is within the given depth margin.
        /// </summary>
        /// <param name="worldPoint"></param>
        /// <param name="zMargin">How far away the point can be on either side of the HUD for it to be considered close enough.</param>
        /// <returns></returns>
        public bool Contains(Vector3 worldPoint, float zMargin = 0.5f)
        {
            Vector3 localPoint = Vector3.Transform(worldPoint, InverseWorldMatrix);
            return Math.Abs(localPoint.Z) < zMargin && ActualSize.Contains(localPoint.XY());
        }
    }
}
