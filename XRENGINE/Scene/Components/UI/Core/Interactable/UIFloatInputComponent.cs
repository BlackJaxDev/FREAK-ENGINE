namespace XREngine.Rendering.UI
{
    public class UIFloatInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => float.TryParse(input, out _);
    }
}
