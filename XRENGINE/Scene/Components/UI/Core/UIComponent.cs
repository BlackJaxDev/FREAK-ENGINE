using XREngine.Components;
using XREngine.Core.Attributes;
using XREngine.Data.Core;

namespace XREngine.Rendering.UI
{
    [RequiresTransform(typeof(UITransform))]
    public class UIComponent : XRComponent
    {
        public UITransform UITransform => TransformAs<UITransform>(true)!;
        public UICanvasComponent? UserInterfaceCanvas => UITransform.ParentCanvas?.SceneNode?.GetComponent<UICanvasComponent>();

        internal override void VerifyInterfacesOnStart()
        {
            base.VerifyInterfacesOnStart();

            if (this is not IRenderable rend)
                return;

            UITransform.PropertyChanging += UITransformPropertyChanging;
            UITransform.PropertyChanged += UITransformPropertyChanged;

            var canvas = UserInterfaceCanvas;
            foreach (var obj in rend.RenderedObjects)
                obj.UserInterfaceCanvas = canvas;
        }
        internal override void VerifyInterfacesOnStop()
        {
            base.VerifyInterfacesOnStop();

            if (this is not IRenderable rend)
                return;

            UITransform.PropertyChanging -= UITransformPropertyChanging;
            UITransform.PropertyChanged -= UITransformPropertyChanged;

            foreach (var obj in rend.RenderedObjects)
                obj.UserInterfaceCanvas = null;
        }

        protected override void OnTransformChanging()
        {
            base.OnTransformChanging();

            if (this is not IRenderable rend || SceneNode.IsTransformNull || Transform is not UITransform uiTfm)
                return;

            uiTfm.PropertyChanging -= UITransformPropertyChanging;
            uiTfm.PropertyChanged -= UITransformPropertyChanged;

            foreach (var obj in rend.RenderedObjects)
                obj.UserInterfaceCanvas = null;
        }
        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();

            if (this is not IRenderable rend || SceneNode.IsTransformNull || Transform is not UITransform uiTfm)
                return;

            uiTfm.PropertyChanging += UITransformPropertyChanging;
            uiTfm.PropertyChanged += UITransformPropertyChanged;

            var canvas = uiTfm.ParentCanvas?.SceneNode?.GetComponent<UICanvasComponent>();
            foreach (var obj in rend.RenderedObjects)
                obj.UserInterfaceCanvas = canvas;
        }
        /// <summary>
        /// Called when one of the transform's properties about to change.
        /// Linking this callback to the current UITransform is handled automatically for you.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void UITransformPropertyChanging(object? sender, IXRPropertyChangingEventArgs e)
        {
            if (e.PropertyName != nameof(UITransform.ParentCanvas) || this is not IRenderable rend)
                return;
            
            foreach (var obj in rend.RenderedObjects)
                obj.UserInterfaceCanvas = null;
        }
        /// <summary>
        /// Called when one of the transform's properties has changed.
        /// Linking this callback to the current UITransform is handled automatically for you.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void UITransformPropertyChanged(object? sender, IXRPropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UITransform.ParentCanvas) || this is not IRenderable rend)
                return;

            var canvas = UserInterfaceCanvas;
            foreach (var obj in rend.RenderedObjects)
                obj.UserInterfaceCanvas = canvas;
        }
    }
}
