using System.Collections;
using System.ComponentModel;
using System.Numerics;
using XREngine.Input.Devices;
using XREngine.Rendering.Commands;
using XREngine.Rendering.Info;

namespace XREngine.Rendering.UI
{
    public class UICanvasTransform : UIBoundableTransform
    {
        public UICanvasTransform()
        {
            //ScreenSpaceCamera = new OrthographicCamera(Vector3.One, Vector3.Zero, Rotator.GetZero(), Vector2.Zero, -0.5f, 0.5f);
            //ScreenSpaceCamera.SetOriginBottomLeft();
            //ScreenSpaceCamera.Resize(1, 1);

            ScreenSpaceUIScene = new XRWorldInstance();

            RenderInfo3D.IsVisible = false;
            RenderInfo2D.IsVisible = false;
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
                        RenderInfo3D.IsVisible = true;
                        RenderInfo2D.IsVisible = true;
                        break;
                    case ECanvasDrawSpace.Screen:
                        RenderInfo3D.IsVisible = false;
                        RenderInfo2D.IsVisible = false;
                        break;
                    case ECanvasDrawSpace.World:
                        RenderInfo3D.IsVisible = true;
                        RenderInfo2D.IsVisible = true;
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

        public XRCamera ScreenSpaceCamera { get; } = new XRCamera();
        public XRWorldInstance ScreenSpaceUIScene { get; }
        public RenderCommandCollection ScreenSpaceRenderPasses { get; set; } = new RenderCommandCollection();
        
        public Vector2 LastCursorPositionWorld { get; private set; }
        public Vector2 CursorPositionWorld { get; private set; }

        public bool PreRenderEnabled { get; }

        //protected override void OnResizeLayout(BoundingRectangleF parentRegion)
        //{
        //    ScreenSpaceUIScene?.Resize(parentRegion.Extents);
        //    ScreenSpaceCamera?.Resize(parentRegion.Width, parentRegion.Height);

        //    base.OnResizeLayout(parentRegion);
        //}

        //public void RenderScreenSpace(XRViewport viewport, QuadFrameBuffer fbo)
        //    => ScreenSpaceUIScene?.Render(ScreenSpaceRenderPasses, ScreenSpaceCamera, viewport, fbo);
        //public void UpdateScreenSpace()
        //    => ScreenSpaceUIScene?.PreRenderUpdate(ScreenSpaceRenderPasses, null, ScreenSpaceCamera);
        //public void SwapBuffersScreenSpace()
        //{
        //    ScreenSpaceUIScene?.GlobalSwap();
        //    ScreenSpaceRenderPasses?.SwapBuffers();
        //}

        protected internal override void RegisterInputs(InputInterface input)
        {
            input.RegisterMouseMove(MouseMove, EMouseMoveType.Absolute);
            input.RegisterMouseButtonEvent(EMouseButton.LeftClick, EButtonInputType.Pressed, OnClick);

            //input.RegisterAxisUpdate(GamePadAxis.LeftThumbstickX, OnLeftStickX, false, EInputPauseType.TickOnlyWhenPaused);
            //input.RegisterAxisUpdate(GamePadAxis.LeftThumbstickY, OnLeftStickY, false, EInputPauseType.TickOnlyWhenPaused);
            //input.RegisterButtonEvent(GamePadButton.DPadUp, ButtonInputType.Pressed, OnDPadUp, EInputPauseType.TickOnlyWhenPaused);
            //input.RegisterButtonEvent(GamePadButton.FaceDown, ButtonInputType.Pressed, OnGamepadSelect, InputPauseType.TickOnlyWhenPaused);
            //input.RegisterButtonEvent(GamePadButton.FaceRight, ButtonInputType.Pressed, OnBackInput, EInputPauseType.TickOnlyWhenPaused);

            base.RegisterInputs(input);
        }

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

        private class Comparer : IComparer<RenderInfo2D>, IComparer
        {
            public int Compare(RenderInfo2D? x, RenderInfo2D? y)
            {
                if (x is not RenderInfo2D left || 
                    y is not RenderInfo2D right)
                    return 0;

                if (left.LayerIndex > right.LayerIndex)
                    return -1;

                if (right.LayerIndex > left.LayerIndex)
                    return 1;

                if (left.IndexWithinLayer > right.IndexWithinLayer)
                    return -1;

                if (right.IndexWithinLayer > left.IndexWithinLayer)
                    return 1;

                return 0;
            }
            public int Compare(object x, object y)
                => Compare((IRenderable)x, (IRenderable)y);
        }
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

        private SortedSet<RenderInfo2D> LastInteractableIntersections = new(new Comparer());
        private SortedSet<RenderInfo2D> InteractableIntersections = new(new Comparer());

        protected bool InteractablePredicate(IRenderable item) => item is UIInteractableComponent;
        protected virtual void MouseMove(float x, float y)
        {
            //Vector2 newPos = GetCursorPositionWorld();
            //if (CursorPositionWorld.DistanceSquared(newPos) < 0.001f)
            //    return;

            //LastCursorPositionWorld = CursorPositionWorld;
            //CursorPositionWorld = newPos;

            //var tree = ScreenSpaceUIScene?.VisualScene;
            //if (tree is null)
            //    return;
            
            ////tree.FindAllIntersectingSorted(CursorPositionWorld, InteractableIntersections, InteractablePredicate);
            ////DeepestInteractable = InteractableIntersections.Min as UIInteractableComponent;

            ////LastInteractableIntersections.ForEach(ValidateIntersection);
            ////InteractableIntersections.ForEach(ValidateIntersection);

            //(LastInteractableIntersections, InteractableIntersections) = (InteractableIntersections, LastInteractableIntersections);
        }
        private void ValidateIntersection(IRenderable obj)
        {
            if (obj is not UIInteractableComponent inter)
                return;
            
            //if (LastInteractableIntersections.Contains(obj))
            //{
            //    //Mouse was over this renderable last update

            //    //if (!InteractableIntersections.Contains(obj))
            //    //{
            //    //    //Lost mouse over
            //    //    inter.IsMouseOver = false;
            //    //    inter.IsMouseDirectlyOver = false;
            //    //}
            //    //else
            //    //{
            //    //    //Had mouse over and still does now
            //    //    //inter.MouseMove(
            //    //    //    inter.ScreenToLocal(LastCursorPositionWorld),
            //    //    //    inter.ScreenToLocal(CursorPositionWorld));
            //    //}
            //}
            //else
            //{
            //    //Mouse was not over this renderable last update

            //    //if (InteractableIntersections.Contains(obj))
            //    //{
            //    //    Got mouse over
            //    //    inter.IsMouseOver = true;
            //    //    inter.IsMouseDirectlyOver = obj == DeepestInteractable;
            //    //}
            //}
        }
        private void OnClick() => FocusedComponent = DeepestInteractable;
        //private Vector2 GetCursorPositionWorld()
        //{
        //    LocalPlayerController controller = pawn is UserInterfaceInputComponent ui && ui.OwningPawn is PawnComponent uiOwner
        //        ? uiOwner.LocalPlayerController ?? pawn.LocalPlayerController
        //        : OwningUserInterface.LocalPlayerController;

        //    XRViewport? v = controller?.Viewport;
        //    return v?.ScreenToWorld(v.CursorPosition, 0.0f).XY() ?? Vector2.Zero;
        //}
    }
}
