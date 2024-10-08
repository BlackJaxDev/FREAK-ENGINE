using System.ComponentModel;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Input.Devices;
using XREngine.Rendering.UI;

namespace XREngine.Components
{
    /// <summary>
    /// Dictates input for a UI canvas component.
    /// </summary>
    [RequireComponents(typeof(UICanvasComponent))]
    public class UIInputComponent : PawnComponent
    {
        protected Vector2 _cursorPos = Vector2.Zero;
        private PawnComponent? _owningPawn;

        public UICanvasComponent? Canvas
        {
            get => _canvas;
            set => SetField(ref _canvas, value);
        }

        public bool IsResizing
        {
            get => _isResizing;
            private set => SetField(ref _isResizing, value);
        }

        public UIComponent? FocusedComponent { get; set; }

        /// <summary>
        /// The pawn that has this HUD linked for screen space use.
        /// </summary>
        public PawnComponent? OwningPawn
        {
            get => _owningPawn;
            set
            {
                UnlinkOwningPawn();
                _owningPawn = value;
                LinkOwningPawn();
            }
        }

        private void LinkOwningPawn()
        {
            if (_owningPawn is null)
                return;

            //if (_owningPawn.IsSpawned)
            //    Spawned(_owningPawn.OwningWorld);

            if (_owningPawn != this && _owningPawn.LocalPlayerController != null)
            {
                //Link input commands from the owning controller to this hud
                //TODO: add register input method only for this pawn
                var input = _owningPawn.LocalPlayerController.Input;
                input.TryUnregisterInput();
                input.InputRegistration += RegisterInput;
                input.TryRegisterInput();
            }
        }

        private void UnlinkOwningPawn()
        {
            if (_owningPawn is null)
                return;

            //if (_owningPawn.IsSpawned)
            //    Despawned();

            if (_owningPawn != this && _owningPawn.LocalPlayerController != null)
            {
                //Unlink input commands from the owning controller to this hud
                //TODO: add unregister input method only for this pawn
                var input = _owningPawn.LocalPlayerController.Input;
                input.TryUnregisterInput();
                input.InputRegistration -= RegisterInput;
                input.TryRegisterInput();
            }
        }

        public override void RegisterInput(InputInterface input)
        {
            //Canvas.RegisterInputs(input);

            input.RegisterMouseMove(MouseMove, EMouseMoveType.Absolute);
            //input.RegisterButtonEvent(EMouseButton.LeftClick, ButtonInputType.Pressed, OnLeftClickSelect, InputPauseType.TickOnlyWhenPaused);

            //input.RegisterAxisUpdate(GamePadAxis.LeftThumbstickX, OnLeftStickX, false, EInputPauseType.TickOnlyWhenPaused);
            //input.RegisterAxisUpdate(GamePadAxis.LeftThumbstickY, OnLeftStickY, false, EInputPauseType.TickOnlyWhenPaused);
            //input.RegisterButtonEvent(GamePadButton.DPadUp, ButtonInputType.Pressed, OnDPadUp, EInputPauseType.TickOnlyWhenPaused);
            //input.RegisterButtonEvent(GamePadButton.FaceDown, ButtonInputType.Pressed, OnGamepadSelect, InputPauseType.TickOnlyWhenPaused);
            //input.RegisterButtonEvent(GamePadButton.FaceRight, ButtonInputType.Pressed, OnBackInput, EInputPauseType.TickOnlyWhenPaused);
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

        protected virtual void MouseMove(float x, float y)
        {
            //_cursorPos = CursorPositionWorld();


        }

        //public List<IRenderable> FindAllComponentsIntersecting(Vector2 viewportPoint)
        //    => Canvas?.FindAllIntersecting(viewportPoint);

        //public UIComponent FindDeepestComponent(Vector2 viewportPoint)
        //    => Canvas?.FindDeepestComponent(viewportPoint, false);

        //UIComponent current = null;
        ////Larger z-indices means the component is closer
        //foreach (UIComponent comp in results)
        //    if (current is null || comp.LayerIndex >= current.LayerIndex)
        //        current = comp;
        //return current;
        //return RootComponent.FindComponent(viewportPoint);

        public virtual void Resize(Vector2 bounds)
        {
            //Bounds = bounds;
            Canvas?.InvalidateLayout();
        }
        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            Canvas?.InvalidateLayout();
        }
        //public void Render()
        //{
        //    if (!Visible)
        //        return;
        //    //AbstractRenderer.PushCurrentCamera(_camera);
        //    _scene.Render(AbstractRenderer.CurrentCamera, AbstractRenderer.CurrentCamera.Frustum, null, false);
        //    //AbstractRenderer.PopCurrentCamera();
        //}
        protected void OnChildAdded(UIComponent child)
        {
            //child.OwningActor = this;
        }
        //public void Render()
        //{
        //    _scene.DoRender(AbstractRenderer.CurrentCamera, null);
        //}

