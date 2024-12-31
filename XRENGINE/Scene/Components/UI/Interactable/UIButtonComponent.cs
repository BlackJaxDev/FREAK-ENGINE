using XREngine.Data.Colors;

namespace XREngine.Rendering.UI
{
    public class UIButtonComponent : UIInteractableComponent
    {
        protected override void OnMouseOverlapEnter() => Highlight();
        protected override void OnMouseOverlapLeave() => Unhighlight();
        protected override void OnGamepadNavigateEnter() => Highlight();
        protected override void OnGamepadNavigateLeave() => Unhighlight();

        public UIMaterialComponent? BackgroundMaterialComponent => GetSiblingComponent<UIMaterialComponent>();
        public UITextComponent? TextComponent => SceneNode.FirstChild?.GetComponent<UITextComponent>();

        public ColorF4 DefaultBackgroundColor { get; set; } = ColorF4.Transparent;
        public ColorF4 HighlightBackgroundColor { get; set; } = ColorF4.DarkGray;
        public string BackgroundColorUniformName { get; set; } = "MatColor";

        public ColorF4 DefaultTextColor { get; set; } = ColorF4.Gray;
        public ColorF4 HighlightTextColor { get; set; } = ColorF4.White;

        public Action? ClickAction { get; set; }

        public virtual void Click()
        {
            ClickAction?.Invoke();
        }
        
        protected virtual void Highlight()
        {
            var bg = BackgroundMaterialComponent?.Material;
            bg?.SetVector4(BackgroundColorUniformName, HighlightBackgroundColor);

            var text = TextComponent;
            if (text is not null)
                text.Color = HighlightTextColor;
        }
        protected virtual void Unhighlight()
        {
            var bg = BackgroundMaterialComponent?.Material;
            bg?.SetVector4(BackgroundColorUniformName, DefaultBackgroundColor);

            var text = TextComponent;
            if (text is not null)
                text.Color = DefaultTextColor;
        }
    }
}