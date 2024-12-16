namespace XREngine.Rendering.UI
{
    public class UIDecimalInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => decimal.TryParse(input, out _);
    }
}
