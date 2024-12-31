using Extensions;
using System.Collections;
using System.ComponentModel;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Input.Devices;
using XREngine.Rendering;
using XREngine.Rendering.Info;
using XREngine.Rendering.UI;

namespace XREngine.Components
{
    /// <summary>
    /// Dictates input for a UI canvas component.
    /// </summary>
    [RequireComponents(typeof(UICanvasComponent))]
    public class UIInputComponent : XRComponent
    {
        /// <summary>
        /// Returns the canvas component this input component is controlling.
        /// </summary>
        public UICanvasComponent? GetCameraCanvas() => Canvas ?? GetSiblingComponent<UICanvasComponent>(true);

        /// <summary>
        /// The canvas component this input component is controlling.
        /// If null, the component will attempt to find a sibling canvas component.
        /// </summary>
        private UICanvasComponent? _canvas;
        public UICanvasComponent? Canvas
        {
            get => _canvas;
            set => SetField(ref _canvas, value);
        }

        private UIInteractableComponent? _focusedComponent;
        /// <summary>
        /// The UI component focused on by the gamepad or last interacted with by the mouse.
        /// </summary>
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

        private PawnComponent? _owningPawn;
        /// <summary>
        /// The pawn that has this HUD linked for screen or camera space use.
        /// World space HUDs do not require a pawn - call SetCursorPosition to set the cursor position.
        /// </summary>
        public PawnComponent? OwningPawn
        {
            get => _owningPawn;
            set => SetField(ref _owningPawn, value);
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(OwningPawn):
                        UnlinkOwningPawn();
                        break;
                }
            }
            return change;
        }
        protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
        {
            base.OnPropertyChanged(propName, prev, field);
            switch (propName)
            {
                case nameof(OwningPawn):
                    LinkOwningPawn();
                    break;
            }
        }

        private void UnlinkOwningPawn()
        {
            if (_owningPawn is null || _owningPawn.LocalPlayerController == null)
                return;

            _owningPawn.PropertyChanging -= OwningPawn_PropertyChanging;
            _owningPawn.PropertyChanged -= OnOwningPawnPropertyChanged;

            UnlinkInput();
        }

        private void LinkOwningPawn()
        {
            if (_owningPawn is null)
                return;

            _owningPawn.PropertyChanging += OwningPawn_PropertyChanging;
            _owningPawn.PropertyChanged += OnOwningPawnPropertyChanged;

            LinkInput();
        }

        private void OwningPawn_PropertyChanging(object? sender, IXRPropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(PawnComponent.LocalPlayerController))
            {
                UnlinkInput();
            }
        }

        private void OnOwningPawnPropertyChanged(object? sender, IXRPropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PawnComponent.LocalPlayerController))
            {
                LinkInput();
            }
        }

        private void LinkInput()
        {
            //Link input commands from the owning controller to this hud
            var input = _owningPawn!.LocalPlayerController!.Input;
            input.TryUnregisterInput();
            input.InputRegistration += RegisterInput;
            input.TryRegisterInput();
        }

        private void UnlinkInput()
        {
            //Unlink input commands from the owning controller to this hud
            var input = _owningPawn!.LocalPlayerController!.Input;
            input.TryUnregisterInput();
            input.InputRegistration -= RegisterInput;
            input.TryRegisterInput();
        }

        public void RegisterInput(InputInterface input)
        {
            input.RegisterMouseMove(MouseMove, EMouseMoveType.Absolute);
            input.RegisterMouseButtonEvent(EMouseButton.LeftClick, EButtonInputType.Pressed, OnLeftClickSelect);

            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickX, OnLeftStickX, false);
            input.RegisterAxisUpdate(EGamePadAxis.LeftThumbstickY, OnLeftStickY, false);
            input.RegisterButtonEvent(EGamePadButton.DPadUp, EButtonInputType.Pressed, OnDPadUp);
            input.RegisterButtonEvent(EGamePadButton.FaceDown, EButtonInputType.Pressed, OnGamepadInteract);
            input.RegisterButtonEvent(EGamePadButton.FaceRight, EButtonInputType.Pressed, OnGamepadBack);
        }

        /// <summary>
        /// The location of the mouse cursor in world space.
        /// </summary>
        public Vector2 CursorPositionWorld2D { get; private set; }
        /// <summary>
        /// The location of the mouse cursor in world space on the last update.
        /// </summary>
        public Vector2 LastCursorPositionWorld2D { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void MouseMove(float x, float y)
        {
            var vp = OwningPawn?.Viewport;
            if (vp is null)
                return;

            var canvas = GetCameraCanvas();
            if (canvas is null)
                return;

            var vpCoord = vp.ScreenToViewportCoordinate(new Vector2(x, y));
            var normCoord = vp.NormalizeViewportCoordinate(vpCoord);
            normCoord.Y = 1.0f - normCoord.Y;

            var canvasTransform = canvas.CanvasTransform;
            var space = canvasTransform.DrawSpace;

            Vector2? uiCoord = GetUICoordinate(vp, canvas.Camera2D, normCoord, canvasTransform, space);

            if (uiCoord is not null)
                CursorPositionWorld2D = uiCoord.Value;
        }

        private void CollectVisible()
        {
            GetCameraCanvas()?.VisualScene2D?.RenderTree?.FindAllIntersectingSorted(CursorPositionWorld2D, InteractableIntersections, InteractablePredicate);
        }

        private void SwapBuffers()
        {
            TopMostInteractable = InteractableIntersections.FirstOrDefault(x => x.Owner is UIInteractableComponent)?.Owner as UIInteractableComponent;
            //if (TopMostInteractable is not null)
            //    Debug.Out($"Topmost interactable: {TopMostInteractable.Name}");
            ValidateAndSwapIntersections();
            LastCursorPositionWorld2D = CursorPositionWorld2D;
        }

        private static Vector2? GetUICoordinate(XRViewport worldVP, XRCamera uiCam, Vector2 normCoord, UICanvasTransform canvasTransform, ECanvasDrawSpace space)
        {
            Vector2? uiCoord;
            //Convert to ui coord depending on the draw space
            switch (space)
            {
                case ECanvasDrawSpace.Screen:
                    {
                        //depth = 0 because we're in 2D, z coord is checked later
                        uiCoord = uiCam.NormalizedViewportToWorldCoordinate(normCoord, 0.0f).XY();
                        break;
                    }
                case ECanvasDrawSpace.Camera:
                    {
                        //Convert the normalized coord to world space using the draw distance
                        Vector3 worldCoord = uiCam.NormalizedViewportToWorldCoordinate(normCoord, XRMath.DistanceToDepth(canvasTransform.CameraDrawSpaceDistance, uiCam.NearZ, uiCam.FarZ));
                        //Transform the world coord to the canvas' local space
                        Matrix4x4 worldToLocal = canvasTransform.InverseWorldMatrix;
                        uiCoord = Vector3.Transform(worldCoord, worldToLocal).XY();
                        break;
                    }
                case ECanvasDrawSpace.World:
                    {
                        // Get the world segment from the normalized coord
                        // Transform the world segment to the canvas' local space
                        Segment localSegment = worldVP.GetWorldSegment(normCoord).TransformedBy(canvasTransform.InverseWorldMatrix);

                        // Check if the segment intersects the canvas' plane
                        if (GeoUtil.SegmentIntersectsPlane(
                            localSegment.Start,
                            localSegment.End,
                            XRMath.GetPlaneDistance(Vector3.Zero, Globals.Backward),
                            Globals.Backward,
                            out Vector3 localIntersectionPoint))
                        {
                            // Check if the point is within the canvas' bounds
                            var bounds = canvasTransform.GetActualBounds();
                            Vector2 point = localIntersectionPoint.XY();
                            if (bounds.Contains(point))
                                uiCoord = point;
                            else
                                uiCoord = null;
                        }
                        else
                            uiCoord = null;
                    }
                    break;
                default:
                    uiCoord = null;
                    break;
            }
            return uiCoord;
        }

        /// <summary>
        /// This verifies the mouseover state of previous and current mouse intersections and swaps them.
        /// </summary>
        private void ValidateAndSwapIntersections()
        {
            var all = LastInteractableIntersections.Union(InteractableIntersections).ToArray();
            all.ForEach(ValidateIntersection);
            (LastInteractableIntersections, InteractableIntersections) = (InteractableIntersections, LastInteractableIntersections);
            InteractableIntersections.Clear();
        }

        private void OnGamepadInteract()
        {

        }
        private void OnLeftClickSelect()
        {
            FocusedComponent = TopMostInteractable;
        }
        protected virtual void OnGamepadBack()
        {
            _focusedComponent?.OnBack();
        }

        protected virtual void OnLeftStickX(float value) { }
        protected virtual void OnLeftStickY(float value) { }

        /// <summary>
        /// Called on either left click or A button.
        /// Default behavior will OnClick the currently focused/highlighted UI component, if anything.
        /// </summary>
        protected virtual void OnInteract()
        {
            _focusedComponent?.OnInteract();
        }
        protected virtual void OnDPadUp()
        {

        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            GetCameraCanvas()?.CanvasTransform.InvalidateLayout();
            Engine.Time.Timer.CollectVisible += CollectVisible;
            Engine.Time.Timer.SwapBuffers += SwapBuffers;
        }
        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();
            Engine.Time.Timer.CollectVisible -= CollectVisible;
            Engine.Time.Timer.SwapBuffers -= SwapBuffers;
        }

        protected void OnChildAdded(UIComponent child)
        {
            //child.OwningActor = this;
        }

        //public void Render()
        //{
        //    _scene.DoRender(AbstractRenderer.CurrentCamera, null);
        //}

        [Browsable(false)]
        public bool IsLayoutInvalidated { get; private set; }
        public void InvalidateLayout() => IsLayoutInvalidated = true;

        /// <summary>
        /// This is the topmost UI component that the mouse is directly over.
        /// Typically, this will be the component that will receive input, like a button.
        /// Backgrounds and other non-interactive components will still be intersected with, but they will not be considered the topmost interactable.
        /// </summary>
        public UIInteractableComponent? TopMostInteractable { get; private set; }

        private SortedSet<RenderInfo2D> LastInteractableIntersections = new(new Comparer());
        private SortedSet<RenderInfo2D> InteractableIntersections = new(new Comparer());

        protected static bool InteractablePredicate(RenderInfo2D item)
            => item.Owner is UIInteractableComponent;
        private void ValidateIntersection(RenderInfo2D item)
        {
            if (item.Owner is not UIInteractableComponent inter)
                return;

            //Quick fix: sortedset uses a comparer to sort, but the comparer doesn't check for equality, resulting in invalid contains checks.
            var last = LastInteractableIntersections.ToArray();
            var curr = InteractableIntersections.ToArray();

            if (last.Contains(item))
            {
                //Mouse was over this renderable last update
                if (!curr.Contains(item))
                {
                    //Lost mouse over
                    inter.IsMouseOver = false;
                    inter.IsMouseDirectlyOver = false;
                }
                else
                {
                    //Had mouse over and still does now
                    var uiTransform = inter.UITransform;
                    if (inter.NeedsMouseMove)
                        inter.MouseMoved(
                            uiTransform.CanvasToLocal(LastCursorPositionWorld2D),
                            uiTransform.CanvasToLocal(CursorPositionWorld2D));
                }
            }
            else if (curr.Contains(item)) //Mouse was not over this renderable last update
            {
                //Got mouse over
                inter.IsMouseOver = true;
                inter.IsMouseDirectlyOver = inter == TopMostInteractable;
            }
            else
            {
                //Not over this renderable
                inter.IsMouseOver = false;
                inter.IsMouseDirectlyOver = false;
            }
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
            public int Compare(object? x, object? y)
                => Compare(x as RenderInfo2D, y as RenderInfo2D);
        }
    }
}
