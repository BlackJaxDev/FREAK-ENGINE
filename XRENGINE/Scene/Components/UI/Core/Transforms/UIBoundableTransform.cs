using Extensions;
using System.Numerics;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    /// <summary>
    /// Represents a UI component with area that can be aligned within its parent.
    /// </summary>
    public class UIBoundableTransform : UITransform
    {
        public UIBoundableTransform() : base(null)
        {
            _normalizedPivot = Vector2.Zero;
            _width = 0.0f;
            _height = 0.0f;
            _minHeight = null;
            _minWidth = null;
            _maxHeight = null;
            _maxWidth = null;
            _margins = Vector4.Zero;
            _padding = Vector4.Zero;
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

        private float? _width;
        /// <summary>
        /// The requested width of this component before layouting.
        /// </summary>
        public float? Width
        {
            get => _width;
            set => SetField(ref _width, value);
        }

        private float? _height;
        /// <summary>
        /// The requested height of this component before layouting.
        /// </summary>
        public float? Height
        {
            get => _height;
            set => SetField(ref _height, value);
        }

        private float? _minHeight, _minWidth, _maxHeight, _maxWidth;

        public float? MaxHeight
        {
            get => _maxHeight;
            set => SetField(ref _maxHeight, value);
        }
        public float? MaxWidth
        {
            get => _maxWidth;
            set => SetField(ref _maxWidth, value);
        }
        public float? MinHeight
        {
            get => _minHeight;
            set => SetField(ref _minHeight, value);
        }
        public float? MinWidth
        {
            get => _minWidth;
            set => SetField(ref _minWidth, value);
        }

        private Vector2 _normalizedPivot = Vector2.Zero;
        /// <summary>
        /// The origin of this component as a percentage of its size.
        /// </summary>
        public Vector2 NormalizedPivot
        {
            get => _normalizedPivot;
            set => SetField(ref _normalizedPivot, value);
        }
        /// <summary>
        /// This is the origin of the component after layouting.
        /// </summary>
        public Vector2 LocalPivotTranslation
        {
            get => NormalizedPivot * ActualSize;
            set
            {
                float x = ActualSize.X.IsZero() ? 0.0f : value.X / ActualSize.X;
                float y = ActualSize.Y.IsZero() ? 0.0f : value.Y / ActualSize.Y;
                NormalizedPivot = new(x, y);
            }
        }
        public float PivotTranslationX
        {
            get => NormalizedPivot.X * ActualWidth;
            set => NormalizedPivot = new Vector2(ActualWidth.IsZero() ? 0.0f : value / ActualWidth, NormalizedPivot.Y);
        }
        public float PivotTranslationY
        {
            get => NormalizedPivot.Y * ActualHeight;
            set => NormalizedPivot = new Vector2(NormalizedPivot.X, ActualHeight.IsZero() ? 0.0f : value / ActualHeight);
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

        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(Margins):
                case nameof(Padding):
                case nameof(Width):
                case nameof(Height):
                case nameof(MinHeight):
                case nameof(MinWidth):
                case nameof(MaxHeight):
                case nameof(MaxWidth):
                case nameof(NormalizedPivot):
                    InvalidateLayout();
                    break;
            }
        }

        /// <summary>
        /// Creates the local transformation of the origin relative to the parent UI transform.
        /// Translates to the parent's translation, applies the origin translation, and then applies this component's translation and scale.
        /// </summary>
        /// <returns></returns>
        protected override Matrix4x4 CreateLocalMatrix()
            => base.CreateLocalMatrix() * Matrix4x4.CreateTranslation(-PivotTranslationX, -PivotTranslationY, 0.0f);

        private Vector2 _normMinAnchor = Vector2.Zero;
        public Vector2 MinAnchor
        {
            get => _normMinAnchor;
            set => SetField(ref _normMinAnchor, value);
        }

        private Vector2 _normMaxAnchor = Vector2.One;
        public Vector2 MaxAnchor
        {
            get => _normMaxAnchor;
            set => SetField(ref _normMaxAnchor, value);
        }

        internal void ChildSizeChanged()
        {
            //Invalidate this component's layout if the size of a child changes and width or height uses auto sizing.
            if (!Width.HasValue || !Height.HasValue)
                InvalidateLayout();
        }

        /// <summary>
        /// This method sets ActualSize and ActualTranslation based on a variety of factors when fitting the component into the parent bounds.
        /// </summary>
        /// <param name="parentBounds"></param>
        protected override void OnResizeActual(BoundingRectangleF parentBounds)
        {
            GetActualBounds(parentBounds, out Vector2 trans, out Vector2 size);
            ActualSize = size;
            ActualTranslation = trans;
        }

        /// <summary>
        /// This method calculates the actual size and translation of the component.
        /// </summary>
        /// <param name="parentBounds"></param>
        /// <param name="trans"></param>
        /// <param name="size"></param>
        protected virtual void GetActualBounds(BoundingRectangleF parentBounds, out Vector2 trans, out Vector2 size)
        {
            GetAnchors(parentBounds, out float minX, out float minY, out float maxX, out float maxY);

            //Translate the component to the min anchor position and then add the translation.
            trans = new(minX + Translation.X, minY + Translation.Y);

            //Calculate the size of the component based on the anchors.
            size = CalcSize(maxX, maxY, trans);

            //Clamp the size to the min and max size.
            ClampSize(ref size);
        }

        /// <summary>
        /// Returns Width / Height
        /// </summary>
        /// <returns></returns>
        public float GetAspect()
            => GetWidth() / GetHeight();
        /// <summary>
        /// Returns the width of the component.
        /// If Width is null, this will calculate the width based on the size of child components.
        /// </summary>
        /// <returns></returns>
        public float GetWidth()
            => Width ?? CalcAutoWidth();
        /// <summary>
        /// Returns the height of the component.
        /// If Height is null, this will calculate the height based on the size of child components.
        /// </summary>
        /// <returns></returns>
        public float GetHeight()
            => Height ?? CalcAutoHeight();

        private Vector2 CalcSize(float maxX, float maxY, Vector2 trans)
        {
            Vector2 size;
            if (XRMath.Approx(MaxAnchor.X, MinAnchor.X))
                size.X = GetWidth();
            else
            {
                //If the min anchor is less than the max anchor, then we should set size too
                //Size.X becomes the offset from the anchor position.
                size.X = maxX + (Width ?? 0.0f) - trans.X;
            }
            if (XRMath.Approx(MaxAnchor.Y, MinAnchor.Y))
                size.Y = GetHeight();
            else
            {
                //If the min anchor is less than the max anchor, then we should set size too
                //Size.Y becomes the offset from the anchor position.
                size.Y = maxY + (Height ?? 0.0f) - trans.Y;
            }
            return size;
        }

        private void ClampSize(ref Vector2 size)
        {
            if (MinWidth.HasValue)
                size.X = Math.Max(size.X, MinWidth.Value);
            if (MinHeight.HasValue)
                size.Y = Math.Max(size.Y, MinHeight.Value);
            if (MaxWidth.HasValue)
                size.X = Math.Min(size.X, MaxWidth.Value);
            if (MaxHeight.HasValue)
                size.Y = Math.Min(size.Y, MaxHeight.Value);
        }

        private void GetAnchors(BoundingRectangleF parentBounds, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = parentBounds.Width * MinAnchor.X;
            maxX = parentBounds.Width * MaxAnchor.X;

            minY = parentBounds.Height * MinAnchor.Y;
            maxY = parentBounds.Height * MaxAnchor.Y;
        }

        public BoundingRectangleF GetActualBounds()
            => new(_actualTranslation, _actualSize);

        /// <summary>
        /// This method is called to fit the contents of this transform into the provided bounds.
        /// </summary>
        /// <param name="parentBounds"></param>
        public override void FitLayout(BoundingRectangleF parentBounds)
        {
            OnResizeActual(ApplyMargins(parentBounds));
            MarkLocalModified();
        }

        protected override void OnLocalMatrixChanged()
        {
            base.OnLocalMatrixChanged();
            OnResizeChildComponents(ApplyPadding(GetActualBounds()));
            RemakeAxisAlignedRegion();
        }

        private BoundingRectangleF ApplyPadding(BoundingRectangleF bounds)
        {
            var padding = Padding;
            float left = padding.X;
            float bottom = padding.Y;
            float right = padding.Z;
            float top = padding.W;

            Vector2 size = bounds.Extents;
            Vector2 pos = bounds.Translation;

            pos += new Vector2(left, bottom);
            size -= new Vector2(left + right, bottom + top);
            bounds = new BoundingRectangleF(pos, size);
            return bounds;
        }

        private BoundingRectangleF ApplyMargins(BoundingRectangleF bounds)
        {
            var margins = Margins;
            float left = margins.X;
            float bottom = margins.Y;
            float right = margins.Z;
            float top = margins.W;

            Vector2 size = bounds.Extents;
            Vector2 pos = bounds.Translation;

            pos += new Vector2(left, bottom);
            size -= new Vector2(left + right, bottom + top);
            bounds = new BoundingRectangleF(pos, size);
            return bounds;
        }

        protected virtual void RemakeAxisAlignedRegion()
        {
            Matrix4x4 mtx = Matrix4x4.CreateScale(ActualSize.X, ActualSize.Y, 1.0f) * WorldMatrix;

            Vector3 minPos = Vector3.Transform(Vector3.Zero, mtx);
            Vector3 maxPos = Vector3.Transform(new Vector3(Vector2.One, 0.0f), mtx);

            // Make sure min is the smallest and max is the largest in case of rotation.
            Vector2 min = new(Math.Min(minPos.X, maxPos.X), Math.Min(minPos.Y, maxPos.Y));
            Vector2 max = new(Math.Max(minPos.X, maxPos.X), Math.Max(minPos.Y, maxPos.Y));

            DebugRenderInfo2D.CullingVolume = BoundingRectangleF.FromMinMaxSides(min.X, max.X, min.Y, max.Y, 0.0f, 0.0f);
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
            catch (Exception ex)
            {
                Debug.LogException(ex);
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

        /// <summary>
        /// Calculates the width of the component based the widths of its children.
        /// </summary>
        /// <returns></returns>
        public override float CalcAutoWidth()
            => 0.0f;

        /// <summary>
        /// Calculates the height of the component based the heights of its children.
        /// </summary>
        /// <returns></returns>
        public override float CalcAutoHeight()
            => 0.0f;

        public override Vector2 ClosestPoint(Vector2 worldPoint)
            => ScreenToLocal(worldPoint).Clamp(-LocalPivotTranslation, ActualSize - LocalPivotTranslation);

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
            Vector3 localPoint = WorldToLocal(worldPoint);
            return Math.Abs(localPoint.Z) < zMargin && ActualSize.Contains(localPoint.XY());
        }

        public Vector2 WorldToLocal(Vector2 worldPoint)
        {
            return Vector2.Transform(worldPoint, InverseWorldMatrix);
        }
        public Vector2 LocalToWorld(Vector2 localPoint)
        {
            return Vector2.Transform(localPoint, WorldMatrix);
        }
        public Vector3 WorldToLocal(Vector2 worldPoint, float worldZ)
        {
            return Vector3.Transform(new Vector3(worldPoint, worldZ), InverseWorldMatrix);
        }
        public Vector3 LocalToWorld(Vector2 localPoint, float worldZ)
        {
            return Vector3.Transform(new Vector3(localPoint, worldZ), WorldMatrix);
        }
        public Vector3 WorldToLocal(Vector3 worldPoint)
        {
            return Vector3.Transform(worldPoint, InverseWorldMatrix);
        }
        public Vector3 LocalToWorld(Vector3 localPoint)
        {
            return Vector3.Transform(localPoint, WorldMatrix);
        }
    }
}
