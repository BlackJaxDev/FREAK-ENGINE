using Silk.NET.GLFW;
using Silk.NET.Input;
using XREngine.Data.Core;
using XREngine.Rendering.UI;

namespace XREngine.Editor.UI.Components;

public partial class UIEditorComponent
{
    public class MenuOption : XRBase
    {
        private string _text = string.Empty;
        private Action<UIInteractableComponent>? _action;
        private List<MenuOption> _childOptions = [];
        private bool _childOptionsVisible = false;
        private UIButtonComponent? _interactableComponent = null;
        private Key[]? _shortcutKeys;

        public MenuOption(string? text = null, Action<UIInteractableComponent>? action = null, Key[]? shortcutKeys = null, params MenuOption[] childOptions)
        {
            Text = text ?? string.Empty;
            Action = action;
            ShortcutKeys = shortcutKeys;
            ChildOptions = new List<MenuOption>(childOptions);
        }

        public string Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }
        public Action<UIInteractableComponent>? Action
        {
            get => _action;
            set => SetField(ref _action, value);
        }
        public List<MenuOption> ChildOptions
        {
            get => _childOptions;
            set => SetField(ref _childOptions, value);
        }
        public bool ChildOptionsVisible
        {
            get => _childOptionsVisible;
            set => SetField(ref _childOptionsVisible, value);
        }
        public Key[]? ShortcutKeys
        {
            get => _shortcutKeys;
            set => SetField(ref _shortcutKeys, value);
        }

        /// <summary>
        /// The interactable component that represents this menu option.
        /// Upon setting this property, the interactable component will be subscribed to the appropriate events.
        /// </summary>
        public UIButtonComponent? InteractableComponent
        {
            get => _interactableComponent;
            set => SetField(ref _interactableComponent, value);
        }

        public void OnInteracted(UIInteractableComponent component)
        {
            Action?.Invoke(component);
            ChildOptionsVisible = !ChildOptionsVisible;
        }

        public void OnCancelInteraction(UIInteractableComponent component)
        {
            ChildOptionsVisible = false;
        }

        protected override bool OnPropertyChanging<T>(string? propName, T field, T @new)
        {
            bool change = base.OnPropertyChanging(propName, field, @new);
            if (change)
            {
                switch (propName)
                {
                    case nameof(InteractableComponent):
                        if (InteractableComponent is not null)
                        {
                            InteractableComponent.InteractAction -= OnInteracted;
                            InteractableComponent.BackAction -= OnCancelInteraction;
                        }
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
                case nameof(InteractableComponent):
                    if (InteractableComponent is not null)
                    {
                        InteractableComponent.InteractAction += OnInteracted;
                        InteractableComponent.BackAction += OnCancelInteraction;
                    }
                    break;
            }
        }
    }
}
