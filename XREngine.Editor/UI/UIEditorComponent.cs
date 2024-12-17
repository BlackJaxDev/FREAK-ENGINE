using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Colors;
using XREngine.Rendering;
using XREngine.Rendering.UI;
using XREngine.Scene;

namespace XREngine.Editor.UI.Components;

/// <summary>
/// The root component for the desktop editor.
/// </summary>
[RequiresTransform(typeof(UISplitTransform))]
public partial class UIEditorComponent : UIInteractableComponent
{
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

    protected override void OnComponentActivated()
    {
        base.OnComponentActivated();
        RemakeChildren();
    }

    public void RemakeChildren()
    {
        SceneNode.Transform.Clear();

        //There are two children, one for the menu and one for the dockable windows.
        var menuNode = SceneNode.NewChild();
        var dockableNode = SceneNode.NewChild();

        //Create the menu transform - this is a horizontal list of buttons.
        var list = menuNode.SetTransform<UIListTransform>();
        list.DisplayHorizontal = true;
        list.ItemSpacing = 4.0f;
        list.Padding = new Vector4(4.0f, 4.0f, 4.0f, 4.0f);
        list.ItemAlignment = EListAlignment.TopOrLeft;
        list.ItemSize = null;
        list.Height = MenuHeight;
        list.MaxAnchor = new Vector2(1.0f, 1.0f);
        list.MinAnchor = new Vector2(0.0f, 1.0f);

        //Create the buttons for each menu option.
        foreach (var menuItem in RootMenuOptions)
        {
            var buttonNode = menuNode.NewChild<UIButtonComponent, UIMaterialComponent>(out var button, out var background);
            menuItem.InteractableComponent = button;
            background.Material = XRMaterial.CreateUnlitColorMaterialForward(ColorF4.Black);

            var buttonTextNode = buttonNode.NewChild<UITextComponent>(out var text);
            text.Text = menuItem.Text;

            var textTfm = text.BoundableTransform;
            textTfm.Width = null;

            var buttonTfm = button.BoundableTransform;
            buttonTfm.Width = null;
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
