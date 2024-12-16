namespace XREngine.Rendering.UI
{
    public class UIUIntInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => uint.TryParse(input, out _);
    }
}
