using System.ComponentModel;
using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Core;
using XREngine.Data.Geometry;
using XREngine.Rendering;
using XREngine.Rendering.UI;
using XREngine.Scene;
using XREngine.Scene.Transforms;

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

            if (!SceneNode.IsTransformNull && Transform is UICanvasTransform tfm)
                tfm.PropertyChanged -= OnCanvasTransformPropertyChanged;
        }
        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();

            if (!SceneNode.IsTransformNull && Transform is UICanvasTransform tfm)
                tfm.PropertyChanged += OnCanvasTransformPropertyChanged;
        }

        private void OnCanvasTransformPropertyChanged(object? sender, IXRPropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(UICanvasTransform.Translation):
                case nameof(UICanvasTransform.ActualSize):
                    ResizeScreenSpace(CanvasTransform.Bounds);
                    break;
            }
        }

        private void ResizeScreenSpace(BoundingRectangleF bounds)
        {
            Camera2D ??= new(new Transform());
            VisualScene2D ??= new();

            //Recreate the size of the render tree to match the new size.
            VisualScene2D?.RenderTree.Remake(bounds);

            //Update the camera parameters to match the new size.
            if (Camera2D.Parameters is not XROrthographicCameraParameters orthoParams)
                Camera2D.Parameters = orthoParams = new XROrthographicCameraParameters(bounds.Width, bounds.Height, -0.5f, 0.5f);
            orthoParams.SetOriginBottomLeft();
            orthoParams.Resize(bounds.Width, bounds.Height);

            if (Transform is UICanvasTransform tfm)
                _renderPipeline.ViewportResized(tfm.ActualSize);
        }

        private XRCamera? _screenSpaceCamera;
        /// <summary>
        /// This is the camera used to render the 2D canvas.
        /// </summary>
        public XRCamera? Camera2D
        {
            get => _screenSpaceCamera;
            private set => SetField(ref _screenSpaceCamera, value);
        }

        private VisualScene2D? _visualScene2D;
        /// <summary>
        /// This is the scene that contains all the 2D renderables.
        /// </summary>
        public VisualScene2D? VisualScene2D
        {
            get => _visualScene2D;
            private set => SetField(ref _visualScene2D, value);
        }

        /// <summary>
        /// Gets the user input component. Not a necessary component, so may be null.
        /// </summary>
        /// <returns></returns>
        public UIInputComponent? GetInputComponent() => GetSiblingComponent<UIInputComponent>();

        public void Render(XRFrameBuffer outputFBO)
        {
            if (Transform is not UICanvasTransform tfm || 
                tfm.DrawSpace != ECanvasDrawSpace.Screen || 
                Camera2D is null || 
                VisualScene2D is null)
                return;

            _renderPipeline.Render(VisualScene2D, Camera2D, null, outputFBO, null, false);
        }

        public void SwapBuffersScreenSpace()
        {
            _renderPipeline.MeshRenderCommands.SwapBuffers(false);
            VisualScene2D?.GlobalSwapBuffers();
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
            if (CanvasTransform.DrawSpace != ECanvasDrawSpace.Screen)
                CanvasTransform.UpdateLayout();
        }

        public void CollectVisibleItemsScreenSpace()
        {
            //Update the layout if it's invalid.
            CanvasTransform.UpdateLayout();

            //Collect the rendered items now that the layout is updated.
            if (_renderPipeline.Pipeline is not null)
                VisualScene2D?.CollectRenderedItems(_renderPipeline.MeshRenderCommands, Camera2D, false, null, false);
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
