using System.Numerics;
using XREngine.Core.Attributes;
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
        //protected internal override void OnComponentActivated()
        //{
        //    base.OnComponentActivated();
        //    CanvasTransform.PropertyChanged += OnCanvasTransformPropertyChanged;
        //}

        //protected internal override void OnComponentDeactivated()
        //{
        //    base.OnComponentDeactivated();
        //    CanvasTransform.PropertyChanged -= OnCanvasTransformPropertyChanged;
        //}

        //private void OnCanvasTransformPropertyChanged(object? sender, PropertyChangedEventArgs e)
        //{
        //    switch (e.PropertyName)
        //    {
        //        case nameof(UICanvasTransform.Size):
        //            _renderPipeline.ViewportResized(CanvasTransform.Size);
        //            break;
        //    }
        //}

        /// <summary>
        /// Gets the user input component. Not a necessary component, so may be null.
        /// </summary>
        /// <returns></returns>
        public UIInputComponent? GetInputComponent() => GetSiblingComponent<UIInputComponent>();

        public UICanvasTransform CanvasTransform => TransformAs<UICanvasTransform>(true)!;

        //protected override void OnResizeLayout(BoundingRectangleF parentRegion)
        //{
        //    //Debug.Out($"UI CANVAS {Name} : {parentRegion.Width.Rounded(2)} x {parentRegion.Height.Rounded(2)}");

        //    //ScreenSpaceUIScene?.Resize(parentRegion.Extents);
        //    //ScreenSpaceCamera?.Resize(parentRegion.Width, parentRegion.Height);
        //    //base.OnResizeLayout(parentRegion);
        //}

        public void Render(XRViewport vp, XRFrameBuffer outputFBO)
            => RenderPipelineInstance.Render(CanvasTransform.Scene2D, CanvasTransform.Camera2D, null, outputFBO, null, false);

        public void SwapBuffers()
        {
            _renderPipeline.MeshRenderCommands.SwapBuffers(false);
        }

        public void CollectRenderedItems(XRViewport viewport)
        {
            if (_renderPipeline is null)
                return;

            //Set the canvas size if it doesn't match the viewport size, and the canvas is in screen space or camera space.
            if (CanvasTransform.DrawSpace != ECanvasDrawSpace.World)
                if (CanvasTransform.Size != viewport.Region.Size)
                    CanvasTransform.Size = viewport.Region.Size;

            //Update the layout if it's invalid.
            CanvasTransform.UpdateLayout();

            //Collect the rendered items now that the layout is updated.
            CanvasTransform.Scene2D?.CollectRenderedItems(_renderPipeline.MeshRenderCommands, CanvasTransform.Camera2D, false, null, false);
        }

        private readonly XRRenderPipelineInstance _renderPipeline = new();
        public XRRenderPipelineInstance RenderPipelineInstance => _renderPipeline;

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

        public UIComponent? FindDeepestComponent(Vector2 normalizedViewportPosition)
        {
            //if (CanvasTransform.DrawSpace == ECanvasDrawSpace.Screen)
            //    return (CanvasTransform.ScreenSpaceWorld.VisualScene as VisualScene2D)?.RenderTree?.FindDeepestComponent(normalizedViewportPosition);

            return null;
        }
    }
}
