namespace XREngine.Rendering.UI
{
    public class UIShortInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => short.TryParse(input, out _);
    }
}
