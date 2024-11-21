using XREngine.Components;
using XREngine.Core.Attributes;

namespace XREngine.Rendering.UI
{
    [RequiresTransform(typeof(UITransform))]
    public class UIComponent : XRComponent
    {
        public bool IsVisible { get; set; } = true;

        public UITransform UITransform => TransformAs<UITransform>(true)!;
    }
}
