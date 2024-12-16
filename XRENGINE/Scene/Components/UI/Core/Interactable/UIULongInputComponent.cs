namespace XREngine.Rendering.UI
{
    public class UIULongInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => ulong.TryParse(input, out _);
    }
}
