namespace XREngine.Rendering.UI
{
    public class UIUShortInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => ushort.TryParse(input, out _);
    }
}
