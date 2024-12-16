namespace XREngine.Rendering.UI
{
    public class UIByteInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => byte.TryParse(input, out _);
    }
}
