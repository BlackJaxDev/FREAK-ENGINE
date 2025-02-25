﻿using XREngine.Core.Attributes;
using XREngine.Editor.UI.Toolbar;
using XREngine.Rendering.UI;
using XREngine.Scene;

namespace XREngine.Editor.UI.Components;

/// <summary>
/// The root component for the desktop editor.
/// </summary>
[RequiresTransform(typeof(UIBoundableTransform))]
public partial class UIEditorComponent : UIComponent
{
    private UIToolbarComponent? _toolbar;
    public UIToolbarComponent? Toolbar => _toolbar;

    private List<ToolbarButton> _rootMenuOptions = [];
    public List<ToolbarButton> MenuOptions
    {
        get => _rootMenuOptions;
        set => SetField(ref _rootMenuOptions, value);
    }

    private float _menuHeight = 34.0f;
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
                if (_toolbar is not null)
                    _toolbar.SubmenuItemHeight = MenuHeight;
                break;
            case nameof(MenuOptions):
                if (_toolbar is not null)
                    _toolbar.RootMenuOptions = MenuOptions;
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

        var splitChild = SceneNode.NewChild();
        var splitTfm = splitChild.GetTransformAs<UIDualSplitTransform>(true)!;
        splitTfm.VerticalSplit = true;
        splitTfm.FirstFixedSize = true;
        splitTfm.FixedSize = MenuHeight;
        splitTfm.SplitterSize = 0.0f;

        splitChild.NewChild<UIToolbarComponent>(out var toolbarComp);
        toolbarComp.RootMenuOptions = MenuOptions;
        toolbarComp.SubmenuItemHeight = MenuHeight;
        _toolbar = toolbarComp;

        var listNode = splitChild.NewChild();
        var listTfm = listNode.SetTransform<UIListTransform>();
        listTfm.DisplayHorizontal = true;
        listTfm.ItemAlignment = EListAlignment.TopOrLeft;

        listNode.NewChild<InspectorPanel>(out var hierarchy);
        //hierarchy.RootNodes.Clear();
        //if (World is not null)
        //    hierarchy.RootNodes.AddRange(World.RootNodes);

        //World.Name = "TestWorld";
        hierarchy.InspectedObjects = [Engine.Rendering.Settings];

        ////Create the dockable windows transform for panels
        //var dockableNode = splitChild.NewChild<UIDockingRootComponent>(out var root);
        //var dock = root.DockingTransform;

        //var leftNode = dock.Left?.SceneNode;
        //if (leftNode is not null)
        //{
        //    leftNode.Transform.Clear();
        //    leftNode.NewChild<HierarchyPanel>(out var hierarchy);

        //    hierarchy.RootNodes.Clear();
        //    if (World is not null)
        //        hierarchy.RootNodes.AddRange(World.RootNodes);
        //}

        //var rightNode = dock.Right?.SceneNode;
        //if (rightNode is not null)
        //{
        //    rightNode.Transform.Clear();
        //    rightNode.NewChild<InspectorPanel>(out var inspector);
        //}

        //var bottomNode = dock.Bottom?.SceneNode;
        //if (bottomNode is not null)
        //{
        //    bottomNode.Transform.Clear();
        //    bottomNode.NewChild<ConsolePanel>(out var console);
        //}
    }

    /// <summary>
    /// The scene node that contains the menu options.
    /// </summary>
    public SceneNode ToolbarNode
    {
        get
        {
            var first = SceneNode.FirstChild;
            if (first is null)
                RemakeChildren();
            if (first!.Transform.Children.Count < 2)
                RemakeChildren();
            return first!.FirstChild!;
        }
    }
    /// <summary>
    /// The scene node that contains the dockable windows.
    /// </summary>
    public SceneNode DockableNode
    {
        get
        {
            var first = SceneNode.FirstChild;
            if (first is null)
                RemakeChildren();
            if (first!.Transform.Children.Count < 2)
                RemakeChildren();
            return first.LastChild!;
        }
    }
}
