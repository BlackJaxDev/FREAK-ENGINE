namespace XREngine.Rendering.UI
{
    public class UIButtonComponent : UIInteractableComponent
    {
        protected override void OnMouseOverlapEnter() => Highlight();
        protected override void OnMouseOverlapLeave() => Unhighlight();
        protected override void OnGamepadNavigateEnter() => Highlight();
        protected override void OnGamepadNavigateLeave() => Unhighlight();

        public virtual void Click()
        {

        }
        
        protected virtual void Highlight()
        {
            //var param = Parameter<ShaderVector4>("MatColor");
            //if (param != null)
            //    param.Value = Color.Orange.ToVector4();
        }
        protected virtual void Unhighlight()
        {
            //var param = Parameter<ShaderVector4>("MatColor");
            //if (param != null)
            //    param.Value = Color.Magenta.ToVector4();
        }
    }
}