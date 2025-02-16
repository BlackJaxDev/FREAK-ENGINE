using Silk.NET.Input;
using XREngine.Data.Core;
using XREngine.Editor.UI.Components;
using XREngine.Rendering.UI;

namespace XREngine.Editor.UI.Toolbar;

public class ToolbarButton : ToolbarItemBase
{
    private string _text = string.Empty;
    private Action<UIInteractableComponent>? _action;
    private bool _childOptionsVisible = false;
    private UIButtonComponent? _interactableComponent = null;
    private UIToolbarComponent? _parentToolbarComponent = null;
    private Key[]? _shortcutKeys;

    private ToolbarButton()
    {
        ChildOptions.PostAnythingAdded += ChildOptions_PostAnythingAdded;
        ChildOptions.PostAnythingRemoved += ChildOptions_PostAnythingRemoved;
    }
    public ToolbarButton(string? text, params ToolbarButton[] childOptions) : this()
    {
        Text = text ?? string.Empty;
        ChildOptions.AddRange(childOptions);
    }
    public ToolbarButton(string? text, Action<UIInteractableComponent>? action, Key[]? shortcutKeys = null) : this()
    {
        Text = text ?? string.Empty;
        Action = action;
        ShortcutKeys = shortcutKeys;
    }
    public ToolbarButton(string? text, Key[]? shortcutKeys, params ToolbarButton[] childOptions) : this()
    {
        Text = text ?? string.Empty;
        ShortcutKeys = shortcutKeys;
        ChildOptions.AddRange(childOptions);
    }
    public ToolbarButton(
        string? text,
        Action<UIInteractableComponent>? action,
        Key[]? shortcutKeys,
        params ToolbarButton[] childOptions) : this()
    {
        Text = text ?? string.Empty;
        Action = action;
        ShortcutKeys = shortcutKeys;
        ChildOptions.AddRange(childOptions);
    }

    private void ChildOptions_PostAnythingRemoved(ToolbarButton item) => item.Parent = null;
    private void ChildOptions_PostAnythingAdded(ToolbarButton item) => item.Parent = this;

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
    private ToolbarButton? _parent;
    public ToolbarButton? Parent
    {
        get => _parent;
        set => SetField(ref _parent, value);
    }
    public EventList<ToolbarButton> ChildOptions { get; } = [];
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
    public UIToolbarComponent? ParentToolbarComponent
    {
        get => _parentToolbarComponent;
        set => SetField(ref _parentToolbarComponent, value);
    }
    public void OnInteracted(UIInteractableComponent component)
    {
        Action?.Invoke(component);
        var interTfm = InteractableComponent?.Transform;
        if (interTfm?.ChildCount >= 2)
            ChildOptionsVisible = !ChildOptionsVisible;
        else
        {
            ChildOptionsVisible = false;
            if (InteractableComponent is not null)
                InteractableComponent.IsFocused = false;
            var parent = Parent;
            while (parent is not null)
            {
                parent.ChildOptionsVisible = false;
                parent = parent.Parent;
            }
        }
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
                        InteractableComponent.PropertyChanged -= OnInteractablePropertyChanged;
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
                    InteractableComponent.PropertyChanged += OnInteractablePropertyChanged;
                }
                break;
            case nameof(ChildOptionsVisible):

                var interTfm = InteractableComponent?.Transform;
                if (interTfm?.ChildCount < 2)
                    break;

                var submenuTfm = InteractableComponent?.Transform?.LastChild() as UIBoundableTransform;
                if (submenuTfm is not null)
                    submenuTfm.Visibility = ChildOptionsVisible 
                        ? EVisibility.Visible
                        : EVisibility.Collapsed;

                if (ParentToolbarComponent is null)
                    break;

                if (ChildOptionsVisible)
                    ParentToolbarComponent.ActiveSubmenus.Add(this);
                else
                    ParentToolbarComponent.ActiveSubmenus.Remove(this);

                break;
        }
    }

    public bool AnyOptionsFocused => 
        (InteractableComponent?.IsFocused ?? false) ||
        ChildOptions.Any(c => c.AnyOptionsFocused);

    private void OnInteractablePropertyChanged(object? sender, IXRPropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(UIButtonComponent.IsFocused):
                if (!AnyOptionsFocused)
                    ChildOptionsVisible = false;
                if (Parent is not null && !Parent.AnyOptionsFocused)
                    Parent.ChildOptionsVisible = false;
                break;
        }
    }
}