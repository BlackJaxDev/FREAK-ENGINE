using System.Reflection;
using XREngine.Editor;
using XREngine.Rendering.UI;
using XREngine.Scene;

public class UIInspectorComponent : UIComponent
{
    private List<MemberInfo> _browsableMembers = [];

    public static SceneNode[] Selected => Selection.SceneNodes;
    public List<MemberInfo> BrowsableMembers
    {
        get => _browsableMembers;
        private set => SetField(ref _browsableMembers, value);
    }

    protected override void OnComponentActivated()
    {
        base.OnComponentActivated();
        Selection.SelectionChanged += OnSelectionChanged;
    }
    protected override void OnComponentDeactivated()
    {
        Selection.SelectionChanged -= OnSelectionChanged;
        base.OnComponentDeactivated();
    }

    protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
    {
        base.OnPropertyChanged(propName, prev, field);
        switch (propName)
        {
            case nameof(BrowsableMembers):
                RemakeUI();
                break;
        }
    }

    private void RemakeUI()
    {
        //Clear all children
        foreach (var child in Transform.Children)
            child.Destroy();

        //Create new children
        foreach (var member in BrowsableMembers)
        {
            var node = new SceneNode(SceneNode) { Name = member.Name };
            var m = node.AddComponent<UIInspectorMemberComponent>()!;
            m.Inspector = this;
            m.Member = member;
        }
    }

    private void OnSelectionChanged(SceneNode[] obj)
    {
        List<MemberInfo> allMatching = [];
        bool first = true;
        foreach (var node in obj)
        {
            List<MemberInfo> matching = [];
            foreach (var comp in node.Components)
            {
                var type = comp.GetType();
                var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
                if (first)
                    allMatching.AddRange(members);
                else
                    allMatching = members.Intersect(allMatching).ToList();
            }
        }
        BrowsableMembers = allMatching;
    }
}
