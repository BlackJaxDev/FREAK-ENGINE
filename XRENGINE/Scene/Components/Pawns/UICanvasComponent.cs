using System.ComponentModel;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Rendering;
using XREngine.Rendering.UI;

namespace XREngine.Components
{
    /// <summary>
    /// Renders a 2D canvas on top of the screen, in world space, or in camera space.
    /// </summary>
    [RequiresTransform(typeof(UICanvasTransform))]
    public class UICanvasComponent : XRComponent
    {
        public UICanvasTransform CanvasTransform => TransformAs<UICanvasTransform>(true)!;

        protected override void OnTransformChanging()
        {
            base.OnTransformChanging();
            if (Transform is UICanvasTransform tfm)
                tfm.PropertyChanged -= OnCanvasTransformPropertyChanged;
        }
        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();
            if (Transform is UICanvasTransform tfm)
                tfm.PropertyChanged += OnCanvasTransformPropertyChanged;
        }

        private void OnCanvasTransformPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(UICanvasTransform.ActualSize):
                    if (Transform is UICanvasTransform tfm)
                        _renderPipeline.ViewportResized(tfm.ActualSize);
                    break;
            }
        }

        /// <summary>
        /// Gets the user input component. Not a necessary component, so may be null.
        /// </summary>
        /// <returns></returns>
        public UIInputComponent? GetInputComponent() => GetSiblingComponent<UIInputComponent>();

        public void Render(XRFrameBuffer outputFBO)
        {
            if (Transform is UICanvasTransform tfm)
                _renderPipeline.Render(tfm.Scene2D, tfm.Camera2D, null, outputFBO, null, false);
        }

        public void SwapBuffersScreenSpace()
        {
            _renderPipeline.MeshRenderCommands.SwapBuffers(false);
        }

        protected internal override void OnComponentActivated()
        {
            base.OnComponentActivated();
            Engine.Time.Timer.CollectVisible += UpdateLayoutWorldSpace;
        }

        protected internal override void OnComponentDeactivated()
        {
            base.OnComponentDeactivated();
            Engine.Time.Timer.CollectVisible -= UpdateLayoutWorldSpace;
        }

        private void UpdateLayoutWorldSpace()
        {
            //If in world space, no camera will be calling this method so we have to do it here.
            if (CanvasTransform.DrawSpace == ECanvasDrawSpace.World)
                CanvasTransform.UpdateLayout();
        }

        public void CollectVisibleItemsScreenSpace()
        {
            //Update the layout if it's invalid.
            CanvasTransform.UpdateLayout();

            //Collect the rendered items now that the layout is updated.
            if (_renderPipeline.Pipeline is not null)
                CanvasTransform.Scene2D?.CollectRenderedItems(_renderPipeline.MeshRenderCommands, CanvasTransform.Camera2D, false, null, false);
        }

        private readonly XRRenderPipelineInstance _renderPipeline = new() { Pipeline = new DefaultRenderPipeline() };
        public XRRenderPipelineInstance RenderPipelineInstance => _renderPipeline;

        public UIComponent? FindDeepestComponent(Vector2 normalizedViewportPosition)
        {
            //if (CanvasTransform.DrawSpace == ECanvasDrawSpace.Screen)
            //    return (CanvasTransform.ScreenSpaceWorld.VisualScene as VisualScene2D)?.RenderTree?.FindDeepestComponent(normalizedViewportPosition);

            return null;
        }
    }
}
