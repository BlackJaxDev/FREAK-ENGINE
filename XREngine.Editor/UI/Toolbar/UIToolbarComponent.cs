using System.Numerics;
using XREngine.Core.Attributes;
using XREngine.Data.Colors;
using XREngine.Editor.UI.Toolbar;
using XREngine.Rendering;
using XREngine.Rendering.UI;
using XREngine.Scene;

namespace XREngine.Editor.UI.Components;

/// <summary>
/// The root component for the desktop editor.
/// </summary>
[RequiresTransform(typeof(UIBoundableTransform))]
public partial class UIToolbarComponent : UIComponent
{
    public UIBoundableTransform BoundableTransform => TransformAs<UIBoundableTransform>(true)!;

    private List<ToolbarButton> _rootMenuOptions = [];
    public List<ToolbarButton> RootMenuOptions
    {
        get => _rootMenuOptions;
        set => SetField(ref _rootMenuOptions, value);
    }

    public List<ToolbarButton> ActiveSubmenus { get; } = [];

    private float _menuHeight = 40.0f;
    public float SubmenuItemHeight
    {
        get => _menuHeight;
        set => SetField(ref _menuHeight, value);
    }

    private const float Margin = 4.0f;

    protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
    {
        base.OnPropertyChanged(propName, prev, field);
        switch (propName)
        {
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
        SceneNode.Transform.Clear();
        //Create the root menu transform - this is a horizontal list of buttons.
        CreateMenu(SceneNode, true, null, null, RootMenuOptions, false, null, this);
    }

    public UIListTransform CreateMenu(
        SceneNode parentNode,
        bool horizontal,
        float? width,
        float? height,
        IList<ToolbarButton> options,
        bool alignSubmenuToSide,
        float? menuHeight,
        UIToolbarComponent toolbar)
    {
        var listNode = parentNode.NewChild<UIMaterialComponent>(out var menuMat);
        menuMat.Material = XRMaterial.CreateUnlitColorMaterialForward(ColorF4.Charcoal);
        var listTfm = listNode.SetTransform<UIListTransform>();
        listTfm.DisplayHorizontal = horizontal;
        listTfm.ItemSpacing = Margin;
        listTfm.Padding = new Vector4(0.0f);
        listTfm.ItemAlignment = EListAlignment.TopOrLeft;
        listTfm.ItemSize = menuHeight;
        listTfm.Width = width;
        listTfm.Height = height;
        CreateChildMenu(options, listNode, alignSubmenuToSide, toolbar);
        return listTfm;
    }

    //Works for both horizontal root menu and vertical submenus
    private void CreateChildMenu(
        IList<ToolbarButton> options,
        SceneNode listNode,
        bool alignSubmenuToSide,
        UIToolbarComponent toolbar)
    {
        //Create the buttons for each menu option.
        foreach (var menuItem in options)
        {
            var buttonNode = listNode.NewChild<UIButtonComponent, UIMaterialComponent>(out var button, out var background);
            menuItem.InteractableComponent = button;
            menuItem.ParentToolbarComponent = toolbar;
            button.Name = menuItem.Text;

            var mat = XRMaterial.CreateUnlitColorMaterialForward(ColorF4.Transparent);
            mat.EnableTransparency();
            background.Material = mat;

            var buttonTfm = buttonNode.GetTransformAs<UIBoundableTransform>(true)!;
            buttonTfm.MaxAnchor = new Vector2(0.0f, 1.0f);
            buttonTfm.Margins = new Vector4(Margin);

            buttonNode.NewChild<UITextComponent>(out var text);

            var textTfm = text.BoundableTransform;
            textTfm.Margins = new Vector4(10.0f, Margin, 10.0f, Margin);

            text.FontSize = 18;
            text.Text = menuItem.Text;
            text.Color = ColorF4.Gray;

            if (menuItem.ChildOptions.Count <= 0)
                continue;

            var submenuList = CreateMenu(buttonNode, false, null, null, menuItem.ChildOptions, true, SubmenuItemHeight, toolbar);
            submenuList.Visibility = EVisibility.Collapsed;
            submenuList.ExcludeFromParentAutoCalcHeight = true;
            submenuList.ExcludeFromParentAutoCalcWidth = true;
            //Undo margin from button
            submenuList.Translation = alignSubmenuToSide
                ? new Vector2(Margin, Margin)
                : new Vector2(-Margin, -Margin);
            //Align top left of submenu...
            submenuList.NormalizedPivot = new Vector2(0.0f, 1.0f);
            if (alignSubmenuToSide)
            {
                //...to top right of parent button
                submenuList.MaxAnchor = new Vector2(1.0f, 1.0f);
                submenuList.MinAnchor = new Vector2(1.0f, 1.0f);
            }
            else
            {
                //...to bottom left of parent button
                submenuList.MaxAnchor = new Vector2(0.0f, 0.0f);
                submenuList.MinAnchor = new Vector2(0.0f, 0.0f);
            }
        }
    }
}
