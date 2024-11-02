using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Vectors;
using XREngine.Rendering;
using XREngine.Rendering.UI;

namespace XREngine.Components
{
    /// <summary>
    /// Renders a 2D canvas on top of the screen, in world space, or in camera space.
    /// </summary>
    [RequireComponents(typeof(CameraComponent))]
    [RequiresTransform(typeof(UICanvasTransform))]
    public class UICanvasComponent : XRComponent
    {
        /// <summary>
        /// Gets the user input component. Not a necessary component, so may be null.
        /// </summary>
        /// <returns></returns>
        public UIInputComponent? GetInputComponent() => GetSiblingComponent<UIInputComponent>();

        public UICanvasTransform CanvasTransform => TransformAs<UICanvasTransform>();
        public CameraComponent ScreenSpaceCamera => GetSiblingComponent<CameraComponent>(true)!;

        public XRWorldInstance ScreenSpaceWorld { get; } = new XRWorldInstance();

        public UICanvasComponent()
        {
            ScreenSpaceCamera.Camera.Parameters = new XROrthographicCameraParameters(1.0f, 1.0f, -0.5f, 0.5f);
        }

        private UIInteractableComponent? _focusedComponent;

        private ECanvasDrawSpace _drawSpace = ECanvasDrawSpace.Screen;
        public ECanvasDrawSpace DrawSpace
        {
            get => _drawSpace;
            set
            {
                if (!SetField(ref _drawSpace, value))
                    return;

                switch (_drawSpace)
                {
                    case ECanvasDrawSpace.Camera:
                        //RenderInfo.IsVisible = true;
                        break;
                    case ECanvasDrawSpace.Screen:
                        //RenderInfo.IsVisible = false;
                        break;
                    case ECanvasDrawSpace.World:
                        //RenderInfo.IsVisible = true;
                        break;
                }
            }
        }

        private float _cameraDrawSpaceDistance = 0.1f;
        public float CameraDrawSpaceDistance
        {
            get => _cameraDrawSpaceDistance;
            set => SetField(ref _cameraDrawSpaceDistance, value);
        }

        public Vector2 LastCursorPositionWorld { get; private set; }
        public Vector2 CursorPositionWorld { get; private set; }
        public bool PreRenderEnabled { get; }

        //protected override void OnResizeLayout(BoundingRectangleF parentRegion)
        //{
        //    //Debug.Out($"UI CANVAS {Name} : {parentRegion.Width.Rounded(2)} x {parentRegion.Height.Rounded(2)}");

        //    //ScreenSpaceUIScene?.Resize(parentRegion.Extents);
        //    //ScreenSpaceCamera?.Resize(parentRegion.Width, parentRegion.Height);
        //    //base.OnResizeLayout(parentRegion);
        //}

        public void RenderScreenSpace(XRViewport viewport, XRQuadFrameBuffer fbo)
        {
            //ScreenSpaceWorld?.VisualScene.Render(viewport, ScreenSpaceCamera, fbo);
        }
        public void UpdateScreenSpace()
        {
            //ScreenSpaceWorld?.VisualScene.PreRenderUpdate(null, ScreenSpaceCamera);
        }
        public void SwapBuffersScreenSpace()
        {
            //ScreenSpaceWorld?.VisualScene.GlobalSwap();
            //ScreenSpaceRenderPasses?.SwapBuffers();
        }

        internal List<IRenderable> FindAllIntersecting(Vector2 viewportPoint) => throw new NotImplementedException();

        //protected internal override void RegisterInputs(InputInterface input)
        //{
        //    input.RegisterMouseMove(MouseMove, EMouseMoveType.Absolute);
        //    input.RegisterButtonEvent(EMouseButton.LeftClick, EButtonInputType.Pressed, OnClick);

        //    //input.RegisterAxisUpdate(GamePadAxis.LeftThumbstickX, OnLeftStickX, false, EInputPauseType.TickOnlyWhenPaused);
        //    //input.RegisterAxisUpdate(GamePadAxis.LeftThumbstickY, OnLeftStickY, false, EInputPauseType.TickOnlyWhenPaused);
        //    //input.RegisterButtonEvent(GamePadButton.DPadUp, ButtonInputType.Pressed, OnDPadUp, EInputPauseType.TickOnlyWhenPaused);
        //    //input.RegisterButtonEvent(GamePadButton.FaceDown, ButtonInputType.Pressed, OnGamepadSelect, InputPauseType.TickOnlyWhenPaused);
        //    //input.RegisterButtonEvent(GamePadButton.FaceRight, ButtonInputType.Pressed, OnBackInput, EInputPauseType.TickOnlyWhenPaused);

        //    base.RegisterInputs(input);
        //}

        protected virtual void OnLeftStickX(float value) { }
        protected virtual void OnLeftStickY(float value) { }

