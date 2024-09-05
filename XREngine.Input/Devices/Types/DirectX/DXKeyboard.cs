namespace XREngine.Input.Devices.DirectX
{
    [Serializable]
    public class DXKeyboard : BaseKeyboard
    {
        public DXKeyboard(int index) : base(index) { }

        protected override void TickStates(float delta)
        {
            throw new NotImplementedException();
        }
    }
}
