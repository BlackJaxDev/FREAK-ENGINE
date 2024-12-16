namespace XREngine.Rendering.UI
{
    public class UIDoubleInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => double.TryParse(input, out _);
    }
}
