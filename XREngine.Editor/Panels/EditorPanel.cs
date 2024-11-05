using XREngine.Data.Core;
using XREngine.Rendering;

namespace XREngine.Editor;

public class EditorPanel : XRBase
{
    public EditorPanel()
    {

    }

    /// <summary>
    /// The window that this panel is displayed in.
    /// </summary>
    public XRWindow? Window { get; set; }
}