using System.Reflection;
using XREngine.Rendering.UI;

public class UIInspectorMemberComponent : UIMaterialComponent
{
    private UIInspectorComponent? _inspector;
    public UIInspectorComponent? Inspector
    {
        get => _inspector;
        set => SetField(ref _inspector, value);
    }

    private MemberInfo? _member = null;
    public MemberInfo? Member
    {
        get => _member;
        set => SetField(ref _member, value);
    }

    public UITextComponent? NameText { get; private set; }

    protected override void OnPropertyChanged<T>(string? propName, T prev, T field)
    {
        base.OnPropertyChanged(propName, prev, field);
        switch (propName)
        {
            case nameof(Member):
            case nameof(Inspector):
                if (Member is not null && Inspector is not null)
                    UpdateUI();
                else
                    ClearUI();
                break;
        }
    }

    private void ClearUI()
    {

    }

    private void UpdateUI()
    {

    }
}
