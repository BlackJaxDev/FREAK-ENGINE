using XREngine.Data.Colors;

namespace XREngine.Rendering.UI
{
    public class UIButtonComponent : UIInteractableComponent
    {
        protected override void OnMouseDirectOverlapEnter() => Highlight();
        protected override void OnMouseDirectOverlapLeave() => Unhighlight();
        protected override void OnGamepadNavigateEnter() => Highlight();
        protected override void OnGamepadNavigateLeave() => Unhighlight();

        public void RegisterClickActions(params Action<UIInteractableComponent>[] actions)
        {
            foreach (var action in actions)
                InteractAction += action;
        }
        public void UnregisterClickActions(params Action<UIInteractableComponent>[] actions)
        {
            foreach (var action in actions)
                InteractAction -= action;
        }

        public UIMaterialComponent? BackgroundMaterialComponent => GetSiblingComponent<UIMaterialComponent>();
        public UITextComponent? TextComponent => SceneNode.FirstChild?.GetComponent<UITextComponent>();

        public ColorF4 DefaultBackgroundColor { get; set; } = ColorF4.Transparent;
        public ColorF4 HighlightBackgroundColor { get; set; } = ColorF4.DarkGray;
        public string BackgroundColorUniformName { get; set; } = "MatColor";

        public ColorF4 DefaultTextColor { get; set; } = ColorF4.Gray;
        public ColorF4 HighlightTextColor { get; set; } = ColorF4.White;

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