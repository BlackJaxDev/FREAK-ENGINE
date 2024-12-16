using System.Numerics;
using XREngine.Components;
using XREngine.Core.Attributes;
using XREngine.Data.Core;
using XREngine.Rendering.UI;
using XREngine.Scene;

namespace XREngine.Editor.UI.Components;

/// <summary>
/// The root component for the desktop editor.
/// </summary>
[RequiresTransform(typeof(UISplitTransform))]
[RequireComponents(typeof(UICanvasComponent))]
[RequireComponents(typeof(UIInputComponent))]
public partial class UIEditorComponent : UIInteractableComponent
{
    public UICanvasComponent Canvas => GetSiblingComponent<UICanvasComponent>(true)!;
    public UIInputComponent Input => GetSiblingComponent<UIInputComponent>(true)!;

    private List<MenuOption> _rootMenuOptions = [];
    public List<MenuOption> RootMenuOptions
    {
        get => _rootMenuOptions;
        set => SetField(ref _rootMenuOptions, value);
    }

    private float _menuHeight = 50.0f;
    public float MenuHeight
    {
        get => _menuHeight;
        set => SetField(ref _menuHeight, value);
    }

    protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
    {
        base.OnPropertyChanged(propName, prev, field);
        switch (propName)
        {
            case nameof(MenuHeight):
                MenuNode.GetTransformAs<UIBoundableTransform>(true)!.Height = MenuHeight;
                break;
        }
    }

    public class MenuOption : XRBase
    {
        private string _text = string.Empty;
        private Action<UIInteractableComponent>? _action;
        private List<MenuOption> _childOptions = [];
        private bool _childOptionsVisible = false;
        private UIButtonComponent? _interactable = null;

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
        public UIButtonComponent? Interactable
        {
            get => _interactable;
            set => SetField(ref _interactable, value);
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
                    case nameof(Interactable):
                        if (Interactable is not null)
                        {
                            Interactable.InteractAction -= OnInteracted;
                            Interactable.BackAction -= OnCancelInteraction;
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
                case nameof(Interactable):
                    if (Interactable is not null)
                    {
                        Interactable.InteractAction += OnInteracted;
                        Interactable.BackAction += OnCancelInteraction;
                    }
                    break;
            }
        }
    }

    public class MenuDropdown : MenuOption
    {
        private bool _isOpen = false;
        private string[] _options = [];

        public bool IsOpen
        {
            get => _isOpen;
            set => SetField(ref _isOpen, value);
        }
        public string[] Options
        {
            get => _options;
            set => SetField(ref _options, value);
        }

        public void SetOptionWithEnum<T>(T value) where T : Enum
        {
            Text = value.ToString();
            Options = Enum.GetNames(typeof(T));
        }
    }

    protected override void OnComponentActivated()
    {
        base.OnComponentActivated();
        RemakeChildren();
    }

    public void RemakeChildren()
    {
        SceneNode.Transform.Clear();

        //There are two children, one for the menu and one for the dockable windows.
        var menuNode = new SceneNode();
        SceneNode.AddChild(menuNode);
        var dockableNode = new SceneNode();
        SceneNode.AddChild(dockableNode);

        //Create the menu transform - this is a horizontal list of buttons.
        var list = menuNode.SetTransform<UIListTransform>();
        list.DisplayHorizontal = true;
        list.ItemSpacing = 4.0f;
        list.Padding = new Vector4(4.0f, 4.0f, 4.0f, 4.0f);
        list.ItemAlignment = EListAlignment.TopOrLeft;
        list.ItemSize = null;
        list.Height = MenuHeight;

        //Create the buttons for each menu option.
        foreach (var menuItem in RootMenuOptions)
        {
            var buttonNode = SceneNode.New<UIButtonComponent, UIMaterialComponent>(menuNode, out var button, out var background);
            menuItem.Interactable = button;
            var buttonTextNode = SceneNode.New<UITextComponent>(buttonNode, out var text);
            text.Text = menuItem.Text;
        }

        //Create the dockable windows transform for panels
        var dock = dockableNode.SetTransform<UISplitTransform>();
        dock.VerticalSplit = false;

    }

    /// <summary>
    /// The scene node that contains the menu options.
    /// </summary>
    public SceneNode MenuNode
    {
        get
        {
            if (SceneNode.Transform.Children.Count < 2)
                RemakeChildren();
            return SceneNode.FirstChild!;
        }
    }
    /// <summary>
    /// The scene node that contains the dockable windows.
    /// </summary>
    public SceneNode DockableNode
    {
        get
        {
            if (SceneNode.Transform.Children.Count < 2)
                RemakeChildren();
            return SceneNode.LastChild!;
        }
    }
}
