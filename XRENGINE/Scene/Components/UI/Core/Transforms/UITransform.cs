using System.Drawing;
using System.Numerics;
using XREngine.Components;
using XREngine.Data.Colors;
using XREngine.Data.Geometry;
using XREngine.Input.Devices;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;
using XREngine.Scene;
using XREngine.Scene.Transforms;

namespace XREngine.Rendering.UI
{
    public class UITransform : TransformBase
    {
        protected Vector2 _translation = Vector2.Zero;
        protected float _z = 0.0f;
        protected Vector3 _scale = Vector3.One;
        private UIInputComponent? _owningUserInterface;
        public RenderCommandMethod2D _rc;
        private bool _renderTransformation = true;
        private UIChildPlacementInfo? _placementInfo = null;

        public UITransform() : this(null) { }
        public UITransform(TransformBase? parent) : base(parent)
        {
            _rc = new RenderCommandMethod2D(0, RenderVisualGuides);
            Children.PostAnythingAdded += OnChildAdded;
            Children.PostAnythingRemoved += OnChildRemoved;
        }
        ~UITransform()
        {
            Children.PostAnythingAdded -= OnChildAdded;
            Children.PostAnythingRemoved -= OnChildRemoved;
        }

        public override SceneNode? SceneNode
        {
            get => base.SceneNode;
            set
            {
                base.SceneNode = value;
                OwningUserInterface = value is not null && value.TryGetComponent<UIInputComponent>(out var ui) ? ui : null;
            }
        }
        public UIInputComponent? OwningUserInterface
        {
            get => _owningUserInterface;
            set
            {
                _owningUserInterface = value;
                foreach (var child in Children)
                    if (child is UITransform uiTransform)
                        uiTransform.OwningUserInterface = value;
            }
        }
        public override TransformBase? Parent
        {
            get => base.Parent;
            set
            {
                base.Parent = value;
                OwningUserInterface = Parent is UITransform uiTfm ? uiTfm.OwningUserInterface : null;
            }
        }

        public virtual Vector2 Translation
        {
            get => _translation;
            set
            {
                SetField(ref _translation, value);
                MarkLocalModified();
            }
        }

        /// <summary>
        /// This is the translation after being potentially modified by the parent's placement info.
        /// </summary>
        public Vector2 ActualTranslation
        {
            get => _actualTranslation;
            set => SetField(ref _actualTranslation, value);
        }

        public virtual float DepthTranslation
        {
            get => _z;
            set
            {
                SetField(ref _z, value);
                MarkLocalModified();
            }
        }

        public virtual Vector3 Scale
        {
            get => _scale;
            set
            {
                SetField(ref _scale, value);
                MarkLocalModified();
            }
        }

        protected override Matrix4x4 CreateLocalMatrix()
            => Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(new Vector3(Translation, DepthTranslation));

        /// <summary>
        /// Scale and translate in/out to/from a specific point.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="worldScreenPoint"></param>
        /// <param name="minScale"></param>
        /// <param name="maxScale"></param>
        public void Zoom(float amount, Vector2 worldScreenPoint, Vector2? minScale, Vector2? maxScale)
        {
            if (Math.Abs(amount) < 0.0001f)
                return;

            Vector2 scale = new(_scale.X, _scale.Y);
            Vector2 multiplier = Vector2.One / scale * amount;
            Vector2 newScale = scale - new Vector2(amount);

            if (minScale != null)
            {
                if (newScale.X < minScale.Value.X)
                    newScale.X = minScale.Value.X;

                if (newScale.Y < minScale.Value.Y)
                    newScale.Y = minScale.Value.Y;
            }

            if (maxScale != null)
            {
                if (newScale.X > maxScale.Value.X)
                    newScale.X = maxScale.Value.X;

                if (newScale.Y > maxScale.Value.Y)
                    newScale.Y = maxScale.Value.Y;
            }

            if (Vector2.Distance(scale, newScale) < 0.0001f)
                return;
            
            Vector2 worldPoint = new(WorldTranslation.X, WorldTranslation.Y);
            Vector2 offset = (worldScreenPoint - worldPoint) * multiplier;
            _translation = Translation + offset;
            _scale = new Vector3(newScale, _scale.Z);
            MarkLocalModified();
            //InvalidateLayout();
        }

        public void InvalidateLayout()
            => OwningUserInterface?.InvalidateLayout();

        /// <summary>
        /// Fits the layout of this UI transform to the parent region.
        /// </summary>
        /// <param name="parentRegion"></param>
        public virtual void FitLayout(BoundingRectangleF parentRegion)
        {

        }

        protected EVisibility _visibility = EVisibility.Collapsed;
        private Vector2 _actualTranslation = new();

        public RenderInfo2D RenderInfo2D { get; private set; }
        public RenderInfo3D RenderInfo3D { get; private set; }

        public bool IsVisible
        {
            get => Visibility == EVisibility.Visible;
            set
            {
                if (value)
                    Visibility = EVisibility.Visible;
                else if (CollapseOnHide)
                    Visibility = EVisibility.Collapsed;
                else
                    Visibility = EVisibility.Hidden;
            }
        }

        public bool CollapseOnHide { get; set; } = true;

        public virtual EVisibility Visibility
        {
            get => _visibility;
            set => SetField(ref _visibility, value, null, VisibilityChanged);
        }

        private void VisibilityChanged(EVisibility v)
        {
            RenderInfo2D.IsVisible = IsVisible;
            RenderInfo3D.IsVisible = IsVisible;
        }

        public bool RenderTransformation
        {
            get => _renderTransformation;
            set => SetField(ref _renderTransformation, value);
        }

