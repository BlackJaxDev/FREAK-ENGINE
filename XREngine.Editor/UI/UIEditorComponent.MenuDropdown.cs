namespace XREngine.Editor.UI.Components;

public partial class UIEditorComponent
{
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
}
