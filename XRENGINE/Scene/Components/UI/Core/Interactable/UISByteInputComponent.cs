namespace XREngine.Rendering.UI
{
    public class UISByteInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => sbyte.TryParse(input, out _);
    }
}
