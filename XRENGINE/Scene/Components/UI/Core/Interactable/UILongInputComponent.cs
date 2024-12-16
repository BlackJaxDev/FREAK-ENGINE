namespace XREngine.Rendering.UI
{
    public class UILongInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => long.TryParse(input, out _);
    }
}