        /// <summary>
        /// Called on either left click or A button.
        /// Default behavior will OnClick the currently focused/highlighted UI component, if anything.
        /// </summary>
        //protected virtual void OnSelectInput()
        //{
        //_focusedComponent?.OnSelect();
        //}
        protected virtual void OnScrolledInput(bool up)
        {
            //_focusedComponent?.OnScrolled(up);
        }
        protected virtual void OnBackInput()
        {
            //_focusedComponent?.OnBack();
        }
        protected virtual void OnDPadUp()
        {

        }

        //private class Comparer : IComparer<RenderInfo2D>
        //{
        //    public int Compare(IRenderable? x, IRenderable? y)
        //    {
        //        RenderInfo2D? left = x?.RenderInfo;
        //        RenderInfo2D? right = y?.RenderInfo;

        //        if (left is null)
        //        {
        //            if (right is null)
        //                return 0;
        //            else
        //                return -1;
        //        }
        //        else if (right is null)
        //            return 1;

        //        if (left.LayerIndex > right.LayerIndex)
        //            return -1;

        //        if (right.LayerIndex > left.LayerIndex)
        //            return 1;

        //        if (left.IndexWithinLayer > right.IndexWithinLayer)
        //            return -1;

        //        if (right.IndexWithinLayer > left.IndexWithinLayer)
        //            return 1;

        //        return 0;
        //    }
        //    public int Compare(object x, object y)
        //        => Compare((IRenderable)x, (IRenderable)y);
        //}
        public UIInteractableComponent? FocusedComponent
        {
            get => _focusedComponent;
            set
            {
                if (_focusedComponent != null)
                    _focusedComponent.IsFocused = false;

                _focusedComponent = value;

                if (_focusedComponent != null)
                    _focusedComponent.IsFocused = true;
            }
        }

        public UIInteractableComponent? DeepestInteractable { get; private set; }

        //private SortedSet<IRenderable> LastInteractableIntersections = new(new Comparer());
        //private SortedSet<IRenderable> InteractableIntersections = new(new Comparer());

        protected bool InteractablePredicate(IRenderable item) => item is UIInteractableComponent;
        protected virtual void MouseMove(float x, float y)
        {
            Vector2 newPos = GetCursorPositionWorld();
            if (Vector2.DistanceSquared(CursorPositionWorld, newPos) < 0.001f)
                return;

            LastCursorPositionWorld = CursorPositionWorld;
            CursorPositionWorld = newPos;

            var tree = ScreenSpaceWorld.VisualScene.RenderablesTree;
            if (tree is null)
                return;

            //tree.FindAllIntersectingSorted(CursorPositionWorld, InteractableIntersections, InteractablePredicate);
            //DeepestInteractable = InteractableIntersections.Min as UIInteractableComponent;

            //LastInteractableIntersections.ForEach(ValidateIntersection);
            //InteractableIntersections.ForEach(ValidateIntersection);

            //(LastInteractableIntersections, InteractableIntersections) = (InteractableIntersections, LastInteractableIntersections);
        }
        private void ValidateIntersection(IRenderable obj)
        {
            if (obj is not UIInteractableComponent inter)
                return;

            //if (LastInteractableIntersections.Contains(obj))
            //{
            //    //Mouse was over this renderable last update

            //    if (!InteractableIntersections.Contains(obj))
            //    {
            //        //Lost mouse over
            //        inter.IsMouseOver = false;
            //        inter.IsMouseDirectlyOver = false;
            //    }
            //    else
            //    {
            //        //Had mouse over and still does now
            //        //inter.MouseMove(
            //        //    inter.ScreenToLocal(LastCursorPositionWorld),
            //        //    inter.ScreenToLocal(CursorPositionWorld));
            //    }
            //}
            //else
            //{
            //    //Mouse was not over this renderable last update

            //    if (InteractableIntersections.Contains(obj))
            //    {
            //        //Got mouse over
            //        inter.IsMouseOver = true;
            //        inter.IsMouseDirectlyOver = obj == DeepestInteractable;
            //    }
            //}
        }
        private void OnClick() => FocusedComponent = DeepestInteractable;
        private Vector2 GetCursorPositionWorld()
        {
            //if (!(OwningActor is IPawn pawn))
            //    return Vector2.Zero;

            //LocalPlayerController controller = pawn is IUserInterfacePawn ui && ui.OwningPawn is IPawn uiOwner
            //    ? uiOwner.LocalPlayerController ?? pawn.LocalPlayerController
            //    : pawn.LocalPlayerController;

            //XRViewport v = controller?.Viewport;
            //return v?.ScreenToWorld(v.CursorPosition).Xy ?? Vector2.Zero;
            return Vector2.Zero;
        }

        internal void InvalidateLayout()
        {

        }

        internal UIComponent? FindDeepestComponent(Vector2 viewportPoint)
        {
            return null;
        }

        public void Resize(IVector2 extents)
        {

        }

        public void CollectVisible(XRViewport viewport)
        {
            var scene = ScreenSpaceWorld?.VisualScene;
            //scene?.CollectRenderedItems(commands, null, CameraComponent?.UserInterfaceOverlay?.ScreenSpaceCamera);
        }
    }
}
