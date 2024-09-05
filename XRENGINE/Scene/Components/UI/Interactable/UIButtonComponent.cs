using Extensions;
using System.Drawing;
using XREngine.Rendering.Models.Materials;

namespace XREngine.Rendering.UI
{
    public class UIButtonComponent : UIInteractableComponent
    {
        public UIButtonComponent()
        {

        }

        protected override void OnMouseEnter() => Highlight();
        protected override void OnMouseLeave() => Unhighlight();
        protected override void OnGamepadEnter() => Highlight();
        protected override void OnGamepadLeave() => Unhighlight();

        public virtual void Click()
        {

        }
        
        protected virtual void Highlight()
        {
            var param = Parameter<ShaderVector4>("MatColor");
            if (param != null)
                param.Value = Color.Orange.ToVector4();
        }
        protected virtual void Unhighlight()
        {
            var param = Parameter<ShaderVector4>("MatColor");
            if (param != null)
                param.Value = Color.Magenta.ToVector4();
        }
    }
}