        /// <summary>
        /// Dictates how this UI component is arranged within the parent transform's bounds.
        /// </summary>
        public UIChildPlacementInfo? PlacementInfo
        {
            get
            {
                Parent?.VerifyPlacementInfo(this);
                return _placementInfo;
            }
            set => _placementInfo = value;
        }

        /// <summary>
        /// Recursively registers (or unregisters) inputs on this and all child UI components.
        /// </summary>
        /// <param name="input"></param>
        internal protected virtual void RegisterInputs(InputInterface input)
        {
            //try
            //{
            //    foreach (ISceneComponent comp in ChildComponents)
            //        if (comp is IUIComponent uiComp)
            //            uiComp.RegisterInputs(input);
            //}
            //catch (Exception ex) 
            //{
            //    Engine.LogException(ex);
            //}
        }
        protected internal override void Start()
        {
            if (this is IRenderable r)
                OwningUserInterface?.AddRenderableComponent(r);
        }
        protected internal override void Stop()
        {
            if (this is IRenderable r)
                OwningUserInterface?.RemoveRenderableComponent(r);
        }

        protected virtual void OnResizeChildComponents(BoundingRectangleF parentRegion)
        {
            try
            {
                //_childLocker.EnterReadLock();
                foreach (var c in Children)
                    if (c is UITransform uiTfm)
                        uiTfm.FitLayout(parentRegion);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                //_childLocker.ExitReadLock();
            }
        }

        /// <summary>
        /// Converts a local-space coordinate of a parent UI component 
        /// to a local-space coordinate of a child UI component.
        /// </summary>
        /// <param name="coordinate">The coordinate relative to the parent UI component.</param>
        /// <param name="parent">The parent UI component whose space the coordinate is already in.</param>
        /// <param name="targetChild">The UI component whose space you wish to convert the coordinate to.</param>
        /// <returns></returns>
        public static Vector2 ConvertUICoordinate(Vector2 coordinate, UITransform parent, UITransform targetChild)
            => Vector2.Transform(coordinate, targetChild.InverseWorldMatrix * parent.WorldMatrix);
        /// <summary>
        /// Converts a screen-space coordinate
        /// to a local-space coordinate of a UI component.
        /// </summary>
        /// <param name="coordinate">The coordinate relative to the screen / origin of the root UI component.</param>
        /// <param name="uiComp">The UI component whose space you wish to convert the coordinate to.</param>
        /// <param name="delta">If true, the coordinate and returned value are treated like a vector offset instead of an absolute point.</param>
        /// <returns></returns>
        public Vector2 ScreenToLocal(Vector2 coordinate)
            => Vector2.Transform(coordinate, OwningUserInterface?.Transform.InverseWorldMatrix ?? Matrix4x4.Identity);
        public Vector3 ScreenToLocal(Vector3 coordinate)
            => Vector3.Transform(coordinate, OwningUserInterface?.Transform.InverseWorldMatrix ?? Matrix4x4.Identity);
        public Vector3 LocalToScreen(Vector3 coordinate)
            => Vector3.Transform(coordinate, OwningUserInterface?.Transform.WorldMatrix ?? Matrix4x4.Identity);
        public Vector2 LocalToScreen(Vector2 coordinate)
            => Vector2.Transform(coordinate, OwningUserInterface?.Transform.WorldMatrix ?? Matrix4x4.Identity);

        public virtual void AddRenderables(RenderCommandCollection passes, XRCamera camera)
        {
            //#if EDITOR
            //if (!Engine.EditorState.InEditMode)
            //    return;
            //#endif

            if (!RenderTransformation)
                return;

            passes.Add(_rc);
        }

        /// <summary>
        /// Helper method for rendering transforms, bounds, rotations, etc in the editor.
        /// </summary>
        protected virtual void RenderVisualGuides()
        {
            Vector3 startPoint = (Parent?.WorldMatrix.Translation ?? Vector3.Zero) + Engine.Rendering.Debug.UIPositionBias;
            Vector3 endPoint = WorldTranslation + Engine.Rendering.Debug.UIPositionBias;

            Engine.Rendering.Debug.RenderLine(startPoint, endPoint, ColorF4.White);
            Engine.Rendering.Debug.RenderPoint(endPoint, ColorF4.White);

            //Vector3 scale = WorldMatrix.Scale;
            Vector3 up = WorldUp * 50.0f;
            Vector3 right = WorldRight * 50.0f;

            Engine.Rendering.Debug.RenderLine(endPoint, endPoint + up, Color.Green);
            Engine.Rendering.Debug.RenderLine(endPoint, endPoint + right, Color.Red);
        }

        public virtual float CalcAutoWidth() => 0.0f;
        public virtual float CalcAutoHeight() => 0.0f;

        public virtual bool Contains(Vector2 worldPoint)
        {
            var worldTranslation = WorldTranslation;
            return Vector2.Distance(worldPoint, new Vector2(worldTranslation.X, worldTranslation.Y)) < 0.0001f;
        }
        public virtual Vector2 ClosestPoint(Vector2 worldPoint)
        {
            var worldTranslation = WorldTranslation;
            return new Vector2(worldTranslation.X, worldTranslation.Y);
        }

        protected virtual void OnChildAdded(TransformBase item)
        {
            //if (item is IRenderable c && c.RenderedObjects is RenderInfo2D r2D)
            //{
            //    r2D.LayerIndex = RenderInfo2D.LayerIndex;
            //    r2D.IndexWithinLayer = RenderInfo2D.IndexWithinLayer + 1;
            //}

            if (item is UITransform uic)
                uic.InvalidateLayout();
        }
        protected virtual void OnChildRemoved(TransformBase item)
        {

        }

        protected virtual void OnResizeActual(BoundingRectangleF parentBounds)
        {
            ActualTranslation = Translation;
        }
    }
}
