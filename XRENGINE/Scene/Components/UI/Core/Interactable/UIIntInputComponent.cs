namespace XREngine.Rendering.UI
{
    public class UIIntInputComponent : UITextInputComponent
    {
        public override bool PostValidateInput(string input)
            => int.TryParse(input, out _);
    }
}
