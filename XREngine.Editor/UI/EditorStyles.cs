using XREngine.Data.Colors;

namespace XREngine.Editor.UI
{
    public static partial class EditorUI
    {
        public static class Styles
        {
            public static ColorF4 PropertyNameTextColor { get; set; } = ColorF4.Black;
            public static ColorF4 PropertyInputTextColor { get; set; } = ColorF4.Black;
            public static float? PropertyInputFontSize { get; set; } = 14;
            public static float PropertyNameFontSize { get; set; } = 14;
        }
    }
}
