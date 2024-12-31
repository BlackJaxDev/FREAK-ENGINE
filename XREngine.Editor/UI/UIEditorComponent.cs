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
public partial class UIEditorComponent : UIComponent
{
    public UISplitTransform SplitTransform => TransformAs<UISplitTransform>(true)!;

    private List<MenuOption> _rootMenuOptions = [];
    public List<MenuOption> RootMenuOptions
    {
        get => _rootMenuOptions;
        set => SetField(ref _rootMenuOptions, value);
    }

    private float _menuHeight = 40.0f;
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
            case nameof(RootMenuOptions):
                RemakeChildren();
                break;
        }
    }

    protected override void OnComponentActivated()
    {
        base.OnComponentActivated();
        RemakeChildren();
    }
    protected override void OnComponentDeactivated()
    {
        base.OnComponentDeactivated();
        SceneNode.Transform.Clear();
    }

    public void RemakeChildren()
    {
        var tfm = SplitTransform;
        tfm.VerticalSplit = true;
        tfm.FirstFixedSize = true;
        tfm.FixedSize = MenuHeight;
        tfm.SplitterSize = 0.0f;

        SceneNode.Transform.Clear();

        //There are two children, one for the menu and one for the dockable windows.
        var menuNode = SceneNode.NewChild<UIMaterialComponent>(out var menuMat);
        var dockableNode = SceneNode.NewChild();
        
        //Create the menu transform - this is a horizontal list of buttons.
        var listTfm = menuNode.SetTransform<UIListTransform>();
        listTfm.DisplayHorizontal = true;
        listTfm.ItemSpacing = 4.0f;
        listTfm.Padding = new Vector4(0.0f);
        listTfm.ItemAlignment = EListAlignment.TopOrLeft;
        listTfm.ItemSize = null;
        listTfm.Height = null;
        listTfm.Width = null;
        listTfm.MaxAnchor = new Vector2(1.0f, 1.0f);
        listTfm.MinAnchor = new Vector2(0.0f, 0.0f);
        listTfm.NormalizedPivot = new Vector2(0.0f, 0.0f);

        menuMat.Material = XRMaterial.CreateUnlitColorMaterialForward(ColorF4.Charcoal);

        //Create the buttons for each menu option.
        foreach (var menuItem in RootMenuOptions)
        {
            var buttonNode = menuNode.NewChild<UIButtonComponent, UIMaterialComponent>(out var button, out var background);
            menuItem.InteractableComponent = button;
            button.Name = menuItem.Text;

            var mat = XRMaterial.CreateUnlitColorMaterialForward(ColorF4.Transparent);
            mat.EnableTransparency();
            background.Material = mat;

            var buttonTfm = buttonNode.GetTransformAs<UIBoundableTransform>(true)!;
            buttonTfm.Width = null;
            buttonTfm.Height = null;
            buttonTfm.Translation = new Vector2(0.0f, 0.0f);
            buttonTfm.MaxAnchor = new Vector2(0.0f, 1.0f);
            buttonTfm.MinAnchor = new Vector2(0.0f, 0.0f);
            buttonTfm.NormalizedPivot = new Vector2(0.0f, 0.0f);
            buttonTfm.Margins = new Vector4(4.0f);

            var buttonTextNode = buttonNode.NewChild<UITextComponent>(out var text);

            var textTfm = text.BoundableTransform;
            textTfm.Width = null;
            textTfm.Height = null;
            textTfm.MaxAnchor = new Vector2(1.0f, 1.0f);
            textTfm.MinAnchor = new Vector2(0.0f, 0.0f);
            textTfm.NormalizedPivot = new Vector2(0.0f, 0.0f);
            textTfm.Margins = new Vector4(10.0f, 4.0f, 10.0f, 4.0f);

            text.FontSize = 18;
            text.Text = menuItem.Text;
            text.Color = ColorF4.Gray;
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