        public void RemoveRenderableComponent(IRenderable component)
        {
            //component.RenderInfo.UnlinkScene();

            //_renderables.Remove(component);
        }
        public void AddRenderableComponent(IRenderable component)
        {
            //component.RenderInfo.LinkScene(component, Canvas.ScreenSpaceUIScene);
            //_screenSpaceUIScene.Add(component);

            //if (_renderables.Count == 0)
            //{
            //    _renderables.AddFirst(component);
            //    return;
            //}

            //int frontDist = _renderables.First.Value.RenderInfo.LayerIndex - component.RenderInfo.LayerIndex;
            //if (frontDist > 0)
            //{
            //    _renderables.AddFirst(component);
            //    return;
            //}

            //int backDist = component.RenderInfo.LayerIndex - _renderables.Last.Value.RenderInfo.LayerIndex;
            //if (backDist > 0)
            //{
            //    _renderables.AddLast(component);
            //    return;
            //}

            ////TODO: check if the following code is right
            //if (frontDist < backDist)
            //{
            //    //loop from back
            //    var last = _renderables.Last;
            //    while (last.Value.RenderInfo.LayerIndex > component.RenderInfo.LayerIndex)
            //        last = last.Previous;
            //    _renderables.AddBefore(last, component);
            //}
            //else
            //{
            //    //loop from front
            //    var first = _renderables.First;
            //    while (first.Value.RenderInfo.LayerIndex < component.RenderInfo.LayerIndex)
            //        first = first.Next;
            //    _renderables.AddAfter(first, component);
            //}
        }

        //public UIComponent FindComponent()
        //    => FindComponent(CursorPositionWorld());

        //public UIComponent FindComponent(Vector2 cursorWorldPos)
        //    => Canvas.FindDeepestComponent(cursorWorldPos, false);

        #region Cursor Position
        ///// <summary>
        ///// Returns the cursor position on the screen relative to the the viewport 
        ///// controlling this UI or the owning pawn's viewport which uses this UI as its HUD.
        ///// </summary>
        //public Vector2 CursorPosition()
        //{
        //    XRViewport v = OwningPawn?.LocalPlayerController?.Viewport ?? Viewport;
        //    Point absolute = Cursor.Position;
        //    Vector2 result;
        //    if (v != null)
        //    {
        //        RenderContext ctx = v.RenderHandler.Context;
        //        absolute = ctx.PointToClient(absolute);
        //        result = new Vector2(absolute.X, absolute.Y);
        //        result = v.AbsoluteToRelative(result);
        //    }
        //    else
        //        result = new Vector2(absolute.X, absolute.Y);
        //    return result;
        //}
        ///// <summary>
        ///// Returns a position in the world using a position relative to the viewport
        ///// controlling this UI or the owning pawn's viewport which uses this UI as its HUD.
        ///// </summary>
        //public Vector2 ViewportPositionToWorld(Vector2 viewportPosition)
        //{
        //    XRViewport v = OwningPawn?.LocalPlayerController?.Viewport ?? Viewport;
        //    return v?.ScreenToWorld(viewportPosition).Xy ?? Vector2.Zero;
        //}
        ///// <summary>
        ///// Returns the cursor position in the world relative to the the viewport 
        ///// controlling this UI or the owning pawn's viewport which uses this UI as its HUD.
        ///// </summary>
        //public Vector2 CursorPositionWorld()
        //{
        //    XRViewport v = OwningPawn?.LocalPlayerController?.Viewport ?? Viewport;
        //    return v?.ScreenToWorld(Viewport.CursorPositionRelativeTo(v)).Xy ?? Vector2.Zero;
        //}

        //public Vector2 CursorPositionWorld(Vector2 v)
        //    => v.ScreenToWorld(Viewport.CursorPositionRelativeTo(v)).Xy;
        //public Vector2 CursorPositionWorld(XRViewport v, Vector2 viewportPosition)
        //    => v.ScreenToWorld(viewportPosition).Xy;

        ///// <summary>
        ///// Renders the HUD in screen-space.
        ///// </summary>
        //public void PreRender()
        //{
        //    XRViewport v = OwningPawn?.LocalPlayerController?.Viewport ?? Viewport;
        //    if (v != null)
        //        Canvas?.ScreenSpaceUIScene?.PreRender(v, Canvas?.ScreenSpaceCamera);
        //}

        //public virtual void PreRenderSwap()
        //{
        //    if (Canvas.DrawSpace == ECanvasDrawSpace.Screen)
        //        Canvas.SwapBuffersScreenSpace();
        //}
        //public void RenderScreenSpace(XRViewport viewport, QuadFrameBuffer fbo)
        //{
        //    if (Canvas.DrawSpace == ECanvasDrawSpace.Screen)
        //        Canvas.RenderScreenSpace(viewport, fbo);
        //}

        public Action<UIInputComponent>? ResizeStarted;
        public Action<UIInputComponent>? ResizeFinished;
        private bool _isResizing = false;
        private UICanvasComponent? _canvas;

        protected virtual void ResizeLayout()
        {
            //Canvas.ResizeLayout(new BoundingRectangleF(
            //Canvas.Translation.Xy,
            //Canvas.Size.Value));
        }

        public virtual void UpdateLayout()
        {
            if (IsLayoutInvalidated)
            {
                IsResizing = true;
                ResizeStarted?.Invoke(this);
                ResizeLayout();
                IsLayoutInvalidated = false;
                IsResizing = false;
                ResizeFinished?.Invoke(this);
            }

            if (Canvas is not null && Canvas.DrawSpace == ECanvasDrawSpace.Screen)
                Canvas.UpdateScreenSpace();
        }

        [Browsable(false)]
        public bool IsLayoutInvalidated { get; private set; }
        public void InvalidateLayout() => IsLayoutInvalidated = true;

        public UIComponent? FindDeepestComponent(Vector2 viewportPoint)
        {
            //TODO
            return null;
            //return Canvas?.FindDeepestComponent(viewportPoint, false);
        }

        #endregion
    }
}
