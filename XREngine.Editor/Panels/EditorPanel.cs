using XREngine.Components;
using XREngine.Core.Attributes;
using XREngine.Rendering;
using XREngine.Rendering.UI;

namespace XREngine.Editor;

[RequiresTransform(typeof(UIBoundableTransform))]
public partial class EditorPanel : XRComponent
{
    public UIBoundableTransform BoundableTransform => SceneNode.GetTransformAs<UIBoundableTransform>()!;

    public EditorPanel()
    {

    }

    /// <summary>
    /// The window that this panel is displayed in.
    /// </summary>
    public XRWindow? Window { get; set; }
}